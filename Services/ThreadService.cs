using System.Collections.Concurrent;

namespace DreamBot.Services
{
    internal class ThreadService
    {
        private readonly ConcurrentDictionary<Guid, Thread> _threads;

        public ThreadService()
        {
            _threads = new ConcurrentDictionary<Guid, Thread>();
        }

        public void Enqueue(Func<Task> action)
        {
            Guid threadId = Guid.NewGuid();

            Thread t = new(async () =>
            {
                try
                {
                    await action.Invoke();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                _ = _threads.TryRemove(threadId, out _);
            });

            _ = _threads.TryAdd(threadId, t);

            t.Start();
        }

        public void Enqueue(Action action)
        {
            Guid threadId = Guid.NewGuid();

            Thread t = new(() =>
            {
                action.Invoke();

                _ = _threads.TryRemove(threadId, out _);
            });

            _ = _threads.TryAdd(threadId, t);

            t.Start();
        }
    }
}