

using BeasiswaIn.Web.Common.Enums;

namespace BeasiswaIn.Web.Common.Abstractions;

public sealed record Error
{
    // attributes
    internal static readonly Error None = new(ErrorType.None, string.Empty, null, null); 

    // properties
    internal ErrorType ErrorType { get; init; }
    internal string Code { get; init; }
    internal string? Description { get; init; } = null;
    internal string? Domain { get; init; } = null;
    internal string? Field { get; init; }

    // constructor
    private Error(ErrorType errorType, string code, string? description, string? domain)
    {
        ErrorType = errorType;
        Code = code;
        Description = description;
        Domain = domain;
    }

    // factory
    internal static Error Create(ErrorType errorType, string code, string description, string domain)
    {
        return new(errorType, $"{domain}Errors.{code}", description, domain);
    }

    public override string ToString()
    {
        return 
        $@"
        Error:
            ErrorType = {ErrorType};
            Code = {Code};
            Description = {Description};
            Domain = {Domain};
        ";
    }
}