

using BeasiswaIn.Web.Common.Enums;

namespace BeasiswaIn.Web.Common.Abstractions;

internal abstract class ExceptionBase : Exception
{
    public abstract ErrorType ErrorType { get; }
    public abstract string Domain { get; }

    protected ExceptionBase(string message): base(message) {}
}