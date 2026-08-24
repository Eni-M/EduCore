using EduCore.Dtos.User;
using FluentValidation;

namespace EduCore.Validation.User;

public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordDtoValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .Length(8, 128)
            .Matches(@"[a-z]").WithMessage("'New Password' must contain a lowercase letter.")
            .Matches(@"[A-Z]").WithMessage("'New Password' must contain an uppercase letter.")
            .Matches(@"\d").WithMessage("'New Password' must contain a digit.")
            .NotEqual(x => x.CurrentPassword).WithMessage("'New Password' must differ from the current password.");

        RuleFor(x => x.ConfirmNewPassword)
            .NotEmpty()
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}
