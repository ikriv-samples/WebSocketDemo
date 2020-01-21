using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
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

        private static async Task ProcessRequest(AspNetWebSocketContext context)
        {
            var ws = context.WebSocket;
            var sender = new WebSocketSender(ws);
            Action tickHandler = () => { sender.QueueSend(GetTime()); };
            GlobalTimer.Instance.Tick += tickHandler;
            await sender.HandleCommunicationAsync();
            GlobalTimer.Instance.Tick -= tickHandler;
        }
    }
}
