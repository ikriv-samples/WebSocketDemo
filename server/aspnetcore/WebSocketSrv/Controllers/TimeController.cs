using System;
using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
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
        public async Task<string> Index()
        {
            var context = ControllerContext.HttpContext;
            if (context.WebSockets.IsWebSocketRequest)
            {
                await ProcessRequest(context.WebSockets);
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

        private static async Task ProcessRequest(WebSocketManager wsManager)
        {
            var ws = await wsManager.AcceptWebSocketAsync();
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
