using System;

namespace WorkShiftManagement.BLL.DTOs.Manager.Response;

public class SendContractTerminationWarningResponse
{
    public int NotificationId { get; set; }
    public int ContractId { get; set; }
    public short TerminationNoticeNo { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int SentWarningCount { get; set; }
    public bool CanRequestTermination { get; set; }
}

