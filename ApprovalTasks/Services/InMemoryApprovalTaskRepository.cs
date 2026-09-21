using ApprovalTasks.Exceptions;
using ApprovalTasks.Interfaces;
using ApprovalTasks.Models;
using System.Collections.Concurrent;

namespace ApprovalTasks.Services
{
    public sealed class InMemoryApprovalTaskRepository : IApprovalTaskRepository
    {
        private readonly ConcurrentDictionary<Guid, ApprovalTask> tasks = new();
        private readonly object sync = new();

        public ApprovalTask? Get(Guid id)
        {
            return tasks.TryGetValue(id, out var task)
                ? task
                : null;
        }

        public void Add(ApprovalTask task)
        {
            lock (sync)
            {
                if (tasks.Values.Any(t =>
                    t.DocumentNumber == task.DocumentNumber))
                {
                    throw new ApprovalTaskConflictException(
                        $"Task for document '{task.DocumentNumber}' already exists.");
                }

                if (!tasks.TryAdd(task.Id, task))
                {
                    throw new InvalidOperationException(
                        $"Task with id '{task.Id}' already exists.");
                }
            }
        }
    }
}
