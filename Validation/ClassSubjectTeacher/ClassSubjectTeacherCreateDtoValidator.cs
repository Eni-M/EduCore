using EduCore.Dtos.ClassSubjectTeacher;
using FluentValidation;

namespace EduCore.Validation.ClassSubjectTeacher;

public class ClassSubjectTeacherCreateDtoValidator : AbstractValidator<ClassSubjectTeacherCreateDto>
{
    public ClassSubjectTeacherCreateDtoValidator()
    {
        RuleFor(x => x.ClassId).NotEmpty();
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.TeacherId).NotEmpty();
    }
}
