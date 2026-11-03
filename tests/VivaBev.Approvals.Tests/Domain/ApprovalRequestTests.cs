using VivaBev.Approvals.Domain;
using VivaBev.SharedKernel.Domain;
using Xunit;

namespace VivaBev.Approvals.Tests.Domain;

public class ApprovalRequestTests
{
    private static readonly Guid Requester = Guid.NewGuid();
    private static readonly Guid Manager = Guid.NewGuid();
    private static readonly Guid Director = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc);

    private static ApprovalRequest NewRequest(params (string Name, Guid ApproverId)[] approvers) =>
        new("Requisition", Guid.NewGuid(), "REQ-2027-00001", "Laptops", 30_000_000m, Requester, Now, approvers);

    [Fact]
    public void Steps_are_decided_in_order()
    {
        var request = NewRequest(("Line manager", Manager), ("Managing Director", Director));

        Assert.Throws<DomainException>(() => request.Approve(Director, null, Now)); // not their turn yet

        request.Approve(Manager, "Fine by me", Now);
        Assert.Equal(ApprovalStatus.Pending, request.Status);
        Assert.Equal(Director, request.CurrentStep!.ApproverId);

        request.Approve(Director, null, Now);
        Assert.Equal(ApprovalStatus.Approved, request.Status);
        Assert.Null(request.CurrentStep);
    }

    [Fact]
    public void The_requester_never_approves_their_own_request()
    {
        var request = NewRequest(("Line manager", Requester), ("Managing Director", Director));

        Assert.Equal(Director, request.Steps.Single().ApproverId);
    }

    [Fact]
    public void Each_person_approves_only_once()
    {
        var request = NewRequest(
            ("Line manager", Manager), ("Head of department", Manager), ("Managing Director", Director));

        var steps = request.Steps.OrderBy(s => s.Sequence).ToList();
        Assert.Equal(new[] { Manager, Director }, steps.Select(s => s.ApproverId));
        Assert.Equal("Line manager", steps[0].Name);
        Assert.Equal(2, steps[1].Sequence);
    }

    [Fact]
    public void Rejecting_needs_a_reason_and_ends_the_request()
    {
        var request = NewRequest(("Line manager", Manager), ("Managing Director", Director));

        Assert.Throws<DomainException>(() => request.Reject(Manager, "  ", Now));

        request.Reject(Manager, "Use the spare laptops in stores", Now);

        Assert.Equal(ApprovalStatus.Rejected, request.Status);
        Assert.Throws<DomainException>(() => request.Approve(Director, null, Now));
    }

    [Fact]
    public void A_request_nobody_else_can_approve_is_refused() =>
        Assert.Throws<DomainException>(() => NewRequest(("Line manager", Requester)));
}
