using System;
using System.Collections.Generic;
using System.Text.Json;
using WorkShiftManagement.BLL.DTOs.Manager.Response;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.BLL.Helpers;

public static class EmploymentContractAuditLogMapper
{
    public static ContractHistoryEntryDto MapAuditLogToEntry(AuditLog log)
    {
        var entry = new ContractHistoryEntryDto
        {
            AuditLogId = log.AuditLogId,
            Action = log.Action,
            ActionTitle = GetActionTitle(log.Action),
            ActorId = log.UserId,
            ActorName = log.User?.FullName ?? (log.UserId == null ? "Hệ thống" : "Người dùng"),
            ActorRole = log.User?.Role?.RoleName ?? (log.UserId == null ? "SYSTEM" : string.Empty),
            Timestamp = log.CreatedAt
        };

        JsonDocument? newDoc = null;
        JsonDocument? oldDoc = null;

        try
        {
            if (!string.IsNullOrWhiteSpace(log.NewValue))
                newDoc = JsonDocument.Parse(log.NewValue);
        }
        catch { /* ignore legacy json error */ }

        try
        {
            if (!string.IsNullOrWhiteSpace(log.OldValue))
                oldDoc = JsonDocument.Parse(log.OldValue);
        }
        catch { /* ignore legacy json error */ }

        using (newDoc)
        using (oldDoc)
        {
            var newRoot = newDoc?.RootElement;
            var oldRoot = oldDoc?.RootElement;

            if (newRoot?.TryGetProperty("fromStatus", out var fsProp) == true && fsProp.ValueKind == JsonValueKind.String)
                entry.FromStatus = fsProp.GetString();
            else if (oldRoot?.TryGetProperty("fromStatus", out var ofsProp) == true && ofsProp.ValueKind == JsonValueKind.String)
                entry.FromStatus = ofsProp.GetString();

            if (newRoot?.TryGetProperty("toStatus", out var tsProp) == true && tsProp.ValueKind == JsonValueKind.String)
                entry.ToStatus = tsProp.GetString();

            if (newRoot?.TryGetProperty("reason", out var rProp) == true && rProp.ValueKind == JsonValueKind.String)
                entry.Reason = rProp.GetString();
            else if (newRoot?.TryGetProperty("staffComment", out var scProp) == true && scProp.ValueKind == JsonValueKind.String)
                entry.Reason = scProp.GetString();
            else if (newRoot?.TryGetProperty("reviewComment", out var rcProp) == true && rcProp.ValueKind == JsonValueKind.String)
                entry.Reason = rcProp.GetString();

            if (newRoot?.TryGetProperty("note", out var nProp) == true && nProp.ValueKind == JsonValueKind.String)
                entry.Note = nProp.GetString();

            if (newRoot?.TryGetProperty("contractTemplateVersion", out var tvProp) == true && tvProp.ValueKind == JsonValueKind.String)
                entry.TemplateVersion = tvProp.GetString();

            if (newRoot?.TryGetProperty("contractFilePath", out var fpProp) == true && fpProp.ValueKind == JsonValueKind.String)
                entry.ContractFilePath = fpProp.GetString();

            if (newRoot?.TryGetProperty("changeRequestId", out var crIdProp) == true && crIdProp.TryGetInt32(out var crIdVal))
                entry.ChangeRequestId = crIdVal;

            if (newRoot?.TryGetProperty("items", out var itemsProp) == true && itemsProp.ValueKind == JsonValueKind.Array)
            {
                var crItems = new List<ContractHistoryChangeItemDto>();
                foreach (var item in itemsProp.EnumerateArray())
                {
                    var fieldName = item.TryGetProperty("fieldName", out var fn) ? fn.GetString() ?? "" : "";
                    var origVal = item.TryGetProperty("originalValue", out var ov) ? ov.GetString() : null;
                    var propVal = item.TryGetProperty("proposedValue", out var pv) ? pv.GetString() ?? "" : "";
                    var itemReason = item.TryGetProperty("reason", out var ir) ? ir.GetString() : null;

                    crItems.Add(new ContractHistoryChangeItemDto
                    {
                        FieldName = GetFriendlyFieldName(fieldName),
                        OriginalValue = DeserializeJsonString(origVal),
                        ProposedValue = DeserializeJsonString(propVal) ?? propVal,
                        Reason = itemReason
                    });
                }
                if (crItems.Count > 0)
                    entry.ChangeRequestItems = crItems;
            }

            if (log.Action.StartsWith("UPDATE_DRAFT", StringComparison.OrdinalIgnoreCase))
            {
                var changedFields = ExtractChangedFields(oldRoot, newRoot);
                if (changedFields.Count > 0)
                    entry.ChangedFields = changedFields;
            }
        }

        return entry;
    }

