namespace EduCore.Dtos.Mark;

public record MarkChangeDto(
    DateTime ChangedAt,
    string ChangedBy,
    int? OldFirstMark,
    int? OldSecondMark,
    int? NewFirstMark,
    int? NewSecondMark,
    string? Reason,
    bool AfterClose);
