using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;

namespace WorkShiftManagement.BLL.Interfaces.Admin;

public interface IShiftTemplateService
{
    Task<PagedResponse<ShiftTemplateListResponse>> GetAllAsync(
        ShiftTemplateListQuery query, CancellationToken cancellationToken = default);

    Task<ShiftTemplateResponse?> GetByIdAsync(
        int id, CancellationToken cancellationToken = default);

    Task<ShiftTemplateResponse?> CreateAsync(
        CreateShiftTemplateRequest request, CancellationToken cancellationToken = default);

    Task<ShiftTemplateResponse?> UpdateAsync(
        int id, UpdateShiftTemplateRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default);
}
