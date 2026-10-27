using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

public sealed class UpdateShiftTemplateRequest : IValidatableObject
{
    public string? ShiftName { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string? Description { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Creation and replacement must enforce the same time and duration rules.
        var request = new CreateShiftTemplateRequest
        {
            ShiftName = ShiftName,
            StartTime = StartTime,
            EndTime = EndTime,
            Description = Description
        };
        return request.Validate(new ValidationContext(request));
    }
}
