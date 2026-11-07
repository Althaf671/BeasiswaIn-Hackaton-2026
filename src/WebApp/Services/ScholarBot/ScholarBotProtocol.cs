using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeasiswaIn.Web.Services.ScholarBot;

internal static class ScholarBotProtocol
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        MaxDepth = 16
    };

    internal static bool IsValidText(string? value, int maximum, bool allowEmpty = false)
    {
        if (value is null || value.Length > maximum || (!allowEmpty && string.IsNullOrWhiteSpace(value)))
            return false;
        for (var index = 0; index < value.Length; index++)
        {
            if (!char.IsSurrogate(value[index])) continue;
            if (!char.IsHighSurrogate(value[index]) || index + 1 >= value.Length ||
                !char.IsLowSurrogate(value[++index])) return false;
        }
        return true;
    }

    internal static bool HasExactly(JsonElement element, params string[] properties)
    {
        if (element.ValueKind != JsonValueKind.Object) return false;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
            if (!properties.Contains(property.Name, StringComparer.Ordinal) || !seen.Add(property.Name))
                return false;
        return seen.Count == properties.Length;
    }

    internal static async Task<ReadOnlyMemory<byte>> ReadBoundedAsync(
        Stream stream, long? contentLength, int maximumBytes, CancellationToken cancellationToken)
    {
        if (contentLength > maximumBytes) throw new InvalidDataException("ScholarBot body exceeds its limit.");
        using var body = new MemoryStream(contentLength is > 0 ? (int)contentLength.Value : 0);
        var buffer = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            while (true)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0,
                    (int)Math.Min(buffer.Length, maximumBytes - body.Length + 1)), cancellationToken);
                if (read == 0) break;
                if (read > maximumBytes - body.Length)
                    throw new InvalidDataException("ScholarBot body exceeds its limit.");
                body.Write(buffer, 0, read);
            }
            return body.GetBuffer().AsMemory(0, (int)body.Length);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }
}
