namespace ApprovalTasks.Models
{
    public sealed record ApprovalTaskResponse(
        Guid Id,
        string DocumentNumber,
        string Name,
        Guid AssigneeId,
        string Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        IReadOnlyList<ApprovalTaskHistoryResponse> History);
}
