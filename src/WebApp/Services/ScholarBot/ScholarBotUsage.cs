using System.ClientModel;
using System.Text.Json;
using BeasiswaIn.Web.Models.ScholarBot;
using OpenAI.Chat;

namespace BeasiswaIn.Web.Services.ScholarBot;

internal sealed class ScholarBotUsage(ILogger<ScholarBotUsage> logger)
{
    private int _inputTokens;
    private int _outputTokens;
    private int _apiCalls;
    private decimal _cost;
    private bool _allCostsKnown = true;
    private int _maxTotalTokens;
    private int _maxOutputTokens;

    public bool IsActive { get; private set; }
    public int RemainingTokens => IsActive ? Math.Max(0, _maxTotalTokens - TotalTokens) : int.MaxValue;
    public int MaxOutputTokens => Math.Min(_maxOutputTokens, RemainingTokens);

    public void Begin(int maxTotalTokens, int maxOutputTokens)
    {
        Reset();
        _maxTotalTokens = maxTotalTokens;
        _maxOutputTokens = maxOutputTokens;
        IsActive = true;
    }

    public int TotalTokens => _inputTokens + _outputTokens;

    public void Reset()
    {
        _inputTokens = 0;
        _outputTokens = 0;
        _apiCalls = 0;
        _cost = 0;
        _allCostsKnown = true;
        IsActive = false;
    }

    public void Record(ClientResult<ChatCompletion> result, string operation)
    {
        var usage = result.Value.Usage;
        _inputTokens += usage?.InputTokenCount ?? 0;
        _outputTokens += usage?.OutputTokenCount ?? 0;
        _apiCalls++;
        decimal? cost = null;
        using var document = JsonDocument.Parse(result.GetRawResponse().Content.ToMemory());
        if (document.RootElement.TryGetProperty("usage", out var rawUsage) &&
            rawUsage.TryGetProperty("cost", out var rawCost) &&
            rawCost.ValueKind == JsonValueKind.Number && rawCost.TryGetDecimal(out var value))
            cost = value;
        if (cost.HasValue) _cost += cost.Value;
        else _allCostsKnown = false;
        logger.LogInformation("AI {Operation}: input={InputTokens} output={OutputTokens} costUsd={CostUsd}",
            operation, usage?.InputTokenCount, usage?.OutputTokenCount, cost);
    }

    public ScholarBotUsageSummary Snapshot() => new(_inputTokens, _outputTokens, _apiCalls,
        _allCostsKnown ? _cost : null);
}
