using ApprovalTasks.Models;

namespace ApprovalTasks.Interfaces
{
    public interface IApprovalTaskRepository
    {
        ApprovalTask? Get(Guid id);

        void Add(ApprovalTask task);
    }
}
