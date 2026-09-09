using System.Diagnostics;
using EduCore.Dtos.Errors;
using EduCore.Localization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EduCore.Exceptions;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IMessageLocalizer localizer) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Prefer the W3C trace id over ASP.NET's TraceIdentifier. Both identify the request, but
        // only this one is the id the trace is filed under in whatever collects traces -- so when
        // a user reports "it said error a1b2c3", that string leads straight to the recorded
        // request, its database statements and its exception. TraceIdentifier is a per-process
        // connection:request counter that means nothing outside this instance's own log file.
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        // Marks the span itself as failed, so the trace shows up in an "errors only" view rather
        // than having to be found by timestamp.
        if (Activity.Current is { } activity)
        {
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity.AddException(exception);
        }

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

        // Translated here rather than at the 240 throw sites: every message the API sends a
        // person passes through this one place, so the services stay readable in English.
        var errorResponse = new ErrorResponseDto(
            StatusCode: statusCode,
            Timestamp: DateTime.UtcNow,
            Message: localizer.Translate(message),
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
