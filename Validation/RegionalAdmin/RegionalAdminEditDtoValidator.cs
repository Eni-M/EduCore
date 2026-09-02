using EduCore.Dtos.RegionalAdmin;
using FluentValidation;

namespace EduCore.Validation.RegionalAdmin;

public class RegionalAdminEditDtoValidator : AbstractValidator<RegionalAdminEditDto>
{
    public RegionalAdminEditDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.CityId).NotEmpty();
    }
}
