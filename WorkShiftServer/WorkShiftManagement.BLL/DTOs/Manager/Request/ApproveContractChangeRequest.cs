using System.Collections.Generic;

namespace WorkShiftManagement.BLL.DTOs.Manager.Request;

public class ApproveContractChangeRequest
{
    public string? ReviewComment { get; set; }

    /// <summary>
    /// Các trường Manager chốt giá trị cuối cùng để áp dụng vào hợp đồng.
    /// </summary>
    public List<ApproveContractChangeRequestItemInputDto>? Items { get; set; }
}

public class ApproveContractChangeRequestItemInputDto
{
    public string FieldName { get; set; } = null!;
    public string FinalValue { get; set; } = null!;
}
