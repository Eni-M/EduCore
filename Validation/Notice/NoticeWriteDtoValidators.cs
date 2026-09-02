using EduCore.Dtos.Notice;
using FluentValidation;

namespace EduCore.Validation.Notice;

public class NoticeCreateDtoValidator : AbstractValidator<NoticeCreateDto>
{
    public NoticeCreateDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(8000);
    }
}

public class NoticeEditDtoValidator : AbstractValidator<NoticeEditDto>
{
    public NoticeEditDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(8000);
    }
}
