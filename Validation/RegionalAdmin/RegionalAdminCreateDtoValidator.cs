using EduCore.Dtos.RegionalAdmin;
using EduCore.Validation;
using FluentValidation;

namespace EduCore.Validation.RegionalAdmin;

public class RegionalAdminCreateDtoValidator : AbstractValidator<RegionalAdminCreateDto>
{
    public RegionalAdminCreateDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).Password();
        RuleFor(x => x.CityId).NotEmpty();
    }
}
