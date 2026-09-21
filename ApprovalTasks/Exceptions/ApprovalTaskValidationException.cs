namespace ApprovalTasks.Exceptions
{
    public sealed class ApprovalTaskValidationException : Exception
    {
        public ApprovalTaskValidationException(string message)
            : base(message)
        {
        }
    }
}
