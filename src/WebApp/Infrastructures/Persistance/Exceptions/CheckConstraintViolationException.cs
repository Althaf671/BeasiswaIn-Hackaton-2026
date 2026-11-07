

using BeasiswaIn.Web.Common.Enums;

namespace BeasiswaIn.Web.Infrastructures.Persistance.Exceptions;

internal sealed class CheckConstraintViolationException : DbExceptionBase
{
    public CheckConstraintViolationException(string message) : base(message)
    {
    }

    public override ErrorType ErrorType => ErrorType.BadRequest;
}