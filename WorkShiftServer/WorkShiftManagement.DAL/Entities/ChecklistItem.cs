namespace WorkShiftManagement.DAL.Entities;

public partial class ChecklistItem
{
    public int ChecklistItemId { get; set; }

    public int ChecklistId { get; set; }

    public string ItemName { get; set; } = null!;

    public bool IsRequired { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Checklist Checklist { get; set; } = null!;

    public virtual ICollection<ChecklistResult> ChecklistResults { get; set; } = new List<ChecklistResult>();
}
