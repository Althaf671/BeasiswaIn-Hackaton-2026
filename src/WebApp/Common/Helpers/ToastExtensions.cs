
 
using System.Net;
using AspNetCoreHero.ToastNotification.Abstractions;

namespace BeasiswaIn.Web.Common.Helpers;

public static class NotyfToastExtensions
{
    private const string Circle = "<circle cx='12' cy='12' r='10'/>";

    private static string Html(string color, string icon, string title, string message) =>
        "<div class=\"toast show\">"
        + "<div class=\"toast-header\">"
        + $"<svg class=\"me-2\" width=\"20\" height=\"20\" viewBox=\"0 0 24 24\" fill=\"none\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" style=\"stroke:var({color})\">{icon}</svg>"
        + $"<strong class=\"me-auto\">{WebUtility.HtmlEncode(title)}</strong>"
        + "<button type=\"button\" class=\"btn-close\" aria-label=\"Close\" onclick=\"this.closest('.notyf__toast').remove()\"></button>"
        + "</div>"
        + $"<div class=\"toast-body\">{WebUtility.HtmlEncode(message)}</div>"
        + "</div>";

    public static void ToastSuccess(this INotyfService n, string title, string message) =>
        n.Success(Html("--color-success", Circle + "<path d='m9 12 2 2 4-4'/>", title, message));

    public static void ToastError(this INotyfService n, string title, string message) =>
        n.Error(Html("--color-error", Circle + "<path d='m15 9-6 6'/><path d='m9 9 6 6'/>", title, message));

    public static void ToastWarning(this INotyfService n, string title, string message) =>
        n.Warning(Html("--color-warning-text", "<path d='m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3'/><path d='M12 9v4'/><path d='M12 17h.01'/>", title, message));

    public static void ToastInfo(this INotyfService n, string title, string message) =>
        n.Information(Html("--color-accent", Circle + "<path d='M12 16v-4'/><path d='M12 8h.01'/>", title, message));
}