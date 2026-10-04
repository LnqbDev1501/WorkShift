using System;

namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

public class AdminEmployeeRequestListQuery
{
    public string? Search { get; set; }

    public string? RequestType { get; set; }

    public string? Status { get; set; }

    public int? BranchId { get; set; }

    public Guid? UserId { get; set; }

    public DateOnly? FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
