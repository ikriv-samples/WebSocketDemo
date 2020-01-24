using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace WebSocketSrv.Controllers
{
    [EnableCors("FreeForAll")]
    public class TimeController : ControllerBase
    {
        private readonly GlobalTimer _timer;

        public TimeController(GlobalTimer timer)
        {
            _timer = timer;
        }

        public async Task<string> Index([FromQuery] int ticks = 0)
        {
            var context = ControllerContext.HttpContext;
            if (context.WebSockets.IsWebSocketRequest)
            {
                await ProcessRequest(context.WebSockets, ticks);
                return null; // by this time the socket is closed, it does not matter what we return
            }

            return GetTime();
        }

        private static string GetTime()
        {
            var timeStr = DateTime.UtcNow.ToString("MMM dd yyyy HH:mm:ss.fff UTC", CultureInfo.InvariantCulture);
            var timeJson = JsonConvert.SerializeObject(timeStr);
            return timeJson;
        }

        private async Task ProcessRequest(WebSocketManager wsManager, int maxTicks)
        {
            var ws = await wsManager.AcceptWebSocketAsync();
            var sender = new WebSocketSender(ws);
            int ticks = 0;
            Action tickHandler = () =>
            {
                sender.QueueSend(GetTime());
                if (maxTicks != 0 && ++ticks >= maxTicks) sender.CloseAsync();
            };
            _timer.Tick += tickHandler;
            await sender.HandleCommunicationAsync();
            _timer.Tick -= tickHandler;
        }
    }
}
