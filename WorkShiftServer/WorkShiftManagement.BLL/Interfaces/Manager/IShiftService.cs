using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;

namespace WorkShiftManagement.BLL.Interfaces.Manager;

public interface IShiftService
{
    Task<ShiftOptionsResponse> GetOptionsAsync(Guid managerId, CancellationToken ct = default);
    Task<ShiftDetailResponse> CreateAsync(Guid managerId, int weekId, CreateShiftRequest request, CancellationToken ct = default);
    Task<ShiftDetailResponse> GetByIdAsync(Guid managerId, int shiftId, CancellationToken ct = default);
    Task<ShiftDetailResponse> UpdateAsync(Guid managerId, int shiftId, UpdateShiftRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid managerId, int shiftId, CancellationToken ct = default);
}
