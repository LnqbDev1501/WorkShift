using System;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.Interfaces.Common;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.BLL.Helpers;

public sealed class AuditLogHelper : IAuditLogHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly ILogger<AuditLogHelper>? _logger;

    public AuditLogHelper(
        AppDbContext context,
        IHttpContextAccessor? httpContextAccessor = null,
        ILogger<AuditLogHelper>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(
        string action,
        string tableName,
        string recordId,
        object? oldValue = null,
        object? newValue = null,
        Guid? actorUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            return;
        }

        AuditLog? entry = null;
        try
        {
            var resolvedActorId = ResolveActorUserId(actorUserId);
            var ipAddress = ResolveIpAddress();

            entry = new AuditLog
            {
                UserId = resolvedActorId,
                Action = action.Trim(),
                TableName = string.IsNullOrWhiteSpace(tableName) ? null : tableName.Trim(),
                RecordId = string.IsNullOrWhiteSpace(recordId) ? null : recordId.Trim(),
                OldValue = SerializeValue(oldValue),
                NewValue = SerializeValue(newValue),
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(entry);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            if (entry != null)
            {
                try
                {
                    var tracked = _context.Entry(entry);
                    if (tracked.State != EntityState.Detached)
                    {
                        tracked.State = EntityState.Detached;
                    }
                }
                catch
                {
                    // Ignore tracker cleanup failures.
                }
            }

            _logger?.LogWarning(
                ex,
                "Failed to write audit log for action '{Action}' on table '{TableName}' (record '{RecordId}').",
                action,
                tableName,
                recordId);
        }
    }

    private Guid? ResolveActorUserId(Guid? explicitActorUserId)
    {
        if (explicitActorUserId.HasValue && explicitActorUserId.Value != Guid.Empty)
        {
            return explicitActorUserId.Value;
        }

        var user = _httpContextAccessor?.HttpContext?.User;
        if (user == null)
        {
            return null;
        }

        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? user.FindFirstValue("user_id");

        return Guid.TryParse(claim, out var userId) && userId != Guid.Empty
            ? userId
            : null;
    }

    private IPAddress? ResolveIpAddress()
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext == null)
        {
            return null;
        }

        if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var rawHeader = forwardedFor.ToString();
            if (!string.IsNullOrWhiteSpace(rawHeader))
            {
                var firstIp = rawHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (firstIp.Length > 0 && TryParseIp(firstIp[0], out var parsedForwardedIp))
                {
                    return NormalizeIp(parsedForwardedIp);
                }
            }
        }

        var remoteIp = httpContext.Connection.RemoteIpAddress;
        return remoteIp != null ? NormalizeIp(remoteIp) : null;
    }

    private static bool TryParseIp(string candidate, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IPAddress? ipAddress)
    {
        ipAddress = null;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        if (IPAddress.TryParse(candidate, out ipAddress))
        {
            return true;
        }

        // Handle "ip:port" IPv4 format if present in X-Forwarded-For.
        var colonIndex = candidate.LastIndexOf(':');
        if (colonIndex > 0 && candidate.IndexOf(':') == colonIndex)
        {
            var hostPart = candidate[..colonIndex];
            return IPAddress.TryParse(hostPart, out ipAddress);
        }

        return false;
    }

    private static IPAddress NormalizeIp(IPAddress ipAddress) =>
        ipAddress.IsIPv4MappedToIPv6 ? ipAddress.MapToIPv4() : ipAddress;

    private static string? SerializeValue(object? value)
    {
        if (value == null)
        {
            return null;
        }

        return JsonSerializer.Serialize(value, JsonOptions);
    }
}
