namespace EduCore.Services.Assignment;

// How many subjects a junior-high or high-school teacher may be qualified for at once.
// Elementary is exempt entirely -- a homeroom teacher covers however many subjects the
// school has, since that IS the elementary teaching model.
public class AssignmentOptions
{
    public const string SectionName = "Assignment";
    public int MaxSubjectsPerTeacher { get; set; } = 2;
}
