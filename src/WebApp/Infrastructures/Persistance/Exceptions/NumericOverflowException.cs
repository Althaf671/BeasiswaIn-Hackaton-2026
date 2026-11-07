

using BeasiswaIn.Web.Common.Enums;

namespace BeasiswaIn.Web.Infrastructures.Persistance.Exceptions;

internal sealed class NumericOverflowException : DbExceptionBase
{
    public NumericOverflowException(string message) : base(message)
    {
    }

    public override ErrorType ErrorType => ErrorType.BadRequest;
}