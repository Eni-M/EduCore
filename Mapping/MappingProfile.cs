using AutoMapper;
using EduCore.Models;

namespace EduCore.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        MapSchools();
        MapSubjects();
        MapAcademicCalendar();
        MapUsers();
        MapStudents();
        MapTeachers();
        MapAdmins();
        MapClasses();
        MapLessonRecords();
    }

    // An inbound DTO may only set the fields it actually carries. Anything else on the entity
    // -- Id, SchoolId, row version, navigations -- is ignored by name, so adding a property to
    // a DTO can never quietly make a new column client-writable.
    //
    // A property with a non-public setter is ignored too, even when a DTO field shares its
    // name. Reflection's CanWrite is true for a private setter, so without this the mapper
    // would write straight past a domain method like Attendance.Record and break the very
    // invariant that method exists to guarantee.
    private IMappingExpression<TSource, TDestination> CreateWriteMap<TSource, TDestination>()
    {
        var sourceMembers = typeof(TSource)
            .GetProperties()
            .Select(p => p.Name)
            .ToHashSet();

        var map = CreateMap<TSource, TDestination>();

        foreach (var member in typeof(TDestination).GetProperties()
                     .Where(p => p.CanWrite
                                 && (!sourceMembers.Contains(p.Name)
                                     || p.SetMethod is not { IsPublic: true })))
            map.ForMember(member.Name, o => o.Ignore());

        return map;
    }

    private void MapSchools()
    {
        CreateMap<School, Dtos.School.SchoolListDto>();
        CreateWriteMap<Dtos.School.SchoolCreateDto, School>();
        CreateWriteMap<Dtos.School.SchoolEditDto, School>();
    }

    private void MapSubjects()
    {
        CreateMap<Subject, Dtos.Subject.SubjectListDto>();
        CreateWriteMap<Dtos.Subject.SubjectCreateDto, Subject>();
        CreateWriteMap<Dtos.Subject.SubjectEditDto, Subject>();

        CreateMap<TeacherSubject, Dtos.TeacherSubject.TeacherSubjectListDto>()
            .ForCtorParam("SubjectName", o => o.MapFrom(ts => ts.Subject.Name));

        CreateWriteMap<Dtos.TeacherSubject.TeacherSubjectCreateDto, TeacherSubject>();
    }

    private void MapAcademicCalendar()
    {
        // SchoolName has no source on the entity: it is joined in only for the SuperAdmin
        // list, which is projected in the repository rather than mapped here.
        CreateMap<AcademicYear, Dtos.AcademicYear.AcademicYearListDto>()
            .ForCtorParam("SchoolName", o => o.MapFrom(_ => (string?)null));
        CreateWriteMap<Dtos.AcademicYear.AcademicYearCreateDto, AcademicYear>();
        // GradingPeriod is built manually in AcademicYearService (auto-created with the year)
        // and edited in place, so it has no write map.
    }

    private void MapUsers()
    {
        CreateWriteMap<Dtos.Student.StudentCreateDto, User>();
        CreateWriteMap<Dtos.Teacher.TeacherCreateDto, User>();
        CreateWriteMap<Dtos.Admin.AdminCreateDto, User>();

        CreateWriteMap<Dtos.Student.StudentEditDto, User>();
        CreateWriteMap<Dtos.Teacher.TeacherEditDto, User>();
        CreateWriteMap<Dtos.Admin.AdminEditDto, User>();
    }

    private void MapStudents()
    {
        CreateMap<Parent, Dtos.Parent.ParentListDto>();
        CreateWriteMap<Dtos.Parent.ParentCreateDto, Parent>();
        CreateWriteMap<Dtos.Parent.ParentEditDto, Parent>();

        CreateMap<Student, Dtos.Student.StudentListDto>()
            .ForCtorParam("FirstName", o => o.MapFrom(s => s.User.FirstName))
            .ForCtorParam("LastName", o => o.MapFrom(s => s.User.LastName))
            .ForCtorParam("Email", o => o.MapFrom(s => s.User.Email))
            .ForCtorParam("IsGraduated", o => o.MapFrom(s => s.GraduationDate != null))
            // Always mapped; the services strip it for callers who may not manage
            // sign-ins. Filling it here rather than in each service keeps the one
            // projection that knows how to reach User in one place.
            .ForCtorParam("Account", o => o.MapFrom(s =>
                new Dtos.Account.AccountDto(s.UserId, s.User.DeactivatedAt)));

        CreateMap<Student, Dtos.Student.StudentDetailDto>()
            .ForCtorParam("FirstName", o => o.MapFrom(s => s.User.FirstName))
            .ForCtorParam("LastName", o => o.MapFrom(s => s.User.LastName))
            .ForCtorParam("Email", o => o.MapFrom(s => s.User.Email))
            // Null when the student has no recorded city, which reads as "not recorded"
            // rather than as a blank the caller has to interpret.
            .ForCtorParam("CityName", o => o.MapFrom(s => s.City != null ? s.City.Name : null))
            .ForCtorParam("StateName", o => o.MapFrom(s => s.City != null ? s.City.State.Name : null));

        CreateWriteMap<Dtos.Student.StudentCreateDto, Student>();
        CreateWriteMap<Dtos.Student.StudentEditDto, Student>();
    }

    private void MapTeachers()
    {
        CreateMap<Teacher, Dtos.Teacher.TeacherListDto>()
            .ForCtorParam("FirstName", o => o.MapFrom(t => t.User.FirstName))
            .ForCtorParam("LastName", o => o.MapFrom(t => t.User.LastName))
            .ForCtorParam("Email", o => o.MapFrom(t => t.User.Email))
            .ForCtorParam("Subjects", o => o.MapFrom(t => t.TeacherSubjects.Select(ts => ts.Subject.Name)))
            .ForCtorParam("Account", o => o.MapFrom(t =>
                new Dtos.Account.AccountDto(t.UserId, t.User.DeactivatedAt)));

        CreateMap<Teacher, Dtos.Teacher.TeacherDetailDto>()
            .ForCtorParam("FirstName", o => o.MapFrom(t => t.User.FirstName))
            .ForCtorParam("LastName", o => o.MapFrom(t => t.User.LastName))
            .ForCtorParam("Email", o => o.MapFrom(t => t.User.Email))
            .ForCtorParam("Subjects", o => o.MapFrom(t => t.TeacherSubjects.Select(ts => ts.Subject.Name)));
    }

    private void MapAdmins()
    {
        CreateMap<Admin, Dtos.Admin.AdminListDto>()
            .ForCtorParam("FirstName", o => o.MapFrom(a => a.User.FirstName))
            .ForCtorParam("LastName", o => o.MapFrom(a => a.User.LastName))
            .ForCtorParam("Email", o => o.MapFrom(a => a.User.Email))
            .ForCtorParam("SchoolName", o => o.MapFrom(a => a.School.Name))
            // /api/admins is SuperAdmin-only, so this is never stripped.
            .ForCtorParam("Account", o => o.MapFrom(a =>
                new Dtos.Account.AccountDto(a.UserId, a.User.DeactivatedAt)));

    }

    private void MapClasses()
    {
        CreateWriteMap<Dtos.Class.ClassCreateDto, Class>();
        CreateWriteMap<Dtos.Class.ClassEditDto, Class>();
        CreateWriteMap<Dtos.ClassSubjectTeacher.ClassSubjectTeacherCreateDto, ClassSubjectTeacher>();
        CreateWriteMap<Dtos.Enrollment.EnrollmentCreateDto, Enrollment>();
    }
    private void MapLessonRecords()
    {
        CreateWriteMap<Dtos.DiaryEntry.DiaryEntryCreateDto, DiaryEntry>();
        CreateWriteMap<Dtos.DiaryEntry.DiaryEntryEditDto, DiaryEntry>();
        // IsPresent and IsExcused need no ForMember(Ignore) here: they have private setters, so
        // CreateWriteMap skips them on its own. They are set through Attendance.Record, which
        // is what keeps "excused presence" from ever being stored. See AttendanceService.
        CreateWriteMap<Dtos.Attendance.AttendanceEditDto, Attendance>();
        CreateWriteMap<Dtos.Mark.MarkUpsertDto, Mark>();
    }
}
