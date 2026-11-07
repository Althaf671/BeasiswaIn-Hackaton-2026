using System.Text.Json;

namespace BeasiswaIn.Web.Models.Chatbot;

public sealed record ChatResponse
{
    public string Answer { get; init; } = string.Empty;
    public JsonElement Sources { get; init; }
    public JsonElement ToolCalls { get; init; }
    public JsonElement Usage { get; init; }

    // Answer yang sudah dipecah per poin oleh StringHelper.ResolveList (diisi di server)
    public List<string> Items { get; init; } = [];
}
