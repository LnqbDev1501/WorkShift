using System;

namespace WorkShiftManagement.BLL.DTOs.Manager.Response;

public class ContractTerminationWarningDto
{
    public int NotificationId { get; set; }
    public int ContractId { get; set; }
    public short WarningNo { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public Guid SentByManagerId { get; set; }
    public string SentByManagerName { get; set; } = string.Empty;
}

