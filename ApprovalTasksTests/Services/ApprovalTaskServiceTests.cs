using ApprovalTasks.Exceptions;
using ApprovalTasks.Interfaces;
using ApprovalTasks.Models;
using ApprovalTasks.Models.Requests;
using ApprovalTasks.Services;
using Moq;

namespace ApprovalTasksTests.Services
{
    public class ApprovalTaskServiceTests
    {
        private readonly Mock<IApprovalTaskRepository> repositoryMock;
        private readonly ApprovalTaskService service;

        private readonly Guid taskId = Guid.NewGuid();
        private readonly Guid assigneeId = Guid.NewGuid();
        private readonly Guid otherUserId = Guid.NewGuid();

        public ApprovalTaskServiceTests()
        {
            repositoryMock = new Mock<IApprovalTaskRepository>();
            service = new ApprovalTaskService(repositoryMock.Object);
        }

        private ApprovalTask CreateDomainTask(
            ApprovalTaskStatus status = ApprovalTaskStatus.Assigned)
        {
            var now = DateTimeOffset.UtcNow;

            return new ApprovalTask
            {
                Id = taskId,
                DocumentNumber = "DOC-001",
                Name = "Test document",
                AssigneeId = assigneeId,
                Status = status,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
        }

        private void SetupTask(ApprovalTask task)
        {
            repositoryMock
                .Setup(x => x.Get(task.Id))
                .Returns(task);
        }

        [Fact]
        public void Create_ShouldCreateAssignedTask()
        {
            // Arrange
            var request = new CreateApprovalTaskRequest
            {
                DocumentNumber = " DOC-001 ",
                Name = " Test document ",
                AssigneeId = assigneeId
            };

            ApprovalTask? createdTask = null;

            repositoryMock
                .Setup(x => x.Add(It.IsAny<ApprovalTask>()))
                .Callback<ApprovalTask>(task => createdTask = task);

            // Act
            var result = service.Create(request);

            // Assert
            Assert.NotNull(createdTask);

            Assert.Equal(createdTask.Id, result.Id);
            Assert.Equal("DOC-001", result.DocumentNumber);
            Assert.Equal("Test document", result.Name);
            Assert.Equal(assigneeId, result.AssigneeId);

            Assert.Equal("Assigned", result.Status);
            Assert.Empty(result.History);

            Assert.Equal(result.CreatedAt, result.UpdatedAt);

            repositoryMock.Verify(
                x => x.Add(It.IsAny<ApprovalTask>()),
                Times.Once);
        }

        [Fact]
        public void Get_WhenTaskExists_ShouldReturnTask()
        {
            // Arrange
            var task = CreateDomainTask();

            SetupTask(task);

            // Act
            var result = service.Get(taskId);

            // Assert
            Assert.Equal(taskId, result.Id);
            Assert.Equal("Assigned", result.Status);
            Assert.Empty(result.History);
        }

        [Fact]
        public void Get_WhenTaskDoesNotExist_ShouldThrowNotFound()
        {
            // Arrange
            repositoryMock
                .Setup(x => x.Get(taskId))
                .Returns((ApprovalTask?)null);

            // Act & Assert
            Assert.Throws<ApprovalTaskNotFoundException>(
                () => service.Get(taskId));
        }

        [Fact]
        public void ExecuteAction_Start_ShouldMoveTaskToInProgress()
        {
            // Arrange
            var task = CreateDomainTask();

            SetupTask(task);

            // Act
            var result = service.ExecuteAction(
                taskId,
                new ApprovalTaskActionRequest
                {
                    ActorId = assigneeId,
                    Action = "Start",
                    Comment = null
                });

            // Assert
            Assert.Equal("InProgress", result.Status);
            Assert.Single(result.History);

            Assert.Equal("Start", result.History[0].Action);
            Assert.Equal(assigneeId, result.History[0].ActorId);
            Assert.Null(result.History[0].Comment);
        }

        [Fact]
        public void ExecuteAction_StartThenApprove_ShouldApproveTask()
        {
            // Arrange
            var task = CreateDomainTask();

            SetupTask(task);

            service.ExecuteAction(
                taskId,
                new ApprovalTaskActionRequest
                {
                    ActorId = assigneeId,
                    Action = "Start",
                    Comment = null
                });

            // Act
            var result = service.ExecuteAction(
                taskId,
                new ApprovalTaskActionRequest
                {
                    ActorId = assigneeId,
                    Action = "Approve",
                    Comment = null
                });

            // Assert
            Assert.Equal("Approved", result.Status);
            Assert.Equal(2, result.History.Count);

            Assert.Equal("Start", result.History[0].Action);
            Assert.Equal("Approve", result.History[1].Action);

            Assert.Equal(assigneeId, result.History[1].ActorId);
            Assert.Null(result.History[1].Comment);
        }

        [Fact]
        public void ExecuteAction_StartThenReject_ShouldRejectTask()
        {
            // Arrange
            var task = CreateDomainTask();

            SetupTask(task);

            service.ExecuteAction(
                taskId,
                new ApprovalTaskActionRequest
                {
                    ActorId = assigneeId,
                    Action = "Start",
                    Comment = null
                });

            // Act
            var result = service.ExecuteAction(
                taskId,
                new ApprovalTaskActionRequest
                {
                    ActorId = assigneeId,
                    Action = "Reject",
                    Comment = "Invalid document"
                });

            // Assert
            Assert.Equal("Rejected", result.Status);
            Assert.Equal(2, result.History.Count);

            Assert.Equal("Reject", result.History[1].Action);
            Assert.Equal("Invalid document", result.History[1].Comment);
        }

        [Theory]
        [InlineData("Approve")]
        [InlineData("Reject")]
        public void ExecuteAction_FromAssigned_ShouldThrowConflict(
            string action)
        {
            // Arrange
            var task = CreateDomainTask();

            SetupTask(task);

            var beforeStatus = task.Status;
            var beforeHistoryCount = task.History.Count;

            // Act
            Assert.Throws<ApprovalTaskConflictException>(() =>
                service.ExecuteAction(
                    taskId,
                    new ApprovalTaskActionRequest
                    {
                        ActorId = assigneeId,
                        Action = action,
                        Comment = action == "Reject" ? "Reason" : null
                    }));

            // Assert
            Assert.Equal(beforeStatus, task.Status);
            Assert.Equal(beforeHistoryCount, task.History.Count);
        }

        [Fact]
        public void ExecuteAction_StartTwice_ShouldThrowConflict()
        {
            // Arrange
            var task = CreateDomainTask();

            SetupTask(task);

            service.ExecuteAction(
                taskId,
                new ApprovalTaskActionRequest
                {
                    ActorId = assigneeId,
                    Action = "Start",
                    Comment = null
                });

            var beforeStatus = task.Status;
            var beforeHistoryCount = task.History.Count;
            var beforeUpdatedAt = task.UpdatedAtUtc;

            // Act
            Assert.Throws<ApprovalTaskConflictException>(() =>
                service.ExecuteAction(
                    taskId,
                    new ApprovalTaskActionRequest
                    {
                        ActorId = assigneeId,
                        Action = "Start",
                        Comment = null
                    }));

            // Assert
            Assert.Equal(beforeStatus, task.Status);
            Assert.Equal(beforeHistoryCount, task.History.Count);
            Assert.Equal(beforeUpdatedAt, task.UpdatedAtUtc);
        }

        [Theory]
        [InlineData(ApprovalTaskStatus.Approved)]
        [InlineData(ApprovalTaskStatus.Rejected)]
        public void ExecuteAction_AfterFinalState_ShouldThrowConflict(
            ApprovalTaskStatus finalStatus)
        {
            // Arrange
            var task = CreateDomainTask(finalStatus);

            SetupTask(task);

            var beforeStatus = task.Status;
            var beforeHistoryCount = task.History.Count;
            var beforeUpdatedAt = task.UpdatedAtUtc;

            // Act
            Assert.Throws<ApprovalTaskConflictException>(() =>
                service.ExecuteAction(
                    taskId,
                    new ApprovalTaskActionRequest
                    {
                        ActorId = assigneeId,
                        Action = "Approve",
                        Comment = null
                    }));

            // Assert
            Assert.Equal(beforeStatus, task.Status);
            Assert.Equal(beforeHistoryCount, task.History.Count);
            Assert.Equal(beforeUpdatedAt, task.UpdatedAtUtc);
        }

        [Fact]
        public void ExecuteAction_WrongActor_ShouldThrowForbiddenAndNotChangeTask()
        {
            // Arrange
            var task = CreateDomainTask();

            SetupTask(task);

            var beforeStatus = task.Status;
            var beforeHistoryCount = task.History.Count;
            var beforeUpdatedAt = task.UpdatedAtUtc;

            // Act
            Assert.Throws<ApprovalTaskForbiddenException>(() =>
                service.ExecuteAction(
                    taskId,
                    new ApprovalTaskActionRequest
                    {
                        ActorId = otherUserId,
                        Action = "Start",
                        Comment = null
                    }));

            // Assert
            Assert.Equal(beforeStatus, task.Status);
            Assert.Equal(beforeHistoryCount, task.History.Count);
            Assert.Equal(beforeUpdatedAt, task.UpdatedAtUtc);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public void ExecuteAction_RejectWithoutComment_ShouldThrowValidation(
            string? comment)
        {
            // Arrange
            var task = CreateDomainTask(
                ApprovalTaskStatus.InProgress);

            SetupTask(task);

            var beforeStatus = task.Status;
            var beforeHistoryCount = task.History.Count;
            var beforeUpdatedAt = task.UpdatedAtUtc;

            // Act
            Assert.Throws<ApprovalTaskValidationException>(() =>
                service.ExecuteAction(
                    taskId,
                    new ApprovalTaskActionRequest
                    {
                        ActorId = assigneeId,
                        Action = "Reject",
                        Comment = comment
                    }));

            // Assert
            Assert.Equal(beforeStatus, task.Status);
            Assert.Equal(beforeHistoryCount, task.History.Count);
            Assert.Equal(beforeUpdatedAt, task.UpdatedAtUtc);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Unknown")]
        [InlineData("123")]
        public void ExecuteAction_InvalidAction_ShouldThrowValidation(
            string? action)
        {
            // Arrange
            var task = CreateDomainTask();

            SetupTask(task);

            var beforeStatus = task.Status;
            var beforeHistoryCount = task.History.Count;
            var beforeUpdatedAt = task.UpdatedAtUtc;

            // Act
            Assert.Throws<ApprovalTaskValidationException>(() =>
                service.ExecuteAction(
                    taskId,
                    new ApprovalTaskActionRequest
                    {
                        ActorId = assigneeId,
                        Action = action,
                        Comment = null
                    }));

            // Assert
            Assert.Equal(beforeStatus, task.Status);
            Assert.Equal(beforeHistoryCount, task.History.Count);
            Assert.Equal(beforeUpdatedAt, task.UpdatedAtUtc);
        }

        [Fact]
        public async Task ExecuteAction_TwoConcurrentFinalActions_ShouldAllowOnlyOne()
        {
            // Arrange
            var task = CreateDomainTask(
                ApprovalTaskStatus.InProgress);

            SetupTask(task);
            var approveRequest = new ApprovalTaskActionRequest
            {
                ActorId = assigneeId,
                Action = "Approve",
                Comment = null
            };

            var rejectRequest = new ApprovalTaskActionRequest
            {
                ActorId = assigneeId,
                Action = "Reject",
                Comment = "Reject"
            };

            var approveTask = Task.Run<object>(() =>
            {
                try
                {
                    return service.ExecuteAction(
                        taskId,
                        approveRequest);
                }
                catch (Exception ex)
                {
                    return ex;
                }
            });

            var rejectTask = Task.Run<object>(() =>
            {
                try
                {
                    return service.ExecuteAction(
                        taskId,
                        rejectRequest);
                }
                catch (Exception ex)
                {
                    return ex;
                }
            });

            // Act
            await Task.WhenAll(approveTask, rejectTask);

            var results = new[]
            {
            approveTask.Result,
            rejectTask.Result
        };

            // Assert
            Assert.Single(
                results.OfType<ApprovalTaskResponse>());

            Assert.Single(
                results.OfType<ApprovalTaskConflictException>());

            Assert.Contains(
                task.Status,
                new[]
                {
                ApprovalTaskStatus.Approved,
                ApprovalTaskStatus.Rejected
                });

            Assert.Single(task.History);
        }
    }
}
