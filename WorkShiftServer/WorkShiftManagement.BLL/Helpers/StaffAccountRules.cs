using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.BLL.Helpers;

internal static class StaffAccountRules
{
    internal static void ValidateStaff(User staff, int? branchId)
    {
        if (staff.Status != "ACTIVE" || staff.DeletedAt.HasValue || staff.Role?.RoleName != "STAFF" ||
            staff.Role.Status != "ACTIVE" || !branchId.HasValue || staff.BranchId != branchId || staff.Branch?.Status != "ACTIVE")
            throw new UnauthorizedAccessException("An active staff account in this branch is required.");
    }
}
