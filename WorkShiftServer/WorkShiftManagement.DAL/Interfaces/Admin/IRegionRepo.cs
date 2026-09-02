using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Admin
{
    public interface IRegionRepo
    {
        Task<List<Region>> GetAllAsync();

    }
}
