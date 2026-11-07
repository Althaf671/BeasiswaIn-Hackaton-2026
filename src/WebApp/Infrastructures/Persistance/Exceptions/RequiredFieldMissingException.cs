

using BeasiswaIn.Web.Common.Enums;

namespace BeasiswaIn.Web.Infrastructures.Persistance.Exceptions;

internal sealed class RequiredFieldMissingException : DbExceptionBase
{
    public RequiredFieldMissingException(string message) : base(message)
    {
    }

    public override ErrorType ErrorType => ErrorType.BadRequest;
}