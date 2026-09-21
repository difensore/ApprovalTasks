using ApprovalTasks.Exceptions;
using ApprovalTasks.Interfaces;
using ApprovalTasks.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace ApprovalTasks.Controllers
{
    [ApiController]
    [Route("api/approval-tasks")]
    public sealed class ApprovalTasksController : ControllerBase
    {
        private readonly IApprovalTaskService approvalTaskService;

        public ApprovalTasksController(IApprovalTaskService service)
        {
            approvalTaskService = service;
        }

        [HttpGet("{id:guid}")]
        public IActionResult Get(Guid id)
        {
            try
            {
                return Ok(approvalTaskService.Get(id));
            }
            catch (ApprovalTaskNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public IActionResult Create(CreateApprovalTaskRequest request)
        {
            try
            {
                var task = approvalTaskService.Create(request);

                return CreatedAtAction(
                    nameof(Create),
                    new { id = task.Id },
                    task);
            }
            catch (ApprovalTaskValidationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (ApprovalTaskConflictException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("{id:guid}/actions")]
        public IActionResult ExecuteAction(
            Guid id,
            ApprovalTaskActionRequest request)
        {
            try
            {
                var task = approvalTaskService.ExecuteAction(id, request);

                return Ok(task);
            }
            catch (ApprovalTaskValidationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (ApprovalTaskNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
            catch (ApprovalTaskForbiddenException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = ex.Message
                });
            }
            catch (ApprovalTaskConflictException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }
    }
}
