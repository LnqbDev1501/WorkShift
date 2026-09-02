using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkShiftManagement.BLL.DTOs.Admin.Response
{
    public class RegionResponse
    {
        public string RegionCode { get; set; } = null!;

        public string RegionName { get; set; } = null!;

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
