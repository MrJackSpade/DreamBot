using DreamBot.Models;
using System.Diagnostics.CodeAnalysis;

namespace DreamBot.Collections
{
    public class TaskCollection
    {
        private readonly object _lock = new();

        private readonly int _maxUserTasks;

        private readonly List<QueuedTask> _tasks = [];

        public TaskCollection(int maxUserTasks)
        {
            _maxUserTasks = maxUserTasks;
        }

        /// <summary>
        /// True if user has reached their limit
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public bool CheckLimit(ulong userId)
        {
            lock (_lock)
            {
                int found = 0;

                foreach (QueuedTask task in _tasks)
                {
                    if (task.UserId == userId)
                    {
                        found++;
                    }

                    if (found >= _maxUserTasks)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void Remove(QueuedTask task)
        {
            lock (_lock)
            {
                _tasks.Remove(task);
            }
        }

        public bool TryCancel(ulong messageId)
        {
            lock (_lock)
            {
                foreach (QueuedTask task in _tasks)
                {
                    if (task.MessageId == messageId)
                    {
                        task.Cancel();
                        this.Remove(task);
                        return true;
                    }
                }
            }

            return false;
        }

        internal bool TryReserve(ulong userId, [NotNullWhen(true)] out QueuedTask? queuedTask)
        {
            lock (_lock)
            {
                if (this.CheckLimit(userId))
                {
                    queuedTask = null;
                    return false;
                }
                else
                {
                    queuedTask = new(userId);
                    _tasks.Add(queuedTask);
                    return true;
                }
            }
        }
    }
}