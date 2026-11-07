

using BeasiswaIn.Web.Common.Enums;

namespace BeasiswaIn.Web.Infrastructures.Persistance.Exceptions;

internal sealed class FieldTooLongException : DbExceptionBase
{
    public FieldTooLongException(string message) : base(message)
    {
    }

    public override ErrorType ErrorType => ErrorType.BadRequest;
}