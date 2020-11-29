using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Objects.Wrappers
{
    public sealed class ExecutionQueue
    {
        private readonly BlockingCollection<Func<Task>> _queue = new BlockingCollection<Func<Task>>();

        public ExecutionQueue() => Completion = Task.Run(() => ProcessQueueAsync());

        public Task Completion { get; }

        public void Complete() => _queue.CompleteAdding();

        private async Task ProcessQueueAsync()
        {
            foreach (var value in _queue.GetConsumingEnumerable())
                await value();
        }
    }
}
