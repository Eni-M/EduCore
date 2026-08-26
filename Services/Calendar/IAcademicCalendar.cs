namespace EduCore.Services.Calendar;

public interface IAcademicCalendar
{
    (DateOnly Start, DateOnly End) FirstPeriod(DateOnly yearStart, DateOnly yearEnd);
    (DateOnly Start, DateOnly End) SecondPeriod(DateOnly yearStart, DateOnly yearEnd);
}
