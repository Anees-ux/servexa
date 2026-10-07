using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Servexa.Application.Exceptions;
using Servexa.Domain.Exceptions;

namespace Servexa.Api.Infrastructure;

/// <summary>
/// Centralized ASP.NET Core exception handler translating application and domain exceptions into RFC 7807 / RFC 9110 ProblemDetails.
/// </summary>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail, type, extensions) = MapException(exception);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Unhandled exception occurred. TraceId: {TraceId}. Message: {Message}",
                httpContext.TraceIdentifier,
                exception.Message);
        }
        else
        {
            logger.LogWarning(
                "Handled domain/application exception [{ExceptionType}] for TraceId: {TraceId}. StatusCode: {StatusCode}. Message: {Message}",
                exception.GetType().Name,
                httpContext.TraceIdentifier,
                statusCode,
                exception.Message);
        }

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (extensions != null)
        {
            foreach (var (key, value) in extensions)
            {
                problemDetails.Extensions[key] = value;
            }
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }

    private static (int StatusCode, string Title, string Detail, string Type, IDictionary<string, object?>? Extensions) MapException(Exception exception)
    {
        return exception switch
        {
            FluentValidation.ValidationException fluentValidationEx => (
                StatusCodes.Status400BadRequest,
                "Validation Error",
                "One or more validation errors occurred.",
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1",
                new Dictionary<string, object?>
                {
                    ["errors"] = fluentValidationEx.Errors
                        .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
                        .ToDictionary(g => g.Key, g => g.ToArray())
                }
            ),

            ValidationException appValidationEx => (
                StatusCodes.Status400BadRequest,
                "Validation Error",
                appValidationEx.Message,
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1",
                new Dictionary<string, object?>
                {
                    ["errors"] = appValidationEx.Errors
                }
            ),

            NotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                "Not Found",
                notFoundEx.Message,
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5",
                null
            ),

            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "Authentication is required to access this resource.",
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.2",
                null
            ),

            ForbiddenAccessException forbiddenEx => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                forbiddenEx.Message,
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.4",
                null
            ),

            ConcurrencyConflictException concurrencyEx => (
                StatusCodes.Status409Conflict,
                "Concurrency Conflict",
                concurrencyEx.Message,
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.10",
                null
            ),

            BusinessRuleConflictException businessRuleEx => (
                StatusCodes.Status409Conflict,
                "Business Rule Violation",
                businessRuleEx.Message,
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.10",
                null
            ),

            IdempotencyConflictException idempotencyEx => (
                StatusCodes.Status409Conflict,
                "Idempotency Conflict",
                idempotencyEx.Message,
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.10",
                null
            ),

            ConflictException conflictEx => (
                StatusCodes.Status409Conflict,
                "Conflict",
                conflictEx.Message,
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.10",
                null
            ),

            DomainException domainEx => (
                StatusCodes.Status409Conflict,
                "Domain Rule Violation",
                domainEx.Message,
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.10",
                null
            ),

            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "An unexpected error occurred while processing your request. Please contact support.",
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1",
                null
            )
        };
    }
}
