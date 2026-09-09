using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SkyRoute.Application.Exceptions;

namespace SkyRoute.WebApi.Middleware;

// Maps the Application exception types from Phase 3 to HTTP status codes, per
// docs/03-execution-plan.md Phase 5, task 5: unknown airport / same origin-destination /
// invalid passenger count -> 400; flight not found -> 404; offer expired -> 409; anything
// unexpected -> 500 with a generic ProblemDetails body (no stack trace leaked).
public sealed class ProblemDetailsExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title, extensions) = exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "One or more validation errors occurred.",
                new Dictionary<string, object?> { ["errors"] = validationException.Errors }),

            FlightNotFoundException flightNotFound => (
                StatusCodes.Status404NotFound,
                flightNotFound.Message,
                null),

            OfferExpiredException offerExpired => (
                StatusCodes.Status409Conflict,
                offerExpired.Message,
                null),

            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                null)
        };

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title
        };

        if (extensions is not null)
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
}
