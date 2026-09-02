using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class ChatMember
{
    public int ConversationMemberId { get; set; }

    public int ConversationId { get; set; }

    public Guid UserId { get; set; }

    public DateTime JoinedAt { get; set; }

    public DateTime? LeftAt { get; set; }

    public bool IsActive { get; set; }

    public int? LastReadMessageId { get; set; }

    public DateTime? LastReadAt { get; set; }

    public virtual ChatConversation Conversation { get; set; } = null!;

    public virtual ChatMessage? LastReadMessage { get; set; }

    public virtual User User { get; set; } = null!;
}
