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
        MapAccountTiers();
        MapClasses();
        MapLessonRecords();
        MapStudentRecords();
        MapCommunication();
    }

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
        CreateMap<School, Dtos.School.SchoolListDto>()
            .ForCtorParam("CityName", o => o.MapFrom(s => s.City != null ? s.City.Name : null))
            .ForCtorParam("ZoneName", o => o.MapFrom(s => s.Zone != null ? s.Zone.Name : null))
            .ForCtorParam("StateName", o => o.MapFrom(s => s.City != null ? s.City.State.Name : null));
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
        CreateMap<AcademicYear, Dtos.AcademicYear.AcademicYearListDto>()
            .ForCtorParam("SchoolName", o => o.MapFrom(_ => (string?)null));
        CreateWriteMap<Dtos.AcademicYear.AcademicYearCreateDto, AcademicYear>();
    }

    private void MapUsers()
    {
        CreateWriteMap<Dtos.Student.StudentCreateDto, User>();
        CreateWriteMap<Dtos.Teacher.TeacherCreateDto, User>();
        CreateWriteMap<Dtos.Directorate.DirectorateCreateDto, User>();

        CreateWriteMap<Dtos.Student.StudentEditDto, User>();
        CreateWriteMap<Dtos.Teacher.TeacherEditDto, User>();
        CreateWriteMap<Dtos.Directorate.DirectorateEditDto, User>();
    }

    private void MapStudents()
    {
        CreateMap<GuardianStudent, Dtos.Guardian.GuardianContactDto>()
            .ForCtorParam("Id", o => o.MapFrom(gs => gs.Guardian.Id))
            .ForCtorParam("FirstName", o => o.MapFrom(gs => gs.Guardian.User.FirstName))
            .ForCtorParam("LastName", o => o.MapFrom(gs => gs.Guardian.User.LastName))
            .ForCtorParam("Email", o => o.MapFrom(gs => gs.Guardian.User.Email))
            .ForCtorParam("NationalId", o => o.MapFrom(gs => gs.Guardian.NationalId))
            .ForCtorParam("PhoneNumber", o => o.MapFrom(gs => gs.Guardian.PhoneNumber));

        CreateMap<Student, Dtos.Student.StudentListDto>()
            .ForCtorParam("FirstName", o => o.MapFrom(s => s.User.FirstName))
            .ForCtorParam("LastName", o => o.MapFrom(s => s.User.LastName))
            .ForCtorParam("Email", o => o.MapFrom(s => s.User.Email))
            .ForCtorParam("IsGraduated", o => o.MapFrom(s => s.GraduationDate != null))
            .ForCtorParam("Account", o => o.MapFrom(s =>
                new Dtos.Account.AccountDto(s.UserId, s.User.DeactivatedAt)));

        CreateMap<Student, Dtos.Student.StudentDetailDto>()
            .ForCtorParam("FirstName", o => o.MapFrom(s => s.User.FirstName))
            .ForCtorParam("LastName", o => o.MapFrom(s => s.User.LastName))
            .ForCtorParam("Email", o => o.MapFrom(s => s.User.Email))
            .ForCtorParam("CountryName", o => o.MapFrom(s => s.Country != null ? s.Country.Name : null))
            .ForCtorParam("OriginCityName", o => o.MapFrom(s => s.OriginCity != null ? s.OriginCity.Name : null))
            .ForCtorParam("Guardians", o => o.MapFrom(s => s.Guardians));

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

    private void MapAccountTiers()
    {
        CreateMap<Directorate, Dtos.Directorate.DirectorateListDto>()
            .ForCtorParam("FirstName", o => o.MapFrom(a => a.User.FirstName))
            .ForCtorParam("LastName", o => o.MapFrom(a => a.User.LastName))
            .ForCtorParam("Email", o => o.MapFrom(a => a.User.Email))
            .ForCtorParam("SchoolName", o => o.MapFrom(a => a.School.Name))
            .ForCtorParam("Account", o => o.MapFrom(a =>
                new Dtos.Account.AccountDto(a.UserId, a.User.DeactivatedAt)));

        CreateMap<RegionalAdmin, Dtos.RegionalAdmin.RegionalAdminListDto>()
            .ForCtorParam("FirstName", o => o.MapFrom(r => r.User.FirstName))
            .ForCtorParam("LastName", o => o.MapFrom(r => r.User.LastName))
            .ForCtorParam("Email", o => o.MapFrom(r => r.User.Email))
            .ForCtorParam("CityName", o => o.MapFrom(r => r.City.Name))
            .ForCtorParam("StateName", o => o.MapFrom(r => r.City.State.Name))
            .ForCtorParam("Account", o => o.MapFrom(r =>
                new Dtos.Account.AccountDto(r.UserId, r.User.DeactivatedAt)));

        CreateMap<StateAdmin, Dtos.StateAdmin.StateAdminListDto>()
            .ForCtorParam("FirstName", o => o.MapFrom(s => s.User.FirstName))
            .ForCtorParam("LastName", o => o.MapFrom(s => s.User.LastName))
            .ForCtorParam("Email", o => o.MapFrom(s => s.User.Email))
            .ForCtorParam("StateName", o => o.MapFrom(s => s.State.Name))
            .ForCtorParam("Account", o => o.MapFrom(s =>
                new Dtos.Account.AccountDto(s.UserId, s.User.DeactivatedAt)));
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
        CreateWriteMap<Dtos.Attendance.AttendanceEditDto, Attendance>();
        CreateWriteMap<Dtos.Mark.MarkUpsertDto, Mark>();
    }
    private void MapStudentRecords()
    {
        CreateMap<SchoolingHistory, Dtos.SchoolingHistory.SchoolingHistoryListDto>();
        CreateWriteMap<Dtos.SchoolingHistory.SchoolingHistoryCreateDto, SchoolingHistory>();
        CreateWriteMap<Dtos.SchoolingHistory.SchoolingHistoryEditDto, SchoolingHistory>();

        CreateMap<StudentDocument, Dtos.StudentDocument.StudentDocumentListDto>();

        CreateMap<StudentMedicalRecord, Dtos.Medical.StudentMedicalRecordDto>();
        CreateWriteMap<Dtos.Medical.StudentMedicalRecordUpsertDto, StudentMedicalRecord>();
    }
    private void MapCommunication()
    {
        // The list DTOs are built by their repositories (they join for author/class/student
        // names), so only the write maps live here.
        CreateWriteMap<Dtos.Calendar.CalendarEventCreateDto, CalendarEvent>();
        CreateWriteMap<Dtos.Calendar.CalendarEventEditDto, CalendarEvent>();
    }
}
