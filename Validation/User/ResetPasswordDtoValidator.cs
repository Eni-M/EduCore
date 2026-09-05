using EduCore.Dtos.User;
using FluentValidation;

namespace EduCore.Validation.User;

public class ResetPasswordDtoValidator : AbstractValidator<ResetPasswordDto>
{
    public ResetPasswordDtoValidator()
    {
        RuleFor(x => x.Token).NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .Length(8, 128)
            .Matches(@"[a-z]").WithMessage("'New Password' must contain a lowercase letter.")
            .Matches(@"[A-Z]").WithMessage("'New Password' must contain an uppercase letter.")
            .Matches(@"\d").WithMessage("'New Password' must contain a digit.");

        RuleFor(x => x.ConfirmNewPassword)
            .NotEmpty()
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}
