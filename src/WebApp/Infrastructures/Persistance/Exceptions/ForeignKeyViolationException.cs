

using BeasiswaIn.Web.Common.Enums;

namespace BeasiswaIn.Web.Infrastructures.Persistance.Exceptions;

internal sealed class ForeignKeyViolationException : DbExceptionBase
{
    public ForeignKeyViolationException(string message) : base(message)
    {
    }

    public override ErrorType ErrorType => ErrorType.Conflict;
}