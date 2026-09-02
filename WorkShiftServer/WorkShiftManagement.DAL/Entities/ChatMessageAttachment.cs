using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class ChatMessageAttachment
{
    public int AttachmentId { get; set; }

    public int MessageId { get; set; }

    public string FileUrl { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public string MimeType { get; set; } = null!;

    public long FileSizeBytes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ChatMessage Message { get; set; } = null!;
}
