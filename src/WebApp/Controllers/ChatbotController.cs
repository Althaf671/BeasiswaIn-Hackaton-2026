using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Security.Claims;
using BeasiswaIn.Web.Infrastructures.Integrations.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;
using BeasiswaIn.Web.Common.Helpers;
using BeasiswaIn.Web.Models.Chatbot;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeasiswaIn.Web.Controllers;

[ApiController]
[Route("api/chatbot")]
[Authorize]
public class ChatBotController: ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PythonApiOptions _options;
    private readonly ILogger<ChatBotController> _logger;
    private readonly IWebHostEnvironment _environment;

    public ChatBotController(
        IHttpClientFactory httpClientFactory,
        IOptions<PythonApiOptions> options,
        ILogger<ChatBotController> logger,
        IWebHostEnvironment environment)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
        _environment = environment;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChatAsync(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId) || userId.Length > 256)
            return Unauthorized();

        var serviceToken = _options.ServiceToken;
        if (string.IsNullOrWhiteSpace(serviceToken))
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                detail: "PythonApi:ServiceToken belum diisi. Gunakan token yang sama dengan SCHOLARBOT_SERVICE_TOKEN pada Python.");

        try
        {
            var client = _httpClientFactory.CreateClient("PythonChat");
            var payload = new PythonChatRequest(request.Message.Trim(), request.History, userId);
            using var outgoing = new HttpRequestMessage(HttpMethod.Post, _options.ChatEndpoint)
            {
                Content = JsonContent.Create(payload)
            };
            outgoing.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceToken);
            using var response = await client.SendAsync(outgoing, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Python chat returned HTTP {StatusCode}", (int)response.StatusCode);
                var detail = "Backend Python gagal memproses pesan.";
                if (_environment.IsDevelopment())
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    const int maxErrorLength = 2000;
                    if (errorBody.Length > maxErrorLength)
                        errorBody = errorBody[..maxErrorLength] + "… [dipotong]";

                    _logger.LogWarning("Python chat error response: {ErrorBody}", errorBody);
                    detail = $"Python HTTP {(int)response.StatusCode}: " +
                        (string.IsNullOrWhiteSpace(errorBody) ? "Respons error kosong." : errorBody);
                }
                return Problem(statusCode: StatusCodes.Status502BadGateway,
                    detail: detail);
            }

            var result = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken);
            if (string.IsNullOrWhiteSpace(result?.Answer))
                return Problem(statusCode: StatusCodes.Status502BadGateway,
                    detail: "Format respons backend Python tidak sesuai.");

            return Ok(result with { Items = StringHelper.ResolveList([result.Answer]) });
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Problem(statusCode: StatusCodes.Status504GatewayTimeout,
                detail: "Backend Python terlalu lama merespons.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Unable to contact Python chat backend");
            return Problem(statusCode: StatusCodes.Status502BadGateway,
                detail: "Backend Python tidak dapat dihubungi.");
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Invalid Python chat response");
            return Problem(statusCode: StatusCodes.Status502BadGateway,
                detail: "Format respons backend Python tidak sesuai.");
        }
    }
}
 
// Explicit wire names keep the strict Python contract independent of MVC JSON settings.
internal sealed record PythonChatRequest(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("history")] List<ChatHistoryMessage> History,
    [property: JsonPropertyName("userId")] string UserId);
