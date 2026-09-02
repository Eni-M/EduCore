using EduCore.Dtos.Guardian;
using FluentValidation;

namespace EduCore.Validation.Guardian;

public class GuardianLinksDtoValidator : AbstractValidator<GuardianLinksDto>
{
    public GuardianLinksDtoValidator()
    {
        RuleFor(x => x.StudentIds).NotEmpty();
        RuleForEach(x => x.StudentIds).NotEmpty();
    }
}
