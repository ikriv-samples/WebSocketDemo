using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketsTest
{
    public class WebSocketSender
    {
        private readonly WebSocket _webSocket;
        private readonly List<Request> _sendQueue = new List<Request>();
        private TaskCompletionSource<bool> _sendQueueIsNoLongerEmpty;
        private Task _communicationTask;

        private struct Request
        {
            public string Data;
            public bool IsCloseRequest;
        }
        
        public WebSocketSender(WebSocket webSocket, Encoding encoding = null)
        {
            _webSocket = webSocket;
            Encoding = encoding ?? Encoding.UTF8;
            _sendQueueIsNoLongerEmpty = new TaskCompletionSource<bool>();
        }

        public Encoding Encoding { get; }


        public void QueueSend(string data)
        {
            QueueSend(new Request { Data = data });
        }

        public Task CloseAsync()
        {
            if (_communicationTask == null) return CloseSocketAsync();
            QueueSend(new Request { IsCloseRequest = true });
            return _communicationTask;
        }

        private Task CloseSocketAsync()
        {
            // CloseAsync() does not work here: it throws throws InvalidOperationException
            // "a receiver operation is already in progress".
            // This is presumably because it tries to read the acknowledgement from the client,
            // but we already have a reading operation in progress, initiated in ReadTask()
            return _webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None);
        }

        private void QueueSend(Request request)
        {
            lock (_sendQueue)
            {
                _sendQueue.Add(request);
                if (_sendQueue.Count == 1)
                {
                    _sendQueueIsNoLongerEmpty?.SetResult(true);
                }
            }
        }

        public Task HandleCommunicationAsync()
        {
            if (_communicationTask != null)
            {
                throw new InvalidOperationException("Detected a second call to HandleCommunicationAsync(). Please call HandleCommunicationAsync() only once");
            }

            _communicationTask = Task.WhenAll(ReceiveTask(), SendTask());
            return _communicationTask;
        }

        private async Task ReceiveTask()
        {
            var buffer = new ArraySegment<byte>(new byte[1024]);
            while (true)
            {
                await _webSocket.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                if (_webSocket.State != WebSocketState.Open) return;
            }
        }

        private async Task SendTask()
        {
            while (true)
            {
                if (_webSocket.State != WebSocketState.Open) return;
                await _sendQueueIsNoLongerEmpty.Task.ConfigureAwait(false);

                Request[] toSend;

                lock (_sendQueue)
                {
                    if (_sendQueue.Count == 0)
                    {
                        // something went very wrong
                        throw new InvalidOperationException(
                            "Logical error: queue should not be empty when _sendQueueIsNoLongerEmpty is finished");
                    }

                    // take all strings to send that appear before the close request (if any)
                    toSend = _sendQueue.ToArray();
                    _sendQueue.Clear();

                    // the queue is now empty; renew "is not longer empty" task completion source, since task can be completed only once
                    _sendQueueIsNoLongerEmpty = new TaskCompletionSource<bool>();
                }

                foreach (var request in toSend)
                {
                    if (request.IsCloseRequest)
                    {
                        if (_webSocket.State == WebSocketState.Open)
                        {
                            await CloseSocketAsync();
                        }

                        return;
                    }

                    if (_webSocket.State != WebSocketState.Open)
                    {
                        throw new InvalidOperationException("Write operation failed, the socket is no longer open");
                    }

                    var buffer = Encoding.UTF8.GetBytes(request.Data);
                    var sendTask = _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                    await sendTask.ConfigureAwait(false);
                }
            }
        }
    }
}
