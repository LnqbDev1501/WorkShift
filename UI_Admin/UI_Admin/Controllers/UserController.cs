using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

public class UserController : Controller
{
    private readonly UserApiService _userApiService;

    public UserController(UserApiService userApiService) => _userApiService = userApiService;

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var branches = await _userApiService.GetActiveBranchesAsync(cancellationToken);
        var roles = await _userApiService.GetOperationalRolesAsync(cancellationToken);
        var defaultRoleId = roles.FirstOrDefault(r => r.Name.Equals("Manager", StringComparison.OrdinalIgnoreCase))?.Id ?? 2;

        var model = new UserCreateViewModel
        {
            AvailableBranches = branches,
            AvailableRoles = roles,
            RoleId = defaultRoleId,
            BranchId = branches.FirstOrDefault()?.Id
        };
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableBranches = await _userApiService.GetActiveBranchesAsync(cancellationToken);
            model.AvailableRoles = await _userApiService.GetOperationalRolesAsync(cancellationToken);
            return View(model);
        }

        try
        {
            await _userApiService.CreateAsync(model, cancellationToken);
            TempData["AccountCreatedSuccess"] = true;
            return RedirectToAction(nameof(List));
        }
        catch (HttpRequestException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            model.AvailableBranches = await _userApiService.GetActiveBranchesAsync(cancellationToken);
            model.AvailableRoles = await _userApiService.GetOperationalRolesAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] UserListQueryViewModel query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            query = new UserListQueryViewModel();

        try
        {
            return View(await _userApiService.GetListAsync(query, cancellationToken));
        }
        catch (HttpRequestException exception)
        {
            return View(new UserListViewModel { Query = query, ErrorMessage = exception.Message });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendSetupLink(Guid userId, UserListQueryViewModel query, CancellationToken cancellationToken) =>
        await RunActionAsync(
            () => _userApiService.ResendSetupLinkAsync(userId, cancellationToken),
            "Account setup link sent successfully.", query);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid userId, UserListQueryViewModel query, CancellationToken cancellationToken) =>
        await RunActionAsync(
            () => _userApiService.DeleteAsync(userId, cancellationToken),
            "Account deactivated successfully.", query);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(Guid userId, int? branchId, UserListQueryViewModel query, CancellationToken cancellationToken) =>
        await RunActionAsync(
            () => _userApiService.RestoreAsync(userId, branchId, cancellationToken),
            "Account restored successfully.", query);

    private async Task<IActionResult> RunActionAsync(Func<Task> action, string successMessage, UserListQueryViewModel query)
    {
        try
        {
            await action();
            TempData["SuccessMessage"] = successMessage;
        }
        catch (HttpRequestException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(List), query);
    }
}
