namespace ApprovalTasks.Exceptions
{
    public sealed class ApprovalTaskForbiddenException : Exception
    {
        public ApprovalTaskForbiddenException()
            : base("Only the assigned user can perform actions on this task.")
        {
        }
    }
}
