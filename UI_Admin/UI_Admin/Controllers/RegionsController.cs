using Microsoft.AspNetCore.Mvc;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

public class RegionsController : Controller
{
    private readonly RegionApiService _regionApiService;

    public RegionsController(RegionApiService regionApiService)
    {
        _regionApiService = regionApiService;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var regions = await _regionApiService.GetAllAsync();

        return View(regions);
    }
}