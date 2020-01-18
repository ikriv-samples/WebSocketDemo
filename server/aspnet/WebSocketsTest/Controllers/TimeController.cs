using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.WebSockets;
using Newtonsoft.Json;

namespace WebSocketsTest.Controllers
{
    [EnableCors(origins:"*", headers:"*", methods:"GET")]
    public class TimeController : ApiController
    {
        [HttpGet]
        public HttpResponseMessage GetResponse()
        {
            var context = HttpContext.Current;
            if (context.IsWebSocketRequest)
            {
                context.AcceptWebSocketRequest(ProcessRequest);
                return new HttpResponseMessage(HttpStatusCode.SwitchingProtocols);
            }

            var time = GetTime();
            var msg = Request.CreateResponse(HttpStatusCode.OK);
            msg.Content = new StringContent(time, Encoding.UTF8, "application/text");
            return msg;
        }

        private static string GetTime()
        {
            var timeStr = DateTime.UtcNow.ToString("MMM dd yyyy HH:mm:ss.fff UTC", CultureInfo.InvariantCulture);
            var timeJson = JsonConvert.SerializeObject(timeStr);
            return timeJson;
        }

        private async Task ProcessRequest(AspNetWebSocketContext context)
        {
            var ws = context.WebSocket;
            await Task.WhenAll(WriteTask(ws), ReadTask(ws));
        }

        // MUST read if we want the socket state to be updated
        private static async Task ReadTask(WebSocket ws)
        {
            var buffer = new ArraySegment<byte>(new byte[1024]);
            while (true)
            {
                await ws.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                if (ws.State != WebSocketState.Open) break;
            }
        }

        private static async Task WriteTask(WebSocket ws)
        {
            while (true)
            {
                var timeStr = GetTime();
                var buffer = Encoding.UTF8.GetBytes(timeStr);
                if (ws.State != WebSocketState.Open) break;
                var sendTask = ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                await sendTask.ConfigureAwait(false);
                if (ws.State != WebSocketState.Open) break;
                await Task.Delay(1000).ConfigureAwait(false); // this is does not guarantee exact timing!
            }
        }

    }
}
