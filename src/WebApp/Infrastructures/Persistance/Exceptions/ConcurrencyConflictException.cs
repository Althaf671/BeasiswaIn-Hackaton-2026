

using BeasiswaIn.Web.Common.Enums;

namespace BeasiswaIn.Web.Infrastructures.Persistance.Exceptions;

internal sealed class ConcurrencyConflictException : DbExceptionBase
{
    public ConcurrencyConflictException(string message) : base(message)
    {
    }

    public override ErrorType ErrorType => ErrorType.Conflict;
}