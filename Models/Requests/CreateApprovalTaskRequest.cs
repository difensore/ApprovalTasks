using System.ComponentModel.DataAnnotations;

namespace ApprovalTasks.Models.Requests
{
    public sealed class CreateApprovalTaskRequest
    {
        [Required]
        public string? DocumentNumber { get; init; }

        [Required]
        public string? Name { get; init; }

        public Guid AssigneeId { get; init; }
    }
}
