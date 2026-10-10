namespace FixFlow.Application.Common;

public enum RequestEventType
{
    Created,
    Assigned,
    Accepted,
    OfferRejected,   // الفني رفض
    OfferExpired,    // الفني ما ردش في المهلة
    OnTheWay,
    WorkStarted,
    Completed,
    Reviewed,
    Cancelled,
    Escalated        // الطلب واقف مدة طويلة
}