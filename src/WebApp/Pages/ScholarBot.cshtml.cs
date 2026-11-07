using BeasiswaIn.Web.Common.Helpers;
using BeasiswaIn.Web.Infrastructures.DependencyInjection;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.ScholarBot;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace BeasiswaIn.Web.Pages;

[Authorize]
[EnableRateLimiting(RateLimitRegistry.HighRateResourcePolicy)]
[RequestSizeLimit(262144)]
public sealed class ScholarBotModel(IScholarBotService scholarBotService) : PageModel
{
    public async Task<IActionResult> OnPostChatAsync(
        [FromBody] ScholarBotRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null || !ModelState.IsValid)
        {
            return new BadRequestObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Pesan tidak valid",
                Detail = "Kirim pesan atau esai hingga 12.000 karakter dengan riwayat percakapan yang valid."
            });
        }

        var result = await scholarBotService.ChatAsync(request, cancellationToken);
        return result.IsSuccess
            ? new JsonResult(result.Value)
            : result.ToProblemDetails(HttpContext);
    }
}
