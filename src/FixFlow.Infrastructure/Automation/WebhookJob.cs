using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FixFlow.Application.Common;
using FixFlow.Domain.Enums;
using FixFlow.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FixFlow.Infrastructure.Automation;

public class WebhookJob
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _http;
    private readonly WebhookSettings _settings;
    private readonly AppUrlSettings _urls;

    public WebhookJob(
        AppDbContext db,
        IHttpClientFactory http,
        IOptions<WebhookSettings> settings,
        IOptions<AppUrlSettings> urls)
    {
        _db = db;
        _http = http;
        _settings = settings.Value;
        _urls = urls.Value;
    }

    [AutomaticRetry(Attempts = 5, DelaysInSeconds = new[] { 10, 30, 60, 120, 300 })]
    public async Task SendRequestEventAsync(int requestId, string eventType)
    {
        if (!IsEnabled())
            return;

        var r = await _db.ServiceRequests
            .AsNoTracking()
            .Include(x => x.ServiceCategory)
            .Include(x => x.TechnicianProfile)
            .FirstOrDefaultAsync(x => x.Id == requestId);

        if (r is null)
            return;

        var customer = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == r.CustomerId)
            .Select(u => new { u.FullName, u.Email, u.PhoneNumber })
            .FirstOrDefaultAsync();

        var links = QuickShareLinks.Build(_urls.TrackingBaseUrl, r.TrackingToken, r.Title);

        var payload = new
        {
            eventType,
            occurredAt = DateTime.UtcNow,
            request = new
            {
                id = r.Id,
                title = r.Title,
                description = r.Description,
                address = r.Address,
                status = r.Status.ToString(),
                category = r.ServiceCategory.Name,
                createdAt = r.CreatedAt,
                completedAt = r.CompletedAt
            },
            customer = new { name = customer?.FullName, email = customer?.Email, phone = customer?.PhoneNumber },
            technician = r.TechnicianProfile is null
                ? null
                : new { name = r.TechnicianProfile.FullName, phone = r.TechnicianProfile.PhoneNumber },
            tracking = new { url = links.TrackingUrl, whatsAppUrl = links.WhatsAppUrl }
        };

        await PostAsync(payload);
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task SendDailyReportAsync()
    {
        if (!IsEnabled())
            return;

        var to = DateTime.UtcNow.Date;
        var from = to.AddDays(-1);

        var created = await _db.ServiceRequests.CountAsync(r => r.CreatedAt >= from && r.CreatedAt < to);
        var completed = await _db.ServiceRequests.CountAsync(r => r.CompletedAt >= from && r.CompletedAt < to);
        var cancelled = await _db.ServiceRequests.CountAsync(r =>
            r.Status == RequestStatus.Cancelled && r.UpdatedAt >= from && r.UpdatedAt < to);
        var openNow = await _db.ServiceRequests.CountAsync(r =>
            r.Status != RequestStatus.Completed && r.Status != RequestStatus.Reviewed &&
            r.Status != RequestStatus.Cancelled && r.Status != RequestStatus.Rejected);

        await PostAsync(new
        {
            eventType = "DailyReport",
            occurredAt = DateTime.UtcNow,
            report = new
            {
                date = from.ToString("yyyy-MM-dd"),
                created,
                completed,
                cancelled,
                openNow
            }
        });
    }

    private bool IsEnabled() => _settings.Enabled && !string.IsNullOrWhiteSpace(_settings.Url);

    private async Task PostAsync(object payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);

        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.Url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrEmpty(_settings.Secret))
            request.Headers.Add("X-FixFlow-Signature", Sign(json));

        var client = _http.CreateClient("webhook");
        var response = await client.SendAsync(request);

        // أي رد غير ناجح يرمي Exception، وHangfire يعيد المحاولة
        response.EnsureSuccessStatusCode();
    }

    private string Sign(string body)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.Secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}