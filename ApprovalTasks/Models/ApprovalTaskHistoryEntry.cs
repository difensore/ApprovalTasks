namespace ApprovalTasks.Models
{
    public class ApprovalTaskHistoryEntry
    {
        public ApprovalAction Action { get; init; }

        public Guid ActorId { get; init; }

        public string? Comment { get; init; }

        public DateTimeOffset TimestampUtc { get; init; }
    }
}
