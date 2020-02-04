using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebSocketsDemo.Common
{
    public class AsyncQueue<T>
    {
        private readonly List<T> _queue = new List<T>();
        private TaskCompletionSource<bool> _queueIsNoLongerEmpty = new TaskCompletionSource<bool>();

        public void Enqueue(T data)
        {
            lock (_queue)
            {
                _queue.Add(data);
                if (_queue.Count == 1)
                {
                    _queueIsNoLongerEmpty.SetResult(true);
                }
            }
        }

        public void EnqueueIfEmpty(T data)
        {
            lock (_queue)
            {
                if (_queue.Count > 0) return;
                Enqueue(data); // will lock _queue again, but this is fine
            }
        }

        public async Task<T[]> DequeueAsync()
        {
            await _queueIsNoLongerEmpty.Task;

            T[] result;

            lock (_queue)
            {
                if (_queue.Count == 0)
                {
                    // something went very wrong
                    throw new InvalidOperationException(
                        "Logical error: queue should not be empty when _queueIsNoLongerEmpty is finished");
                }

                result = _queue.ToArray();
                _queue.Clear();

                // the queue is now empty; renew "is not longer empty" task completion source, since task can be completed only once
                _queueIsNoLongerEmpty = new TaskCompletionSource<bool>();
            }

            return result;
        }
    }
}
