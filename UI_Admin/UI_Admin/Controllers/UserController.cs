using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

[Authorize(Roles = "ADMIN")]
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

    [HttpGet]
    public async Task<IActionResult> Detail(Guid? id, CancellationToken cancellationToken)
    {
        if (!id.HasValue || id.Value == Guid.Empty)
        {
            TempData["ErrorMessage"] = "Invalid user ID.";
            return RedirectToAction(nameof(List));
        }

        try
        {
            var model = await _userApiService.GetDetailAsync(id.Value, cancellationToken);
            return View(model);
        }
        catch (HttpRequestException exception)
        {
            TempData["ErrorMessage"] = $"Could not load user details: {exception.Message}";
            return RedirectToAction(nameof(List));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid? id, CancellationToken cancellationToken)
    {
        if (!id.HasValue || id.Value == Guid.Empty)
        {
            TempData["ErrorMessage"] = "Invalid user ID.";
            return RedirectToAction(nameof(List));
        }

        try
        {
            var detail = await _userApiService.GetDetailAsync(id.Value, cancellationToken);
            var branches = await _userApiService.GetActiveBranchesAsync(cancellationToken);
            var roles = await _userApiService.GetOperationalRolesAsync(cancellationToken);

            var model = new UserEditViewModel
            {
                UserId = detail.UserId,
                FullName = detail.FullName,
                Email = detail.Email,
                Phone = detail.Phone,
                IdentityNo = detail.IdentityNo,
                Gender = detail.Gender,
                DateOfBirth = detail.DateOfBirth,
                Address = detail.Address,
                RoleId = detail.RoleId,
                RoleName = detail.RoleName,
                BranchId = detail.BranchId,
                AvatarUrl = detail.AvatarUrl,
                Status = detail.Status,
                AvailableBranches = branches,
                AvailableRoles = roles
            };

            return View(model);
        }
        catch (HttpRequestException exception)
        {
            TempData["ErrorMessage"] = $"Could not load user data: {exception.Message}";
            return RedirectToAction(nameof(List));
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UserEditViewModel model, CancellationToken cancellationToken)
    {
        if (!model.IsAdmin && (!model.BranchId.HasValue || model.BranchId <= 0))
        {
            ModelState.AddModelError(nameof(model.BranchId), "Target branch is required for Manager and Accountant accounts.");
        }

        if (!ModelState.IsValid)
        {
            model.AvailableBranches = await _userApiService.GetActiveBranchesAsync(cancellationToken);
            model.AvailableRoles = await _userApiService.GetOperationalRolesAsync(cancellationToken);
            return View(model);
        }

        try
        {
            await _userApiService.UpdateAsync(id, model, cancellationToken);
            TempData["UserUpdatedSuccess"] = true;
            return RedirectToAction(nameof(Detail), new { id });
        }
        catch (HttpRequestException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            model.AvailableBranches = await _userApiService.GetActiveBranchesAsync(cancellationToken);
            model.AvailableRoles = await _userApiService.GetOperationalRolesAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendDetailSetupLink(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            await _userApiService.ResendSetupLinkAsync(userId, cancellationToken);
            TempData["ResendSuccess"] = true;
        }
        catch (HttpRequestException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Detail), new { id = userId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendSetupLink(Guid userId, UserListQueryViewModel query, CancellationToken cancellationToken)
    {
        try
        {
            await _userApiService.ResendSetupLinkAsync(userId, cancellationToken);
            TempData["ResendSuccess"] = true;
        }
        catch (HttpRequestException exception)
        {
            TempData["ResendError"] = exception.Message;
        }

        return RedirectToAction(nameof(List), query);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid userId, UserListQueryViewModel query, CancellationToken cancellationToken)
    {
        try
        {
            await _userApiService.DeleteAsync(userId, cancellationToken);
            TempData["UserDeactivatedSuccess"] = true;
        }
        catch (HttpRequestException exception)
        {
            TempData["UserDeactivatedError"] = exception.Message;
        }

        return RedirectToAction(nameof(List), query);
    }

    [HttpGet]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var detail = await _userApiService.GetDetailAsync(id, cancellationToken);
            if (!detail.IsInactive)
            {
                TempData["ErrorMessage"] = "Only inactive accounts can be restored.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var branches = await _userApiService.GetActiveBranchesAsync(cancellationToken);
            var model = new UserRestoreViewModel
            {
                UserId = detail.UserId,
                FullName = detail.FullName,
                Email = detail.Email,
                Phone = detail.Phone,
                RoleName = detail.RoleName,
                AvatarUrl = detail.AvatarUrl,
                Initials = detail.Initials,
                PreviousBranchId = detail.PreviousBranchId ?? detail.BranchId,
                PreviousBranchName = detail.PreviousBranchName ?? detail.BranchName,
                BranchId = detail.PreviousBranchId ?? detail.BranchId,
                AvailableBranches = branches
            };

            return View(model);
        }
        catch (HttpRequestException exception)
        {
            TempData["ErrorMessage"] = $"Could not load account details: {exception.Message}";
            return RedirectToAction(nameof(List));
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(Guid id, UserRestoreViewModel model, CancellationToken cancellationToken)
    {
        if (!model.BranchId.HasValue || model.BranchId <= 0)
        {
            ModelState.AddModelError(nameof(model.BranchId), "Please select a target branch for this account.");
        }

        if (!ModelState.IsValid)
        {
            model.AvailableBranches = await _userApiService.GetActiveBranchesAsync(cancellationToken);
            return View(model);
        }

        try
        {
            await _userApiService.RestoreAsync(id, model.BranchId, cancellationToken);
            TempData["UserRestoredSuccess"] = true;
            return RedirectToAction(nameof(Detail), new { id });
        }
        catch (HttpRequestException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            model.AvailableBranches = await _userApiService.GetActiveBranchesAsync(cancellationToken);
            return View(model);
        }
    }

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
