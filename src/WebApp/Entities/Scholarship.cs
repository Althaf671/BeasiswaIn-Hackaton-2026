using BeasiswaIn.Web.Common.Abstractions;

namespace BeasiswaIn.Web.Entities;

internal sealed class Scholarship : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal TotalPrize { get; set; }
    public string ContentImageUrl { get; set; } = string.Empty;
    public string ContentImagePublicId { get; set; } = string.Empty;
    public string GuideUrl { get; set; } = string.Empty;
    public string RegistrationUrl { get; set; } = string.Empty;
    public List<string> Requirement { get; set; } = [];
    public List<string> ContactPerson { get; set; } = [];
    public bool IsEligible { get; set; }
    public DateTimeOffset RegistrationStartAt { get; set; }
    public DateTimeOffset RegistrationEndAt { get; set; }
    public bool IsBookmarked { get; set; }
    public int AiRecommendationOpenCount { get; set; }
    public int UserOpenCount { get; set; }

    public Scholarship(
        string id,
        string name,
        string description,
        decimal totalPrize,
        string contentImageUrl,
        string contentImagePublicId,
        string guideUrl,
        string registrationUrl,
        List<string> requirement,
        List<string> contactPerson,
        bool isEligible,
        DateTimeOffset registrationStartAt,
        DateTimeOffset registrationEndAt,
        bool isBookmarked) : base(id)
    {
        Name = name;
        Description = description;
        TotalPrize = totalPrize;
        ContentImageUrl = contentImageUrl;
        ContentImagePublicId = contentImagePublicId;
        GuideUrl = guideUrl;
        RegistrationUrl = registrationUrl;
        Requirement = requirement;
        ContactPerson = contactPerson;
        IsEligible = isEligible;
        RegistrationStartAt = registrationStartAt;
        RegistrationEndAt = registrationEndAt;
        IsBookmarked = isBookmarked;
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
