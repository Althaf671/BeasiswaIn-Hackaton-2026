using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Errors;
using BeasiswaIn.Web.Infrastructures.Integrations.Options;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.ScholarBot;
using Microsoft.Extensions.Options;

namespace BeasiswaIn.Web.Services.ScholarBot;

internal sealed class ScholarBotClient(
    HttpClient client,
    IUserContext userContext,
    IHttpContextAccessor httpContext,
    IOptions<ScholarBotOptions> options,
    TimeProvider timeProvider,
    ILogger<ScholarBotClient> logger) : IScholarBotService
{
    public async Task<Result<ScholarBotResponse>> ChatAsync(
        ScholarBotRequest request, CancellationToken cancellationToken)
    {
        var userId = userContext.UserId;
        if (!userContext.IsAuthenticated || !ScholarBotProtocol.IsValidText(userId, 256))
            return Result<ScholarBotResponse>.Failure(ScholarBotErrors.Unauthorized);
        if (!IsValidRequest(request))
            return Result<ScholarBotResponse>.Failure(ScholarBotErrors.InvalidRequest);

        var requestAborted = httpContext.HttpContext?.RequestAborted ?? CancellationToken.None;
        cancellationToken.ThrowIfCancellationRequested();
        requestAborted.ThrowIfCancellationRequested();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(options.Value.TimeoutSeconds), timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, requestAborted, timeout.Token);
        try
        {
            var body = JsonSerializer.SerializeToUtf8Bytes(new
            {
                request.Message,
                request.History,
                userId
            }, ScholarBotProtocol.JsonOptions);
            if (body.Length > 262144)
                return Result<ScholarBotResponse>.Failure(ScholarBotErrors.InvalidRequest);
            using var message = new HttpRequestMessage(HttpMethod.Post, "scholarbot/chat");
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ServiceToken);
            message.Content = new ByteArrayContent(body);
            message.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, linked.Token);
            await using var stream = await response.Content.ReadAsStreamAsync(linked.Token);
            var bytes = await ScholarBotProtocol.ReadBoundedAsync(stream,
                response.Content.Headers.ContentLength, 1048576, linked.Token);
            linked.Token.ThrowIfCancellationRequested();
            if (!response.IsSuccessStatusCode)
                return Result<ScholarBotResponse>.Failure(MapError(response.StatusCode, bytes));
            if (response.StatusCode != HttpStatusCode.OK ||
                response.Content.Headers.ContentType?.MediaType != "application/json")
                return Result<ScholarBotResponse>.Failure(ScholarBotErrors.InvalidResponse);
            using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 16 });
            if (!IsValidResponse(document.RootElement))
                return Result<ScholarBotResponse>.Failure(ScholarBotErrors.InvalidResponse);
            var result = document.RootElement.Deserialize<ScholarBotResponse>(ScholarBotProtocol.JsonOptions);
            return result is null
                ? Result<ScholarBotResponse>.Failure(ScholarBotErrors.InvalidResponse)
                : Result<ScholarBotResponse>.Success(result);
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested || requestAborted.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
            requestAborted.ThrowIfCancellationRequested();
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result<ScholarBotResponse>.Failure(ScholarBotErrors.Timeout);
        }
        catch (Exception) when (timeout.IsCancellationRequested)
        {
            return Result<ScholarBotResponse>.Failure(ScholarBotErrors.Timeout);
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException or
                                        InvalidOperationException or ArgumentException or OverflowException)
        {
            logger.LogWarning("ScholarBot unusable response: type={ExceptionType}", exception.GetType().Name);
            return Result<ScholarBotResponse>.Failure(ScholarBotErrors.InvalidResponse);
        }
        catch (Exception exception)
        {
            logger.LogWarning("ScholarBot request failure: type={ExceptionType}", exception.GetType().Name);
            return Result<ScholarBotResponse>.Failure(ScholarBotErrors.Unavailable);
        }
    }

    private static bool IsValidRequest(ScholarBotRequest? request)
    {
        if (request is null || !ScholarBotProtocol.IsValidText(request.Message, ScholarBotRequest.MaxMessageCharacters) ||
            request.History is null || request.History.Count > ScholarBotRequest.MaxHistoryMessages) return false;
        var characters = request.Message.Length;
        foreach (var item in request.History)
        {
            if (item is null || item.Role is not ("user" or "assistant") ||
                !ScholarBotProtocol.IsValidText(item.Content, item.Role == "user"
                    ? ScholarBotRequest.MaxMessageCharacters : ScholarBotRequest.MaxHistoryMessageCharacters)) return false;
            characters += item.Content.Length;
            if (characters > ScholarBotRequest.MaxContextCharacters) return false;
        }
        return true;
    }

    private static Error MapError(HttpStatusCode status, ReadOnlyMemory<byte> body)
    {
        try
        {
            using var document = JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = 4 });
            var root = document.RootElement;
            if (ScholarBotProtocol.HasExactly(root, "error") &&
                ScholarBotProtocol.HasExactly(root.GetProperty("error"), "code", "message"))
            {
                var error = root.GetProperty("error");
                if (error.GetProperty("code").ValueKind != JsonValueKind.String ||
                    error.GetProperty("message").ValueKind != JsonValueKind.String)
                    return ScholarBotErrors.InvalidResponse;
                return (error.GetProperty("code").GetString(), (int)status) switch
                {
                    ("Unauthorized", 401) => ScholarBotErrors.Unauthorized,
                    ("InvalidRequest", 400) => ScholarBotErrors.InvalidRequest,
                    ("Unavailable", 503) => ScholarBotErrors.Unavailable,
                    ("Timeout", 408) => ScholarBotErrors.Timeout,
                    ("BudgetExceeded", 400) => ScholarBotErrors.BudgetExceeded,
                    ("InvalidResponse", 503) => ScholarBotErrors.InvalidResponse,
                    _ => ScholarBotErrors.InvalidResponse
                };
            }
        }
        catch (JsonException) { }
        return status is HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout
            ? ScholarBotErrors.Timeout
            : status is HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable
                ? ScholarBotErrors.Unavailable : ScholarBotErrors.InvalidResponse;
    }

    private static bool IsValidResponse(JsonElement root)
    {
        if (!ScholarBotProtocol.HasExactly(root, "answer", "sources", "toolCalls", "usage") ||
            !Text(root.GetProperty("answer"), ScholarBotRequest.MaxHistoryMessageCharacters)) return false;
        var usage = root.GetProperty("usage");
        if (!ScholarBotProtocol.HasExactly(usage, "inputTokens", "outputTokens", "apiCalls", "costUsd") ||
            !NonnegativeInt(usage.GetProperty("inputTokens")) || !NonnegativeInt(usage.GetProperty("outputTokens")) ||
            !NonnegativeInt(usage.GetProperty("apiCalls"))) return false;
        var cost = usage.GetProperty("costUsd");
        if (cost.ValueKind != JsonValueKind.Null &&
            (cost.ValueKind != JsonValueKind.Number || !cost.TryGetDecimal(out var amount) || amount < 0)) return false;
        var sources = root.GetProperty("sources");
        var traces = root.GetProperty("toolCalls");
        if (sources.ValueKind != JsonValueKind.Array || sources.GetArrayLength() > 60 ||
            traces.ValueKind != JsonValueKind.Array || traces.GetArrayLength() > 12) return false;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var source in sources.EnumerateArray())
        {
            if (!ScholarBotProtocol.HasExactly(source, "id", "name", "registrationUrl", "guideUrl", "deadline") ||
                !Text(source.GetProperty("id"), 256) || !ids.Add(source.GetProperty("id").GetString()!) ||
                !Text(source.GetProperty("name"), 2000) || !SafeUrl(source.GetProperty("registrationUrl")) ||
                !SafeUrl(source.GetProperty("guideUrl")) || source.GetProperty("deadline").ValueKind != JsonValueKind.String ||
                !source.GetProperty("deadline").TryGetDateTimeOffset(out _)) return false;
        }
        foreach (var trace in traces.EnumerateArray())
        {
            if (!ScholarBotProtocol.HasExactly(trace, "name", "success", "durationMilliseconds") ||
                trace.GetProperty("name").ValueKind != JsonValueKind.String ||
                trace.GetProperty("name").GetString() is not ("GetUserProfile" or "GetRecommendations" or
                    "GetScholarshipDetails" or "UnknownTool") ||
                trace.GetProperty("success").ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                trace.GetProperty("durationMilliseconds").ValueKind != JsonValueKind.Number ||
                !trace.GetProperty("durationMilliseconds").TryGetInt64(out var duration) || duration < 0) return false;
        }
        return true;
    }

    private static bool Text(JsonElement value, int maximum) => value.ValueKind == JsonValueKind.String &&
        ScholarBotProtocol.IsValidText(value.GetString(), maximum);

    private static bool NonnegativeInt(JsonElement value) => value.ValueKind == JsonValueKind.Number &&
        value.TryGetInt32(out var number) && number >= 0;

    private static bool SafeUrl(JsonElement value) => value.ValueKind == JsonValueKind.Null ||
        (Text(value, 8192) && Uri.TryCreate(value.GetString(), UriKind.Absolute, out var uri) &&
         uri.Scheme is "http" or "https");
}
