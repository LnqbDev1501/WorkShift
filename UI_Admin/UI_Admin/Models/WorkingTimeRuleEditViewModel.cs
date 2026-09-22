namespace UI_Admin.Models;

public sealed class WorkingTimeRuleEditViewModel : WorkingTimeRuleFormViewModel
{
    public int WorkingTimeRuleId { get; set; }
    public override bool IsEdit => true;
    public override int? VersionId => WorkingTimeRuleId;
}
