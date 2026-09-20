namespace ApprovalTasks.Exceptions
{
    public sealed class ApprovalTaskNotFoundException : Exception
    {
        public ApprovalTaskNotFoundException(Guid id)
            : base($"Approval task '{id}' was not found.")
        {
        }
    }
}
