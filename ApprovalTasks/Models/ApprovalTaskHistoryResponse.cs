namespace ApprovalTasks.Models
{
    public sealed record ApprovalTaskHistoryResponse(
        string Action,
        Guid ActorId,
        string? Comment,
        DateTimeOffset Timestamp);
}
