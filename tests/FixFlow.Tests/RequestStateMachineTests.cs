using FixFlow.Domain.Common;
using FixFlow.Domain.Enums;
using Xunit;

namespace FixFlow.Tests;

public class RequestStateMachineTests
{
    [Theory]
    [InlineData(RequestStatus.Pending, RequestStatus.Assigned)]
    [InlineData(RequestStatus.Assigned, RequestStatus.Accepted)]
    [InlineData(RequestStatus.Assigned, RequestStatus.Pending)]     // الفني رفض
    [InlineData(RequestStatus.Accepted, RequestStatus.OnTheWay)]
    [InlineData(RequestStatus.OnTheWay, RequestStatus.InProgress)]
    [InlineData(RequestStatus.InProgress, RequestStatus.Completed)]
    [InlineData(RequestStatus.Completed, RequestStatus.Reviewed)]
    public void Allowed_transitions_are_accepted(RequestStatus from, RequestStatus to)
    {
        Assert.True(RequestStateMachine.CanTransition(from, to));
    }

    [Theory]
    [InlineData(RequestStatus.Pending, RequestStatus.Completed)]     // قفز
    [InlineData(RequestStatus.Accepted, RequestStatus.InProgress)]   // قفز
    [InlineData(RequestStatus.OnTheWay, RequestStatus.Cancelled)]    // مفيش إلغاء بعد ما الفني اتحرك
    [InlineData(RequestStatus.Reviewed, RequestStatus.Pending)]      // حالة نهائية
    [InlineData(RequestStatus.Cancelled, RequestStatus.Assigned)]    // حالة نهائية
    public void Forbidden_transitions_are_rejected(RequestStatus from, RequestStatus to)
    {
        Assert.False(RequestStateMachine.CanTransition(from, to));
    }
}