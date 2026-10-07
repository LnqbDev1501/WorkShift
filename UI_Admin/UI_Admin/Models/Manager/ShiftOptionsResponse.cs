namespace UI_Admin.Models.Manager;

public sealed class ShiftOptionsResponse
{
    public List<ShiftTemplateOptionResponse> Templates { get; set; } = [];
    public List<ShiftTypeOptionResponse> EmployeeTypes { get; set; } = [];
    public List<ShiftStaffOptionResponse> Staff { get; set; } = [];
}
public sealed class ShiftTemplateOptionResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int BreakMinutes { get; set; }
}
public sealed class ShiftTypeOptionResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
public sealed class ShiftStaffOptionResponse
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = "";
    public int EmployeeTypeId { get; set; }
    public string EmployeeTypeName { get; set; } = "";
}

