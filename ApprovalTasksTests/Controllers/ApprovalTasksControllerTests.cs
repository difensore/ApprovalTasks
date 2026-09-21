using ApprovalTasks.Controllers;
using ApprovalTasks.Exceptions;
using ApprovalTasks.Interfaces;
using ApprovalTasks.Models;
using ApprovalTasks.Models.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ApprovalTasksTests.Controllers
{
    public class ApprovalTasksControllerTests
    {
        private readonly Mock<IApprovalTaskService> serviceMock;
        private readonly ApprovalTasksController controller;

        private readonly Guid taskId = Guid.NewGuid();
        private readonly Guid assigneeId = Guid.NewGuid();
        private readonly Guid actorId = Guid.NewGuid();

        public ApprovalTasksControllerTests()
        {
            serviceMock = new Mock<IApprovalTaskService>();
            controller = new ApprovalTasksController(serviceMock.Object);
        }

        [Fact]
        public void Get_WhenTaskExists_ShouldReturn200Ok()
        {
            var response = CreateResponse();

            serviceMock
                .Setup(x => x.Get(taskId))
                .Returns(response);

            var result = controller.Get(taskId);

            var okResult = Assert.IsType<OkObjectResult>(result);

            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.Same(response, okResult.Value);

            serviceMock.Verify(
                x => x.Get(taskId),
                Times.Once);
        }

        [Fact]
        public void Get_WhenTaskDoesNotExist_ShouldReturn404NotFound()
        {
            serviceMock
                .Setup(x => x.Get(taskId))
                .Throws(new ApprovalTaskNotFoundException(taskId));

            var result = controller.Get(taskId);

            var notFoundResult =
                Assert.IsType<NotFoundObjectResult>(result);

            Assert.Equal(
                StatusCodes.Status404NotFound,
                notFoundResult.StatusCode);

            serviceMock.Verify(
                x => x.Get(taskId),
                Times.Once);
        }

        [Fact]
        public void Create_WhenValid_ShouldReturn201Created()
        {
            var request = new CreateApprovalTaskRequest
            {
                DocumentNumber = "DOC-001",
                Name = "Test approval",
                AssigneeId = assigneeId
            };

            var response = CreateResponse();

            serviceMock
                .Setup(x => x.Create(request))
                .Returns(response);

            var result = controller.Create(request);

            var createdResult =
                Assert.IsType<CreatedAtActionResult>(result);

            Assert.Equal(
                StatusCodes.Status201Created,
                createdResult.StatusCode);

            Assert.Equal(
                nameof(ApprovalTasksController.Create),
                createdResult.ActionName);

            Assert.Equal(
                taskId,
                createdResult.RouteValues!["id"]);

            Assert.Same(response, createdResult.Value);

            serviceMock.Verify(
                x => x.Create(request),
                Times.Once);
        }

        [Fact]
        public void Create_WhenValidationFails_ShouldReturn400BadRequest()
        {
            var request = new CreateApprovalTaskRequest
            {
                DocumentNumber = "",
                Name = "Test approval",
                AssigneeId = assigneeId
            };

            serviceMock
                .Setup(x => x.Create(request))
                .Throws(new ApprovalTaskValidationException(
                    "Document number is required."));

            var result = controller.Create(request);

            var badRequestResult =
                Assert.IsType<BadRequestObjectResult>(result);

            Assert.Equal(
                StatusCodes.Status400BadRequest,
                badRequestResult.StatusCode);
        }

        [Fact]
        public void Create_WhenConflictOccurs_ShouldReturn409Conflict()
        {
            var request = new CreateApprovalTaskRequest
            {
                DocumentNumber = "DOC-001",
                Name = "Test approval",
                AssigneeId = assigneeId
            };

            serviceMock
                .Setup(x => x.Create(request))
                .Throws(new ApprovalTaskConflictException(
                    "Document already exists."));

            var result = controller.Create(request);

            var conflictResult =
                Assert.IsType<ConflictObjectResult>(result);

            Assert.Equal(
                StatusCodes.Status409Conflict,
                conflictResult.StatusCode);
        }

        [Fact]
        public void ExecuteAction_WhenSuccessful_ShouldReturn200Ok()
        {
            var request = new ApprovalTaskActionRequest
            {
                ActorId = actorId,
                Action = "Start",
                Comment = null
            };

            var response = CreateResponse("InProgress");

            serviceMock
                .Setup(x => x.ExecuteAction(taskId, request))
                .Returns(response);

            var result = controller.ExecuteAction(taskId, request);

            var okResult =
                Assert.IsType<OkObjectResult>(result);

            Assert.Equal(
                StatusCodes.Status200OK,
                okResult.StatusCode);

            Assert.Same(response, okResult.Value);

            serviceMock.Verify(
                x => x.ExecuteAction(taskId, request),
                Times.Once);
        }

        [Fact]
        public void ExecuteAction_WhenValidationFails_ShouldReturn400BadRequest()
        {
            var request = new ApprovalTaskActionRequest
            {
                ActorId = actorId,
                Action = "UnknownAction",
                Comment = null
            };

            serviceMock
                .Setup(x => x.ExecuteAction(taskId, request))
                .Throws(new ApprovalTaskValidationException(
                    "Unknown action."));

            var result = controller.ExecuteAction(taskId, request);

            var badRequestResult =
                Assert.IsType<BadRequestObjectResult>(result);

            Assert.Equal(
                StatusCodes.Status400BadRequest,
                badRequestResult.StatusCode);
        }

        [Fact]
        public void ExecuteAction_WhenTaskDoesNotExist_ShouldReturn404NotFound()
        {
            var request = new ApprovalTaskActionRequest
            {
                ActorId = actorId,
                Action = "Start",
                Comment = null
            };

            serviceMock
                .Setup(x => x.ExecuteAction(taskId, request))
                .Throws(new ApprovalTaskNotFoundException(taskId));

            var result = controller.ExecuteAction(taskId, request);

            var notFoundResult =
                Assert.IsType<NotFoundObjectResult>(result);

            Assert.Equal(
                StatusCodes.Status404NotFound,
                notFoundResult.StatusCode);
        }

        [Fact]
        public void ExecuteAction_WhenActorIsNotAssignee_ShouldReturn403Forbidden()
        {
            var request = new ApprovalTaskActionRequest
            {
                ActorId = actorId,
                Action = "Start",
                Comment = null
            };

            serviceMock
                .Setup(x => x.ExecuteAction(taskId, request))
                .Throws(new ApprovalTaskForbiddenException());

            var result = controller.ExecuteAction(taskId, request);

            var objectResult =
                Assert.IsType<ObjectResult>(result);

            Assert.Equal(
                StatusCodes.Status403Forbidden,
                objectResult.StatusCode);
        }

        [Fact]
        public void ExecuteAction_WhenTransitionIsInvalid_ShouldReturn409Conflict()
        {
            var request = new ApprovalTaskActionRequest
            {
                ActorId = actorId,
                Action = "Approve",
                Comment = null
            };

            serviceMock
                .Setup(x => x.ExecuteAction(taskId, request))
                .Throws(new ApprovalTaskConflictException(
                    "Action is not allowed."));

            var result = controller.ExecuteAction(taskId, request);

            var conflictResult =
                Assert.IsType<ConflictObjectResult>(result);

            Assert.Equal(
                StatusCodes.Status409Conflict,
                conflictResult.StatusCode);
        }

        private ApprovalTaskResponse CreateResponse(
            string status = "Assigned")
        {
            return new ApprovalTaskResponse(
                taskId,
                "DOC-001",
                "Test approval",
                assigneeId,
                status,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                []);
        }
    }
}
