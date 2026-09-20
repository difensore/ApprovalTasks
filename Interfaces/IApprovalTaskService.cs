using ApprovalTasks.Models;
using ApprovalTasks.Models.Requests;

namespace ApprovalTasks.Interfaces
{
    public interface IApprovalTaskService
    {
        ApprovalTaskResponse Create(CreateApprovalTaskRequest request);

        public ApprovalTaskResponse Get(Guid id);

        public ApprovalTaskResponse ExecuteAction(
            Guid id,
            ApprovalTaskActionRequest request);
    }
}
