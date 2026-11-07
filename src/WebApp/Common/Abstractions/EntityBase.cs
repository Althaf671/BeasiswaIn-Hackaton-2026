

using BeasiswaIn.Web.Interfaces;

namespace BeasiswaIn.Web.Common.Abstractions;

internal abstract class EntityBase : IEntity
{
    public string Id { get; private set; } = string.Empty;
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public uint RowVersion { get; private set; }

    protected EntityBase() {} 

    protected EntityBase(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) 
            throw new ArgumentException("ID tidak boleh kosong.", nameof(id));
            
        Id = id;
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = null;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    protected void MarkAsUpdated()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    protected void MarkAsDeleted()
    {
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
    }
}