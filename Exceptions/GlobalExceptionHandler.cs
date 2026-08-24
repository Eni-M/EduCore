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

        // Unwraps EF Core's DbUpdateException to find a raw Postgres unique-violation and
        // reports it as a normal conflict instead of a raw 500 -- needed because several
        // entities (e.g. Student.PersonalNumber) enforce uniqueness only at the DB level.
        if (exception is DbUpdateException dbEx && dbEx.InnerException is PostgresException { SqlState: "23505" } pgEx)
            exception = new ConflictException($"A record with the same {pgEx.ConstraintName} already exists.");

        var (statusCode, error, message) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found", exception.Message),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized", exception.Message),
            RequestValidationException => (StatusCodes.Status400BadRequest, "Validation Failed", exception.Message),
            BusinessException => (StatusCodes.Status400BadRequest, "Business Error", exception.Message),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict", exception.Message),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden", exception.Message),
            InvalidRequestException => (StatusCodes.Status406NotAcceptable, "Invalid Request", exception.Message),
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
