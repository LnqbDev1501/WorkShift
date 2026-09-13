using WorkShiftManagement.BLL.DTOs.Admin.PublicHoliday.Request;
using WorkShiftManagement.BLL.DTOs.Admin.PublicHoliday.Response;

namespace WorkShiftManagement.BLL.Interfaces.Admin;

public interface IPublicHolidayService
{
    Task<PublicHolidayPageResponse> GetAllAsync(
        PublicHolidayListRequest request,
        Guid callerId,
        CancellationToken ct = default);
    Task<PublicHolidayResponse> GetByIdAsync(
        int id,
        Guid callerId,
        CancellationToken ct = default);
    Task<IReadOnlyList<PublicHolidayResponse>> CreateAsync(
        CreatePublicHolidaysRequest request,
        Guid callerId,
        CancellationToken ct = default);
    Task<PublicHolidayResponse> UpdateAsync(
        int id,
        UpdatePublicHolidayRequest request,
        Guid callerId,
        CancellationToken ct = default);
    Task DeleteAsync(
        int id, 
        Guid callerId,
        CancellationToken ct = default);
}
