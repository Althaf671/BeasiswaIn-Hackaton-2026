using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace BeasiswaIn.Web.Models.Chatbot;

public sealed record ChatRequest : IValidatableObject
{
    [Required, MaxLength(12000)]
    public string Message { get; init; } = string.Empty;

    [Required, MaxLength(12)]
    public List<ChatHistoryMessage> History { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (History is null) yield break;
        if ((long)(Message?.Length ?? 0) + History.Sum(item => (long)(item?.Content?.Length ?? 0)) > 48000)
            yield return new ValidationResult("Riwayat terlalu panjang. Mulai percakapan baru.", [nameof(History)]);
        if (History.Any(item => item is null))
            yield return new ValidationResult("Riwayat tidak valid.", [nameof(History)]);
    }
}

public sealed record ChatHistoryMessage : IValidatableObject
{
    [Required, RegularExpression("^(user|assistant)$")]
    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;

    [Required, MaxLength(24000)]
    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Role == "user" && Content?.Length > 12000)
            yield return new ValidationResult("Pesan pengguna maksimal 12.000 karakter.", [nameof(Content)]);
    }
}
