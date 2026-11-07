
 
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BeasiswaIn.Web.Common.Abstractions;

namespace BeasiswaIn.Web.Common.Helpers;

internal static class ResultExtension
{
    private const string ProblemTypeBaseUrl = "https://PhotographyStudioCms/errors";

    internal static IActionResult ToMessageResponse(string message, int status)
        => new ObjectResult(message) { StatusCode = status };

    internal static ProblemDetails ToProblemDetailsObject(this Result result, HttpContext httpContext)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Tidak bisa memanggil ToProblemDetails ketika result sukses.");

        var error = result.Error;
        var statusCode = ErrorMapper.MapToHttpStatusCode(error.ErrorType);

        return new ProblemDetails
        {
            Type = BuildProblemType(error),
            Title = StringHelper.ToProblemTitle(error.Code),
            Status = statusCode,
            Detail = error.Description,
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier
            }
        };
    }

    internal static IActionResult ToProblemDetails(this Result result, HttpContext httpContext)
    {
        var problemDetails = result.ToProblemDetailsObject(httpContext);
        return new ObjectResult(problemDetails) { StatusCode = problemDetails.Status };
    }

    private static string BuildProblemType(Error error)
    {
        var reason = error.Code.Contains('.')
            ? error.Code[(error.Code.IndexOf('.') + 1)..]
            : error.Code;

        var domainSegment = (error.Domain ?? "general").ToLowerInvariant();
        var reasonSegment = reason.ToKebabCase;

        return $"{ProblemTypeBaseUrl}/{domainSegment}/{reasonSegment}";
    }
}