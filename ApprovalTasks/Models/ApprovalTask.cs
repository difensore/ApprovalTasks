namespace ApprovalTasks.Models
{
    public class ApprovalTask
    {
        public Guid Id { get; init; }

        public object SyncRoot { get; } = new();

        public string DocumentNumber { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public Guid AssigneeId { get; init; }

        public ApprovalTaskStatus Status { get; set; }

        public DateTimeOffset CreatedAtUtc { get; init; }

        public DateTimeOffset UpdatedAtUtc { get; set; }

        public List<ApprovalTaskHistoryEntry> History { get; } = [];
    }
}
