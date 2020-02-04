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
using WebSocketsDemo.Common;

namespace WebSocketsTest.Controllers
{
    [EnableCors(origins:"*", headers:"*", methods:"GET")]
    public class TimeController : ApiController
    {
        [HttpGet]
        public HttpResponseMessage GetResponse([FromUri] int ticks = 0)
        {
            var context = HttpContext.Current;
            if (context.IsWebSocketRequest)
            {
                context.AcceptWebSocketRequest(c=>ProcessRequest(c, ticks));
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

        private static async Task ProcessRequest(AspNetWebSocketContext context, int maxTicks)
        {
            var ws = context.WebSocket;
            var sender = new WebSocketSender(ws);
            int ticks = 0;
            Action tickHandler = () =>
            {
                sender.QueueSend(GetTime());
                if (maxTicks != 0 && ++ticks >= maxTicks) sender.CloseAsync();
            };

            GlobalTimer.Instance.Tick += tickHandler;
            try
            {
                await sender.HandleCommunicationAsync();
            }
            finally
            {
                GlobalTimer.Instance.Tick -= tickHandler;
            }
        }
    }
}
