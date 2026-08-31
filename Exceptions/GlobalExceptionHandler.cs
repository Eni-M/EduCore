using EduCore.Dtos.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EduCore.Exceptions;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = httpContext.TraceIdentifier;

        if (exception is DbUpdateException dbEx && dbEx.InnerException is PostgresException { SqlState: "23505" } pgEx)
        {
            logger.LogWarning(
                "Unique violation on constraint {Constraint}. TraceId: {TraceId}", pgEx.ConstraintName, traceId);

            exception = new ConflictException("A record with the same details already exists.");
        }

        var (statusCode, error, message) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found", exception.Message),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized", exception.Message),
            RequestValidationException => (StatusCodes.Status400BadRequest, "Validation Failed", exception.Message),
            BusinessException => (StatusCodes.Status400BadRequest, "Business Error", exception.Message),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict", exception.Message),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden", exception.Message),
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "Conflict",
                "This record was changed by someone else. Reload it and try again."),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", traceId);
        }
        else
        {
            logger.LogWarning("{Error}: {Message}. TraceId: {TraceId}", error, exception.Message, traceId);
        }

        var errorResponse = new ErrorResponseDto(
            StatusCode: statusCode,
            Timestamp: DateTime.UtcNow,
            Message: message,
            Error: error,
            TraceId: traceId,
            Path: httpContext.Request.Path,
            FieldErrors: (exception as RequestValidationException)?.FieldErrors
        );

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);

        return true;
    }
}
