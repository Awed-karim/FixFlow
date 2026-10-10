namespace FixFlow.Application.Common;

public record ShareLinks(string TrackingUrl, string WhatsAppUrl, string Message);

public static class QuickShareLinks
{
    public static ShareLinks Build(string trackingBaseUrl, Guid token, string title)
    {
        var url = $"{trackingBaseUrl.TrimEnd('/')}/{token}";
        var message = $"تابع حالة طلب الصيانة «{title}» من هنا: {url}";
        var whatsApp = $"https://wa.me/?text={Uri.EscapeDataString(message)}";

        return new ShareLinks(url, whatsApp, message);
    }
}