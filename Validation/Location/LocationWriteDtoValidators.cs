using EduCore.Dtos.Location;
using FluentValidation;

namespace EduCore.Validation.Location;

public class StateWriteDtoValidator : AbstractValidator<StateWriteDto>
{
    public StateWriteDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
    }
}

public class CityWriteDtoValidator : AbstractValidator<CityWriteDto>
{
    public CityWriteDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.StateId).NotEmpty();
    }
}
