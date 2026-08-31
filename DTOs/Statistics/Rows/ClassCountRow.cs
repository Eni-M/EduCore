namespace EduCore.Dtos.Statistics.Rows;

public record ClassCountRow(Guid ClassId, string ClassName, int Grade, int Count);
