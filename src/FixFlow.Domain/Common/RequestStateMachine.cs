using FixFlow.Domain.Enums;
using static FixFlow.Domain.Enums.RequestStatus;

namespace FixFlow.Domain.Common;

public static class RequestStateMachine
{
    private static readonly Dictionary<RequestStatus, RequestStatus[]> Allowed = new()
    {
        [Pending] = new[] { Assigned, Cancelled },
        [Assigned] = new[] { Accepted, Pending, Cancelled },   // Pending = الفني رفض
        [Accepted] = new[] { OnTheWay, Cancelled },
        [OnTheWay] = new[] { InProgress },
        [InProgress] = new[] { Completed },
        [Completed] = new[] { Reviewed },
        [Reviewed] = Array.Empty<RequestStatus>(),
        [Cancelled] = Array.Empty<RequestStatus>(),
        [Rejected] = Array.Empty<RequestStatus>()
    };

    public static bool CanTransition(RequestStatus from, RequestStatus to)
        => Allowed.TryGetValue(from, out var targets) && targets.Contains(to);
}