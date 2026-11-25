using System;
using System.Threading;
using System.Threading.Tasks;

namespace WorkShiftManagement.BLL.Interfaces.Common;

public interface IAuditLogHelper
{
    Task LogAsync(
        string action,
        string tableName,
        string recordId,
        object? oldValue = null,
        object? newValue = null,
        Guid? actorUserId = null,
        CancellationToken cancellationToken = default);
}

