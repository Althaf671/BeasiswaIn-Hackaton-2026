using System.ClientModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Errors;
using BeasiswaIn.Web.Infrastructures.Integrations.Options;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.ScholarBot;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace BeasiswaIn.Web.Services.ScholarBot;

internal sealed class ScholarBotService(
    [FromKeyedServices(ScholarBotOptions.ClientKey)] ChatClient chatClient,
    IScholarBotTools tools,
    IUserContext userContext,
    ScholarBotUsage usage,
    IOptions<ScholarBotOptions> options,
    TimeProvider timeProvider,
    ILogger<ScholarBotService> logger) : IScholarBotService
{
    private const int MaxInputCharacters = ScholarBotRequest.MaxContextCharacters;
    private const int MaxAnswerCharacters = ScholarBotRequest.MaxHistoryMessageCharacters;
    private const int MaxToolArgumentCharacters = 16000;

    public async Task<Result<ScholarBotResponse>> ChatAsync(
        ScholarBotRequest request, CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || string.IsNullOrWhiteSpace(userContext.UserId))
            return Result<ScholarBotResponse>.Failure(ScholarBotErrors.Unauthorized);
        if (!IsValid(request))
            return Result<ScholarBotResponse>.Failure(ScholarBotErrors.InvalidRequest);

        cancellationToken.ThrowIfCancellationRequested();
        var limits = options.Value;
        usage.Begin(limits.MaxTotalTokens, limits.MaxOutputTokens);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(limits.TimeoutSeconds), timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        var token = linked.Token;

        try
        {
            // All browser-controlled characters have been bounded before constructing SDK messages.
            List<ChatMessage> messages = [new SystemChatMessage(ScholarBotInstructions.Create(timeProvider.GetUtcNow()))];
            foreach (var item in request.History)
                messages.Add(item.Role == "user"
                    ? new UserChatMessage(item.Content)
                    : new AssistantChatMessage(item.Content));
            messages.Add(new UserChatMessage(request.Message));

            var completionOptions = new ChatCompletionOptions { AllowParallelToolCalls = false };
            foreach (var definition in tools.Definitions)
                completionOptions.Tools.Add(definition);

            var sources = new Dictionary<string, ScholarBotSource>(StringComparer.Ordinal);
            var traces = new List<ScholarBotToolTrace>();
            var toolCallCount = 0;
            for (var iteration = 0; iteration < limits.MaxIterations; iteration++)
            {
                token.ThrowIfCancellationRequested();
                var remainingTokens = limits.MaxTotalTokens - usage.TotalTokens;
                if (remainingTokens <= 0)
                    return Result<ScholarBotResponse>.Failure(ScholarBotErrors.BudgetExceeded);
                completionOptions.MaxOutputTokenCount = Math.Min(limits.MaxOutputTokens, remainingTokens);

                var result = await chatClient.CompleteChatAsync(messages, completionOptions, token);
                token.ThrowIfCancellationRequested();
                var completion = result.Value;
                // Missing usage cannot honestly be treated as free processing under a total-token cap.
                if (completion?.Usage is null || completion.Usage.InputTokenCount < 0 ||
                    completion.Usage.OutputTokenCount < 0)
                    return Result<ScholarBotResponse>.Failure(ScholarBotErrors.InvalidResponse);
                usage.Record(result, "ScholarBot.Chat");
                if (usage.TotalTokens >= limits.MaxTotalTokens)
                    return Result<ScholarBotResponse>.Failure(ScholarBotErrors.BudgetExceeded);
                if (completion.Usage.OutputTokenCount > completionOptions.MaxOutputTokenCount ||
                    completion.FinishReason is ChatFinishReason.Length or ChatFinishReason.ContentFilter ||
                    !string.IsNullOrWhiteSpace(completion.Refusal))
                    return Result<ScholarBotResponse>.Failure(ScholarBotErrors.InvalidResponse);

                if (!TryGetText(completion, out var answer))
                    return Result<ScholarBotResponse>.Failure(ScholarBotErrors.InvalidResponse);

                // Tool calls can accompany finish_reason=stop; the collection is authoritative.
                if (completion.ToolCalls.Count == 0)
                {
                    if (completion.FinishReason != ChatFinishReason.Stop || string.IsNullOrWhiteSpace(answer))
                        return Result<ScholarBotResponse>.Failure(ScholarBotErrors.InvalidResponse);
                    return Result<ScholarBotResponse>.Success(new ScholarBotResponse
                    {
                        Answer = answer.Trim(),
                        Sources = sources.Values.ToArray(),
                        ToolCalls = traces,
                        Usage = usage.Snapshot()
                    });
                }

                // Count every requested call, including unknown names and malformed arguments.
                if (completion.ToolCalls.Count > limits.MaxToolCalls - toolCallCount ||
                    iteration + 1 >= limits.MaxIterations)
                    return Result<ScholarBotResponse>.Failure(ScholarBotErrors.BudgetExceeded);
                toolCallCount += completion.ToolCalls.Count;
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var call in completion.ToolCalls)
                {
                    if (call is null || string.IsNullOrWhiteSpace(call.Id) || call.Id.Length > 256 || !ids.Add(call.Id) ||
                        call.Kind != ChatToolCallKind.Function || string.IsNullOrWhiteSpace(call.FunctionName) ||
                        call.FunctionName.Length > 64 || call.FunctionArguments is null ||
                        call.FunctionArguments.ToMemory().Length > MaxToolArgumentCharacters * 4 ||
                        Encoding.UTF8.GetCharCount(call.FunctionArguments.ToMemory().Span) > MaxToolArgumentCharacters)
                        return Result<ScholarBotResponse>.Failure(ScholarBotErrors.InvalidResponse);
                }

                messages.Add(new AssistantChatMessage(completion));
                foreach (var call in completion.ToolCalls)
                {
                    token.ThrowIfCancellationRequested();
                    if (usage.TotalTokens >= limits.MaxTotalTokens)
                        return Result<ScholarBotResponse>.Failure(ScholarBotErrors.BudgetExceeded);
                    var started = Stopwatch.GetTimestamp();
                    var knownTool = IsKnownTool(call.FunctionName);
                    ScholarBotToolResult toolResult;
                    if (!knownTool)
                    {
                        toolResult = ToolError("UnknownTool", "Alat yang diminta tidak tersedia.");
                    }
                    else
                    {
                        toolResult = await tools.ExecuteAsync(call.FunctionName, call.FunctionArguments.ToString(), token);
                    }
                    token.ThrowIfCancellationRequested();
                    toolResult = NormalizeToolResult(toolResult, limits.MaxToolResultCharacters);
                    traces.Add(new ScholarBotToolTrace(knownTool ? call.FunctionName : "UnknownTool",
                        toolResult.Success, (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds));
                    if (usage.TotalTokens >= limits.MaxTotalTokens)
                        return Result<ScholarBotResponse>.Failure(ScholarBotErrors.BudgetExceeded);

                    // Never derive sources from model text, arguments, or browser history.
                    if (toolResult.Success)
                    {
                        foreach (var source in toolResult.Sources)
                        {
                            if (source is not null && !string.IsNullOrWhiteSpace(source.Id))
                                sources.TryAdd(source.Id, source);
                        }
                    }
                    messages.Add(new ToolChatMessage(call.Id, toolResult.Content));
                }
            }
            return Result<ScholarBotResponse>.Failure(ScholarBotErrors.BudgetExceeded);
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
        catch (ClientResultException exception)
        {
            logger.LogWarning("ScholarBot provider failure: type={ExceptionType} status={Status}",
                exception.GetType().Name, exception.Status);
            return Result<ScholarBotResponse>.Failure(exception.Status is 408 or 504
                ? ScholarBotErrors.Timeout : ScholarBotErrors.Unavailable);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or ArgumentException)
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

    private static bool IsValid(ScholarBotRequest? request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Message) ||
            request.Message.Length > ScholarBotRequest.MaxMessageCharacters ||
            request.History is null || request.History.Count > ScholarBotRequest.MaxHistoryMessages)
            return false;
        var characters = request.Message.Length;
        foreach (var item in request.History)
        {
            if (item is null || item.Role is not ("user" or "assistant") ||
                string.IsNullOrWhiteSpace(item.Content) || item.Content.Length >
                (item.Role == "user" ? ScholarBotRequest.MaxMessageCharacters : ScholarBotRequest.MaxHistoryMessageCharacters))
                return false;
            characters += item.Content.Length;
            if (characters > MaxInputCharacters)
                return false;
        }
        return true;
    }

    private static bool IsKnownTool(string name) =>
        name is "GetUserProfile" or "GetRecommendations" or "GetScholarshipDetails";

    private static bool TryGetText(ChatCompletion completion, out string text)
    {
        var builder = new StringBuilder();
        foreach (var part in completion.Content)
        {
            if (part.Kind != ChatMessageContentPartKind.Text || part.Text is null ||
                part.Text.Length > MaxAnswerCharacters - builder.Length)
            {
                text = string.Empty;
                return false;
            }
            builder.Append(part.Text);
        }
        text = builder.ToString();
        return true;
    }

    private static ScholarBotToolResult NormalizeToolResult(ScholarBotToolResult? result, int maxCharacters)
    {
        if (result is null || string.IsNullOrWhiteSpace(result.Content) || result.Sources is null)
            return ToolError("InvalidToolResult", "Alat tidak mengembalikan data yang dapat digunakan.");
        if (result.Content.Length > maxCharacters)
            return ToolError("ToolResultTooLarge", "Hasil alat terlalu panjang. Sempitkan pertanyaan atau kurangi jumlah hasil.");
        try
        {
            using var document = JsonDocument.Parse(result.Content);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return ToolError("InvalidToolResult", "Format hasil alat tidak dapat digunakan.");
        }
        catch (JsonException)
        {
            return ToolError("InvalidToolResult", "Format hasil alat tidak dapat digunakan.");
        }
        return result;
    }

    private static ScholarBotToolResult ToolError(string code, string message) =>
        new(JsonSerializer.Serialize(new { error = new { code, message } }), false, []);
}
