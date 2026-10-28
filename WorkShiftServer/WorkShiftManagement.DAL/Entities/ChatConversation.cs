namespace WorkShiftManagement.DAL.Entities;

public partial class ChatConversation
{
    public int ConversationId { get; set; }

    public int? BranchId { get; set; }

    public string ConversationType { get; set; } = null!;

    public string? Title { get; set; }

    public string Status { get; set; } = null!;

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastMessageAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Branch? Branch { get; set; }

    public virtual ICollection<ChatMember> ChatMembers { get; set; } = new List<ChatMember>();

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual User CreatedByNavigation { get; set; } = null!;
}
