using System.Security.Claims;
using System.Text.Json;
using BeasiswaIn.Web.Infrastructures.Integrations.Options;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.ScholarBot;
using BeasiswaIn.Web.Services.ScholarBot;
using Microsoft.Extensions.Options;

namespace BeasiswaIn.Web.Infrastructures.DependencyInjection;

internal static class ScholarBotEndpoints
{
    public static IEndpointRouteBuilder MapAppScholarBot(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/internal/scholarbot/tools", ExecuteAsync)
            .AllowAnonymous()
            .DisableAntiforgery();
        return endpoints;
    }

    private static async Task<IResult> ExecuteAsync(
        HttpContext context, ScholarBotDelegation delegation, IOptions<ScholarBotOptions> options,
        TimeProvider timeProvider)
    {
        // Cookies are neither a fallback nor an identity source for this service-only endpoint.
        context.User = new ClaimsPrincipal(new ClaimsIdentity());
        if (!delegation.HasServiceAuthorization(context.Request) ||
            !delegation.TryReadUser(context.Request, out var userId))
            return Results.StatusCode(StatusCodes.Status401Unauthorized);
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId)], "ScholarBotDelegation"));

        ToolRequest? request;
        try
        {
            if (!context.Request.HasJsonContentType()) return Results.BadRequest();
            var body = await ScholarBotProtocol.ReadBoundedAsync(context.Request.Body,
                context.Request.ContentLength, 32768, context.RequestAborted);
            using var document = JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = 8 });
            if (!ScholarBotProtocol.HasExactly(document.RootElement,
                "name", "arguments", "remainingTokens", "maxOutputTokens")) return Results.BadRequest();
            request = document.RootElement.Deserialize<ToolRequest>(ScholarBotProtocol.JsonOptions);
            if (request is null || !ScholarBotProtocol.IsValidText(request.Name, 64) ||
                !ScholarBotProtocol.IsValidText(request.Arguments, 8000) ||
                request.RemainingTokens is < 1 or > 64000 || request.MaxOutputTokens is < 1 or > 10000 ||
                request.MaxOutputTokens > request.RemainingTokens)
                return Results.BadRequest();
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException or
                                        InvalidOperationException or ArgumentException or BadHttpRequestException)
        {
            return Results.BadRequest();
        }

        // Resolve scoped tools only after replacing the callback principal with its signed identity.
        var usage = context.RequestServices.GetRequiredService<ScholarBotUsage>();
        var limits = options.Value;
        var totalTokens = Math.Min(request.RemainingTokens, limits.MaxMatchingTotalTokens);
        usage.Begin(totalTokens, Math.Min(totalTokens,
            Math.Min(request.MaxOutputTokens, limits.MaxMatchingOutputTokens)));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(limits.TimeoutSeconds), timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted, timeout.Token);
        ScholarBotToolResult result;
        try
        {
            var tools = context.RequestServices.GetRequiredService<IScholarBotTools>();
            result = await tools.ExecuteAsync(request.Name, request.Arguments, linked.Token);
            linked.Token.ThrowIfCancellationRequested();
            result = Normalize(result);
        }
        catch (Exception) when (context.RequestAborted.IsCancellationRequested)
        {
            context.RequestAborted.ThrowIfCancellationRequested();
            throw;
        }
        catch (OperationCanceledException)
        {
            result = Failure("TIMEOUT", "Pengambilan data melewati batas waktu.");
        }
        catch (Exception)
        {
            result = Failure("SERVICE_UNAVAILABLE", "Data belum dapat diambil dari layanan.");
        }
        // Snapshot after execution, including matching usage when its output could not be validated.
        return Results.Json(new { result.Content, result.Success, result.Sources, usage = usage.Snapshot() },
            ScholarBotProtocol.JsonOptions);
    }

    private static ScholarBotToolResult Normalize(ScholarBotToolResult? result)
    {
        if (result is null || result.Sources is null ||
            !ScholarBotProtocol.IsValidText(result.Content, 16000))
            return Failure("INVALID_TOOL_RESULT", "Hasil alat tidak dapat digunakan atau terlalu panjang.");
        try
        {
            using var document = JsonDocument.Parse(result.Content);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return Failure("INVALID_TOOL_RESULT", "Format hasil alat tidak dapat digunakan.");
        }
        catch (JsonException)
        {
            return Failure("INVALID_TOOL_RESULT", "Format hasil alat tidak dapat digunakan.");
        }
        return result;
    }

    private static ScholarBotToolResult Failure(string code, string message) =>
        new(JsonSerializer.Serialize(new { error = new { code, message } }, ScholarBotProtocol.JsonOptions), false, []);

    private sealed record ToolRequest(string Name, string Arguments, int RemainingTokens, int MaxOutputTokens);
}
