using EduCore.Enums;
using EduCore.Exceptions;

namespace EduCore.Services;

public static class GradeBands
{
    private static readonly Dictionary<SchoolType, (int Min, int Max)> Bands = new()
    {
        [SchoolType.Elementary] = (1, 5),
        [SchoolType.JuniorHigh] = (6, 9),
        [SchoolType.HighSchool] = (10, 12),
    };

    public static void Validate(SchoolType type, int grade)
    {
        var (min, max) = Bands[type];

        if (grade < min || grade > max)
            throw new BusinessException(
                $"Grade {grade} does not belong to a {type} school (grades {min}-{max}).");
    }
}
