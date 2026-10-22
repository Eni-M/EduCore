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

    // An inbound map writes only the members it names; everything else on the entity is ignored.
    // Copying every same-named property instead meant a field added to a DTO later -- a SchoolId,
    // a PersonalNumber -- would quietly become writable by whoever can call that endpoint. Each
    // list is the review: to let a request set a field, add it here.
    private IMappingExpression<TSource, TDestination> CreateWriteMap<TSource, TDestination>(params string[] writable)
    {
        var sourceMembers = typeof(TSource).GetProperties().Select(p => p.Name).ToHashSet();

        foreach (var name in writable)
            if (!sourceMembers.Contains(name)
                || typeof(TDestination).GetProperty(name)?.SetMethod is not { IsPublic: true })
                throw new InvalidOperationException(
                    $"{typeof(TSource).Name} -> {typeof(TDestination).Name}: '{name}' is not on both types.");

        var map = CreateMap<TSource, TDestination>();

        foreach (var member in typeof(TDestination).GetProperties()
                     .Where(p => p.CanWrite && !writable.Contains(p.Name)))
            map.ForMember(member.Name, o => o.Ignore());

        return map;
    }

    private void MapSchools()
    {
        CreateMap<School, Dtos.School.SchoolListDto>()
            .ForCtorParam("CityName", o => o.MapFrom(s => s.City != null ? s.City.Name : null))
            .ForCtorParam("ZoneName", o => o.MapFrom(s => s.Zone != null ? s.Zone.Name : null))
            .ForCtorParam("StateName", o => o.MapFrom(s => s.City != null ? s.City.State.Name : null));
        CreateWriteMap<Dtos.School.SchoolCreateDto, School>(nameof(School.Name), nameof(School.Type), nameof(School.CityId), nameof(School.ZoneId));
        CreateWriteMap<Dtos.School.SchoolEditDto, School>(nameof(School.Name), nameof(School.Type), nameof(School.CityId), nameof(School.ZoneId));
    }

    private void MapSubjects()
    {
        CreateMap<Subject, Dtos.Subject.SubjectListDto>();
        CreateWriteMap<Dtos.Subject.SubjectCreateDto, Subject>(nameof(Subject.Name));
        CreateWriteMap<Dtos.Subject.SubjectEditDto, Subject>(nameof(Subject.Name));

        CreateMap<TeacherSubject, Dtos.TeacherSubject.TeacherSubjectListDto>()
            .ForCtorParam("SubjectName", o => o.MapFrom(ts => ts.Subject.Name));

        CreateWriteMap<Dtos.TeacherSubject.TeacherSubjectCreateDto, TeacherSubject>(nameof(TeacherSubject.TeacherId), nameof(TeacherSubject.SubjectId));
    }

    private void MapAcademicCalendar()
    {
        CreateMap<AcademicYear, Dtos.AcademicYear.AcademicYearListDto>()
            .ForCtorParam("SchoolName", o => o.MapFrom(_ => (string?)null));
        CreateWriteMap<Dtos.AcademicYear.AcademicYearCreateDto, AcademicYear>(nameof(AcademicYear.Name), nameof(AcademicYear.StartDate), nameof(AcademicYear.EndDate));
    }

    private void MapUsers()
    {
        CreateWriteMap<Dtos.Student.StudentCreateDto, User>(nameof(User.FirstName), nameof(User.LastName), nameof(User.Email));
        CreateWriteMap<Dtos.Teacher.TeacherCreateDto, User>(nameof(User.FirstName), nameof(User.LastName), nameof(User.Email));
        CreateWriteMap<Dtos.Directorate.DirectorateCreateDto, User>(nameof(User.FirstName), nameof(User.LastName), nameof(User.Email));

        CreateWriteMap<Dtos.Student.StudentEditDto, User>(nameof(User.FirstName), nameof(User.LastName), nameof(User.Email));
        CreateWriteMap<Dtos.Teacher.TeacherEditDto, User>(nameof(User.FirstName), nameof(User.LastName), nameof(User.Email));
        CreateWriteMap<Dtos.Directorate.DirectorateEditDto, User>(nameof(User.FirstName), nameof(User.LastName), nameof(User.Email));
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

        CreateWriteMap<Dtos.Student.StudentCreateDto, Student>(nameof(Student.DateOfBirth), nameof(Student.PersonalNumber), nameof(Student.CountryId), nameof(Student.OriginCityId), nameof(Student.Address), nameof(Student.Gender));
        CreateWriteMap<Dtos.Student.StudentEditDto, Student>(nameof(Student.DateOfBirth), nameof(Student.PersonalNumber), nameof(Student.CountryId), nameof(Student.OriginCityId), nameof(Student.Address), nameof(Student.Gender));
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
        CreateWriteMap<Dtos.Class.ClassCreateDto, Class>(nameof(Class.Name), nameof(Class.AcademicYearId), nameof(Class.Grade), nameof(Class.HomeroomTeacherId), nameof(Class.Capacity), nameof(Class.ShiftId));
        CreateWriteMap<Dtos.Class.ClassEditDto, Class>(nameof(Class.Name), nameof(Class.Grade), nameof(Class.Capacity), nameof(Class.ShiftId));
        CreateWriteMap<Dtos.ClassSubjectTeacher.ClassSubjectTeacherCreateDto, ClassSubjectTeacher>(nameof(ClassSubjectTeacher.ClassId), nameof(ClassSubjectTeacher.SubjectId), nameof(ClassSubjectTeacher.TeacherId));
        CreateWriteMap<Dtos.Enrollment.EnrollmentCreateDto, Enrollment>(nameof(Enrollment.StudentId), nameof(Enrollment.ClassId));
    }
    private void MapLessonRecords()
    {
        CreateWriteMap<Dtos.DiaryEntry.DiaryEntryCreateDto, DiaryEntry>(nameof(DiaryEntry.ClassId), nameof(DiaryEntry.SubjectId), nameof(DiaryEntry.TeacherId), nameof(DiaryEntry.Date), nameof(DiaryEntry.Period), nameof(DiaryEntry.Topic), nameof(DiaryEntry.Notes));
        CreateWriteMap<Dtos.DiaryEntry.DiaryEntryEditDto, DiaryEntry>(nameof(DiaryEntry.Topic), nameof(DiaryEntry.Notes));
        CreateWriteMap<Dtos.Attendance.AttendanceEditDto, Attendance>(nameof(Attendance.Comment), nameof(Attendance.BehaviorMark));
    }
    private void MapStudentRecords()
    {
        CreateMap<SchoolingHistory, Dtos.SchoolingHistory.SchoolingHistoryListDto>();
        CreateWriteMap<Dtos.SchoolingHistory.SchoolingHistoryCreateDto, SchoolingHistory>(nameof(SchoolingHistory.PreviousSchoolName), nameof(SchoolingHistory.City), nameof(SchoolingHistory.FromDate), nameof(SchoolingHistory.ToDate), nameof(SchoolingHistory.GradeOrLevel), nameof(SchoolingHistory.ExitReason), nameof(SchoolingHistory.Notes));
        CreateWriteMap<Dtos.SchoolingHistory.SchoolingHistoryEditDto, SchoolingHistory>(nameof(SchoolingHistory.PreviousSchoolName), nameof(SchoolingHistory.City), nameof(SchoolingHistory.FromDate), nameof(SchoolingHistory.ToDate), nameof(SchoolingHistory.GradeOrLevel), nameof(SchoolingHistory.ExitReason), nameof(SchoolingHistory.Notes));

        CreateMap<StudentDocument, Dtos.StudentDocument.StudentDocumentListDto>();

        CreateMap<StudentMedicalRecord, Dtos.Medical.StudentMedicalRecordDto>();
        CreateWriteMap<Dtos.Medical.StudentMedicalRecordUpsertDto, StudentMedicalRecord>(nameof(StudentMedicalRecord.BloodType), nameof(StudentMedicalRecord.Allergies), nameof(StudentMedicalRecord.ChronicConditions), nameof(StudentMedicalRecord.Medications), nameof(StudentMedicalRecord.EmergencyContactName), nameof(StudentMedicalRecord.EmergencyContactPhone), nameof(StudentMedicalRecord.Notes));
    }
    private void MapCommunication()
    {
        // The list DTOs are built by their repositories (they join for author/class/student
        // names), so only the write maps live here.
        CreateWriteMap<Dtos.Calendar.CalendarEventCreateDto, CalendarEvent>(nameof(CalendarEvent.Title), nameof(CalendarEvent.Description), nameof(CalendarEvent.Type), nameof(CalendarEvent.Audience), nameof(CalendarEvent.ClassId), nameof(CalendarEvent.StudentId), nameof(CalendarEvent.Date), nameof(CalendarEvent.StartTime), nameof(CalendarEvent.EndTime));
        CreateWriteMap<Dtos.Calendar.CalendarEventEditDto, CalendarEvent>(nameof(CalendarEvent.Title), nameof(CalendarEvent.Description), nameof(CalendarEvent.Type), nameof(CalendarEvent.Date), nameof(CalendarEvent.StartTime), nameof(CalendarEvent.EndTime));
    }
}
