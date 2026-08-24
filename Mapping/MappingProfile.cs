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

    // An inbound DTO is only ever allowed to set the fields it actually carries. Anything on
    // the entity the DTO doesn't name -- surrogate keys, SchoolId, audit/concurrency fields,
    // navigations -- is ignored here and stays the service's decision. Doing it by name
    // means a new entity column can't silently become client-writable just because someone
    // added a same-named property to a DTO's neighbour.
    private IMappingExpression<TSource, TDestination> CreateWriteMap<TSource, TDestination>()
    {
        var sourceMembers = typeof(TSource)
            .GetProperties()
            .Select(p => p.Name)
            .ToHashSet();

        var map = CreateMap<TSource, TDestination>();

        foreach (var member in typeof(TDestination).GetProperties()
                     .Where(p => p.CanWrite && !sourceMembers.Contains(p.Name)))
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
        CreateMap<AcademicYear, Dtos.AcademicYear.AcademicYearListDto>();
        CreateWriteMap<Dtos.AcademicYear.AcademicYearCreateDto, AcademicYear>();
        CreateWriteMap<Dtos.GradingPeriod.GradingPeriodCreateDto, GradingPeriod>();
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
            .ForCtorParam("IsGraduated", o => o.MapFrom(s => s.GraduationDate != null));

        CreateMap<Student, Dtos.Student.StudentDetailDto>()
            .ForCtorParam("FirstName", o => o.MapFrom(s => s.User.FirstName))
            .ForCtorParam("LastName", o => o.MapFrom(s => s.User.LastName))
            .ForCtorParam("Email", o => o.MapFrom(s => s.User.Email));

        CreateWriteMap<Dtos.Student.StudentCreateDto, Student>();
        CreateWriteMap<Dtos.Student.StudentEditDto, Student>();
    }

    private void MapTeachers()
    {
        CreateMap<Teacher, Dtos.Teacher.TeacherListDto>()
            .ForCtorParam("FirstName", o => o.MapFrom(t => t.User.FirstName))
            .ForCtorParam("LastName", o => o.MapFrom(t => t.User.LastName))
            .ForCtorParam("Email", o => o.MapFrom(t => t.User.Email));

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
            .ForCtorParam("SchoolName", o => o.MapFrom(a => a.School.Name));

        CreateWriteMap<Dtos.Admin.AdminCreateDto, Admin>();
        CreateWriteMap<Dtos.Admin.AdminEditDto, Admin>();
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
        CreateWriteMap<Dtos.Attendance.AttendanceCreateDto, Attendance>();
        CreateWriteMap<Dtos.Attendance.AttendanceEditDto, Attendance>();
        CreateWriteMap<Dtos.Mark.MarkCreateDto, Mark>();
    }
}
