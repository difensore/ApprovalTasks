using ApprovalTasks.Exceptions;
using ApprovalTasks.Interfaces;
using ApprovalTasks.Models;
using ApprovalTasks.Models.Requests;

namespace ApprovalTasks.Services
{
    public sealed class ApprovalTaskService : IApprovalTaskService
    {
        private readonly IApprovalTaskRepository repository;

        public ApprovalTaskService(
            IApprovalTaskRepository repository)
        {
            this.repository = repository;
        }

        public ApprovalTaskResponse Create(CreateApprovalTaskRequest request)
        {

            var now = DateTimeOffset.UtcNow;

            var task = new ApprovalTask
            {
                Id = Guid.NewGuid(),
                DocumentNumber = request.DocumentNumber!.Trim(),
                Name = request.Name!.Trim(),
                AssigneeId = request.AssigneeId,
                Status = ApprovalTaskStatus.Assigned,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            repository.Add(task);

            return ToResponse(task);
        }

        public ApprovalTaskResponse Get(Guid id)
        {
            var task = repository.Get(id);
            if(task == null)
            {
                throw new ApprovalTaskNotFoundException(id);
            }

            return ToResponse(task);
        }

        public ApprovalTaskResponse ExecuteAction(
            Guid id,
            ApprovalTaskActionRequest request)
        {
            var action = ParseAction(request.Action);

            var task = this.repository.Get(id);
            if (task == null)
            {
                throw new ApprovalTaskNotFoundException(id);
            }

            if (task.AssigneeId != request.ActorId)
            {
                throw new ApprovalTaskForbiddenException();
            }

            if (action == ApprovalAction.Reject &&
                string.IsNullOrWhiteSpace(request.Comment))
            {
                throw new ApprovalTaskValidationException(
                    "Comment is required when rejecting a task.");
            }

            var now = DateTimeOffset.UtcNow;
            lock (task.SyncRoot)
            {
                if (task.Status is
                ApprovalTaskStatus.Approved or
                ApprovalTaskStatus.Rejected)
                {
                    throw new ApprovalTaskConflictException(
                        "The task is already in a final state.");
                }

                var newStatus = GetNewStatus(task.Status, action);
                task.Status = newStatus;

                task.UpdatedAtUtc = now;

                task.History.Add(new ApprovalTaskHistoryEntry
                {
                    Action = action,
                    ActorId = request.ActorId,
                    Comment = string.IsNullOrWhiteSpace(request.Comment)
                        ? null
                        : request.Comment.Trim(),
                    TimestampUtc = now
                });
            }

            return ToResponse(task);

        }

        private static ApprovalAction ParseAction(string? action)
        {
            if (string.IsNullOrWhiteSpace(action))
            {
                throw new ApprovalTaskValidationException(
                    "Action is required.");
            }

            if (!Enum.TryParse<ApprovalAction>(
                action,
                ignoreCase: true,
                out var result) ||
                    !Enum.IsDefined(result))
            {
                throw new ApprovalTaskValidationException(
                    $"Unknown action '{action}'. Allowed actions: Start, Approve, Reject.");
            }

            return result;
        }

        private static ApprovalTaskStatus GetNewStatus(
            ApprovalTaskStatus currentStatus,
            ApprovalAction action)
        {
            return (currentStatus, action) switch
            {
                (ApprovalTaskStatus.Assigned, ApprovalAction.Start)
                    => ApprovalTaskStatus.InProgress,

                (ApprovalTaskStatus.InProgress, ApprovalAction.Approve)
                    => ApprovalTaskStatus.Approved,

                (ApprovalTaskStatus.InProgress, ApprovalAction.Reject)
                    => ApprovalTaskStatus.Rejected,

                _ => throw new ApprovalTaskConflictException(
                    $"Action '{action}' is not allowed when task is in '{currentStatus}' state.")
            };
        }

        public static ApprovalTaskResponse ToResponse(ApprovalTask task)
        {
            return new ApprovalTaskResponse(
                task.Id,
                task.DocumentNumber,
                task.Name,
                task.AssigneeId,
                task.Status.ToString(),
                task.CreatedAtUtc,
                task.UpdatedAtUtc,
                task.History
                    .Select(history => new ApprovalTaskHistoryResponse(
                        history.Action.ToString(),
                        history.ActorId,
                        history.Comment,
                        history.TimestampUtc))
                    .ToList());
        }
    }
}
