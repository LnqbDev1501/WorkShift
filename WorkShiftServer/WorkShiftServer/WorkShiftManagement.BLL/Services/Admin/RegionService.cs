using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.BLL.Services.Admin
{
    public class RegionService : IRegionService
    {
        private readonly IRegionRepo _regionRepo;

        public RegionService(IRegionRepo regionRepo)
        {
            _regionRepo = regionRepo;
        }

        public async Task<List<RegionResponse>> GetAllAsync()
        {
            var regions = await _regionRepo.GetAllAsync();
            return regions.Select(region => new RegionResponse
            {
                RegionCode = region.RegionCode,
                RegionName = region.RegionName,
                Description = region.Description,
                IsActive = region.IsActive,
                CreatedAt = region.CreatedAt
            }).ToList();
        }
    }
}
