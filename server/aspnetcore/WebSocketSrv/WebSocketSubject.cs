using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace WebSocketSrv
{
    public interface IChannel<T>
    {
        void Write(T value);
        event Action<T> DataReceived;
        event Action<Exception> Error;
    }

    public class WebSocketSubject<TSend, TReceive> : IObserver<TSend>, IObservable<TReceive>
    {
        /// <summary>
        /// Closes the web socket and stops communication
        /// </summary>
        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            // This call does not make much sense for web sockets
            // The semantics woudl
        }

        public void OnNext(TSend value)
        {
            throw new NotImplementedException();
        }

        public IDisposable Subscribe(IObserver<TReceive> observer)
        {
            throw new NotImplementedException();
        }
    }
}
