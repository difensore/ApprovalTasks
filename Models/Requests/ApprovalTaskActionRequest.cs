using System.ComponentModel.DataAnnotations;

namespace ApprovalTasks.Models.Requests
{
    public sealed class ApprovalTaskActionRequest
    {
        public Guid ActorId { get; init; }

        [Required]
        public string Action { get; init; }

        public string? Comment { get; init; }
    }
}
