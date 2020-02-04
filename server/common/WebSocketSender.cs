using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketsDemo.Common
{
    /// <summary>
    /// Sends incoming data stream to a web socket
    /// </summary>
    /// <remarks>Call HandleCommunicationAsync() to start working with the socket.
    /// Call QueueSend() to schedule sending message to the socket. It will be sent after all messages before it
    /// have been sent. Call CloseAsync() to close the socket from the server end.</remarks>
    public class WebSocketSender
    {
        private readonly WebSocket _webSocket;
        private readonly AsyncQueue<Request> _sendQueue = new AsyncQueue<Request>();
        private Task _communicationTask;

        private struct Request
        {
            public string Data;
            public bool IsCloseRequest;
        }

        /// <summary>
        /// Associate WebSocketSender with the web socket. Default encoding is UTF8
        /// </summary>
        public WebSocketSender(WebSocket webSocket, Encoding encoding = null)
        {
            _webSocket = webSocket;
            Encoding = encoding ?? Encoding.UTF8;
        }

        public Encoding Encoding { get; }

        /// <summary>
        /// Start handling web socket communication
        /// </summary>
        /// <returns>The task ends when the web socket is closed by either side</returns>
        public Task HandleCommunicationAsync()
        {
            if (_communicationTask != null)
            {
                throw new InvalidOperationException("Detected a second call to HandleCommunicationAsync(). Please call HandleCommunicationAsync() only once");
            }

            _communicationTask = Task.WhenAll(ReceiveTask(), SendTask());
            return _communicationTask;
        }

        /// <summary>
        /// Add message to be sent to the socket
        /// </summary>
        /// <remarks>Messages are sent in the order they were queued, when the socket is ready to accept them</remarks>
        /// <param name="data"></param>
        public void QueueSend(string data)
        {
            _sendQueue.Enqueue(new Request { Data = data });
        }

        /// <summary>
        /// Request to close the socket from the server end
        /// </summary>
        /// <returns>Task that is completed when the socket is closed</returns>
        public Task CloseAsync()
        {
            if (_communicationTask == null) return CloseSocketAsync();
            _sendQueue.Enqueue(new Request { IsCloseRequest = true });
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

        // Monitors socket status and returns when the client closes the socket
        private async Task ReceiveTask()
        {
            var buffer = new ArraySegment<byte>(new byte[1024]);
            while (true)
            {
                try
                {
                    await _webSocket.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Error reading from the socket
                    // If the client just dropped the connection prematurely, we can ignore it; otherwise log
                    bool canIgnoreException = (ex as WebSocketException)?.WebSocketErrorCode ==
                                              WebSocketError.ConnectionClosedPrematurely;

                    if (!canIgnoreException)
                    {
                        // log the exception; this sample just sends it to the standard output
                        Console.WriteLine(ex);
                    }
                }

                if (_webSocket.State != WebSocketState.Open)
                {
                    // Client closed the socket; if the send queue is empty, wake up the write task by pushing the close request
                    // If the client closed the socket in response to the closure from our (server) side, this might be the second
                    // close request in the queue, but that's OK, the SendTask() will quit after the first close request, and
                    // this request will simply be ignored
                    _sendQueue.EnqueueIfEmpty(new Request { IsCloseRequest =  true} );
                    return;
                }
            }
        }

        // Send data to the socket as it becomes available
        private async Task SendTask()
        {
            while (true)
            {
                if (_webSocket.State != WebSocketState.Open) return;
                Request[] toSend = await _sendQueue.DequeueAsync().ConfigureAwait(false);

                // process dequeued requests one by one
                foreach (var request in toSend)
                {
                    if (request.IsCloseRequest)
                    {
                        // Close the socket only if it is still open; otherwise just finish SendTask
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
