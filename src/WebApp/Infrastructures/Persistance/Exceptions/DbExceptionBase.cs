

using BeasiswaIn.Web.Common.Abstractions;

namespace BeasiswaIn.Web.Infrastructures.Persistance.Exceptions;

internal abstract class DbExceptionBase : ExceptionBase
{
    protected DbExceptionBase(string message) : base(message)
    {
    }

    public override string Domain => "Database";
}