using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Infrastructure.Automation;

public static class RecurringJobsRegistrar
{
    public static void Register(IServiceProvider services)
    {
        var manager = services.GetRequiredService<IRecurringJobManager>();
        var options = new RecurringJobOptions();

        manager.AddOrUpdate<AutomationJobs>(
            "retry-pending-assignments",
            j => j.RetryPendingAssignmentsAsync(),
            Cron.Minutely(), options);

        manager.AddOrUpdate<AutomationJobs>(
            "expire-overdue-offers",
            j => j.ExpireOverdueOffersAsync(),
            Cron.Minutely(), options);

        manager.AddOrUpdate<AutomationJobs>(
            "escalate-stale-requests",
            j => j.EscalateStaleRequestsAsync(),
            "*/15 * * * *", options);

        // 06:00 UTC = 08:00 بتوقيت القاهرة (شتاءً)
        manager.AddOrUpdate<WebhookJob>(
            "daily-report",
            j => j.SendDailyReportAsync(),
            Cron.Daily(6), options);
    }
}