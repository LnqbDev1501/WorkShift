using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class ChatMessage
{
    public int MessageId { get; set; }

    public int ConversationId { get; set; }

    public Guid SenderId { get; set; }

    public string? Content { get; set; }

    public DateTime SentAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<ChatMember> ChatMembers { get; set; } = new List<ChatMember>();

    public virtual ICollection<ChatMessageAttachment> ChatMessageAttachments { get; set; } = new List<ChatMessageAttachment>();

    public virtual ChatConversation Conversation { get; set; } = null!;

    public virtual User Sender { get; set; } = null!;
}
