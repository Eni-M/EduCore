using EduCore.Dtos.Errors;
using EduCore.Exceptions;
using FluentValidation;

namespace EduCore.Endpoints;

// Validates any argument that has a FluentValidation validator registered. Added once to the
// api group, so no endpoint has to remember to call it.
public class ValidationEndpointFilter(IServiceProvider services) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var errors = new List<FieldError>();

        foreach (var argument in context.Arguments)
        {
            // Only request bodies are worth looking up: the rest of the arguments are route
            // values, injected services and the cancellation token.
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
