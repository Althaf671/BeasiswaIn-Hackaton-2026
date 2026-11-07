using BeasiswaIn.Web.Common.Abstractions;

namespace BeasiswaIn.Web.Entities;

internal sealed class ScholarshipCategory : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public ScholarshipCategory(string id, string name, bool isActive) : base(id)
    {
        Name = name;
        IsActive = isActive;
    }

    public void MarkItAsUpdated()
    {
        MarkAsUpdated();
    }

    public void MarkItAsDeleted()
    {
        MarkAsDeleted();
    }
}
