namespace ApprovalTasks.Exceptions
{
    public sealed class ApprovalTaskConflictException : Exception
    {
        public ApprovalTaskConflictException(string message)
            : base(message)
        {
        }
    }
}
