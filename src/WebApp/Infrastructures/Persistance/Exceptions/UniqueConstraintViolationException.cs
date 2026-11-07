

using BeasiswaIn.Web.Common.Enums;

namespace BeasiswaIn.Web.Infrastructures.Persistance.Exceptions;

internal sealed class UniqueConstraintViolationException : DbExceptionBase
{
    public UniqueConstraintViolationException(string message) : base(message)
    {
    }

    public override ErrorType ErrorType => ErrorType.Conflict;
}