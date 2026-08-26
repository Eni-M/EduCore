namespace EduCore.Dtos.Statistics.Rows;

// Separate from CountRow because a class breakdown needs the real id and grade -- stringifying
// the id and parsing it back on the other side loses both.
public record ClassCountRow(Guid ClassId, string ClassName, int Grade, int Count);
