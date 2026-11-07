

using BeasiswaIn.Web.Common.Enums;

namespace BeasiswaIn.Web.Common.Helpers;

internal static class ErrorMapper
{
    public static int MapToHttpStatusCode(ErrorType type)
    {
        return type switch
        {
            ErrorType.BadRequest => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.ServiceUnavailable => StatusCodes.Status503ServiceUnavailable,
            ErrorType.TooManyRequests => StatusCodes.Status429TooManyRequests,
            ErrorType.Locked => StatusCodes.Status423Locked,
            ErrorType.RequestTimeout => StatusCodes.Status408RequestTimeout,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}