using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Admin
{
    [Route("api/admin/region")]
    [ApiController]
    public class AdminRegionController : ControllerBase
    {
        private readonly IRegionService _regionService;

        public AdminRegionController(IRegionService regionService)
        {
            _regionService = regionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
                var result = await _regionService.GetAllAsync();

                return Ok(result);
        }

    }
}
