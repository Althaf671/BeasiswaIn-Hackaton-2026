

namespace BeasiswaIn.Web.Common.Enums;

public enum ErrorType
{
    None = 0,
    BadRequest = 1,
    Unauthorized = 2,
    Forbidden = 3,
    Conflict = 4,
    NotFound = 5,
    ServiceUnavailable = 6,
    TooManyRequests = 7,
    RequestTimeout = 8,
    InternalServerError = 9,
    Locked = 10
}