    public static List<ContractHistoryChangeFieldDto> ExtractChangedFields(JsonElement? oldRoot, JsonElement? newRoot)
    {
        var list = new List<ContractHistoryChangeFieldDto>();
        if (!newRoot.HasValue)
            return list;

        var newElem = newRoot.Value;
        var oldElem = oldRoot;

        string? GetVal(JsonElement elem, params string[] names)
        {
            foreach (var name in names)
            {
                if (elem.TryGetProperty(name, out var prop))
                {
                    return prop.ValueKind switch
                    {
                        JsonValueKind.String => prop.GetString(),
                        JsonValueKind.Number => prop.GetRawText(),
                        JsonValueKind.Null => null,
                        _ => prop.GetRawText()
                    };
                }
            }
            return null;
        }

        void CompareField(string friendlyName, params string[] propNames)
        {
            string? newVal = GetVal(newElem, propNames);
            string? oldVal = oldElem.HasValue ? GetVal(oldElem.Value, propNames) : null;

            if (newVal != null && oldVal != null && !string.Equals(newVal, oldVal, StringComparison.Ordinal))
            {
                list.Add(new ContractHistoryChangeFieldDto
                {
                    FieldName = friendlyName,
                    OldValue = oldVal,
                    NewValue = newVal
                });
            }
        }

        CompareField("Ngày bắt đầu", "startDate", "StartDate");
        CompareField("Ngày kết thúc", "endDate", "EndDate");
        CompareField("Mức lương giờ", "hourlyRate", "HourlyRate");
        CompareField("Vị trí công việc", "employeePosition", "EmployeePositionSnapshot", "employeePositionSnapshot");
        CompareField("Điều khoản hợp đồng", "contractTerms", "ContractTerms");

        return list;
    }

    public static string GetFriendlyFieldName(string fieldName) => fieldName.ToLowerInvariant() switch
    {
        "startdate" or "start_date" => "Ngày bắt đầu",
        "enddate" or "end_date" => "Ngày kết thúc",
        "hourlyrate" or "hourly_rate" => "Mức lương giờ",
        "employeeposition" or "employee_position" => "Vị trí công việc",
        "contractterms" or "contract_terms" => "Điều khoản hợp đồng",
        _ => fieldName
    };

    public static string GetActionTitle(string action) => action.ToUpperInvariant() switch
    {
        "CREATE_DRAFT" => "Tạo dự thảo hợp đồng",
        "UPDATE_DRAFT" => "Cập nhật dự thảo hợp đồng",
        "UPDATE_DRAFT_AFTER_APPROVAL" => "Cập nhật hợp đồng theo yêu cầu thay đổi",
        "SEND_CONTRACT" => "Gửi hợp đồng cho nhân viên",
        "RESEND_CONTRACT" => "Gửi lại hợp đồng mới cho nhân viên",
        "STAFF_ACCEPT_CONTRACT" => "Nhân viên đồng ý hợp đồng",
        "SUBMIT_CHANGE_REQUEST" => "Nhân viên yêu cầu thay đổi hợp đồng",
        "APPROVE_CHANGE_REQUEST" => "Quản lý duyệt yêu cầu thay đổi",
        "REJECT_CHANGE_REQUEST" => "Quản lý từ chối yêu cầu thay đổi",
        "CANCEL_CONTRACT" => "Hủy hợp đồng",
        "AUTO_CANCEL_CONTRACT" => "Hệ thống tự động hủy hợp đồng do quá hạn chấp nhận",
        "TERMINATE_CONTRACT" => "Chấm dứt hợp đồng",
        "AUTO_ACTIVATE_CONTRACT" => "Hệ thống tự động kích hoạt hợp đồng",
        "AUTO_EXPIRE_CONTRACT" => "Hệ thống tự động hết hạn hợp đồng",
        "CREATE_RENEWAL_CONTRACT" => "Tạo hợp đồng gia hạn/thay thế",
        "LINKED_REPLACEMENT_CONTRACT" => "Liên kết hợp đồng thay thế mới",
        _ => action
    };

    public static string? DeserializeJsonString(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<string>(json);
        }
        catch
        {
            return json;
        }
    }
}
