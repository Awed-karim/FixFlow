namespace FixFlow.Infrastructure.Automation;

public class AutomationSettings
{
    // مهلة رد الفني على العرض
    public int OfferTimeoutSeconds { get; set; } = 120;

    // الطلب اللي ما اتحركش المدة دي يتصعّد للأدمن
    public int StaleAfterMinutes { get; set; } = 1440;
}

public class AppUrlSettings
{
    // أساس رابط التتبع (لما نعمل الفرونت هنغيره لصفحة React)
    public string TrackingBaseUrl { get; set; } = "http://localhost:5000/api/requests/track";
}

public class WebhookSettings
{
    public bool Enabled { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
}