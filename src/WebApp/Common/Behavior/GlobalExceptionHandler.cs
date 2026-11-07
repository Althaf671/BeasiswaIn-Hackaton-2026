
 
using Microsoft.AspNetCore.Diagnostics;
using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Errors;
using BeasiswaIn.Web.Common.Helpers;

namespace BeasiswaIn.Web.Common.Behavior;

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is ExceptionBase appException)
        {
            _logger.LogWarning(exception, "Handled application exception occured.");

            var error = Error.Create(
                appException.ErrorType,
                appException.GetType().Name,
                appException.Message,
                appException.Domain);

            var problemDetails = Result.Failure(error)
                .ToProblemDetailsObject(httpContext);

            httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception occurred.");

            var error = _environment.IsDevelopment()
                ? SystemError.InternalServerError with { Description = exception.Message }
                : SystemError.InternalServerError;

            var problemDetails = Result.Failure(error)
                .ToProblemDetailsObject(httpContext);

            httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;     
        }
    }
}