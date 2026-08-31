using EduCore.Dtos.Errors;
using EduCore.Exceptions;
using FluentValidation;

namespace EduCore.Endpoints;

public class ValidationEndpointFilter(IServiceProvider services) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var errors = new List<FieldError>();

        foreach (var argument in context.Arguments)
        {
            if (argument is null || argument.GetType().Namespace?.StartsWith("EduCore.Dtos") != true)
                continue;

            if (services.GetService(typeof(IValidator<>).MakeGenericType(argument.GetType())) is not IValidator validator)
                continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument));

            errors.AddRange(result.Errors.Select(e => new FieldError
            {
                Field = e.PropertyName,
                Message = e.ErrorMessage
            }));
        }

        if (errors.Count > 0)
            throw new RequestValidationException(errors);

        return await next(context);
    }
}
