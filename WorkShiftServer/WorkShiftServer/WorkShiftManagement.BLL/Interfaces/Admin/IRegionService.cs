using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkShiftManagement.BLL.DTOs.Admin.Response;

namespace WorkShiftManagement.BLL.Interfaces.Admin
{
    public interface IRegionService
    {
        Task<List<RegionResponse>> GetAllAsync();
    }
}
