namespace EduCore.Models;

public class Message
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    public Guid SenderUserId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime SentAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public Conversation Conversation { get; set; } = null!;
}