using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Models.Admin;

public class CancelOverdueContractsResult
{
    public int CancelledCount { get; set; }
    public List<OverdueContractNotificationDispatch> Dispatches { get; set; } = new();
}

public class OverdueContractNotificationDispatch
{
    public int NotificationId { get; set; }
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int BranchId { get; set; }
    public List<Guid> RecipientUserIds { get; set; } = new();
    public List<string> RecipientEmails { get; set; } = new();
    public bool HasEmailChannel { get; set; }
}
