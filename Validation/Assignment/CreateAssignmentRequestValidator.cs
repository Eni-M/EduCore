using EduCore.Dtos.Assignment;
using FluentValidation;

namespace EduCore.Validation.Assignment;

public class CreateAssignmentRequestValidator
    : AbstractValidator<CreateAssignmentRequest>
{
    public CreateAssignmentRequestValidator()
    {
        RuleFor(x => x.ClassId)
            .NotEmpty();

        RuleFor(x => x.SubjectId)
            .NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(8000);

        RuleFor(x => x.DueDate)
            .NotEmpty();
    }
}