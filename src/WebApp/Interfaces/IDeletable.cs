namespace BeasiswaIn.Web.Interfaces;

public interface IDeletable
{
    public bool IsDeleted { get; }
    public DateTimeOffset? DeletedAt { get; }
}