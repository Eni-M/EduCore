namespace EduCore.Dtos.Notification;

public record NotificationDto(Guid Id, string Message, string? Link, DateTime CreatedAt, DateTime? ReadAt);
