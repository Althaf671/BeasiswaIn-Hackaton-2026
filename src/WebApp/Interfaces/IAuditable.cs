namespace BeasiswaIn.Web.Interfaces;

public interface IAuditable
{
    public DateTimeOffset? UpdatedAt { get; }
    public DateTimeOffset CreatedAt { get; }
}