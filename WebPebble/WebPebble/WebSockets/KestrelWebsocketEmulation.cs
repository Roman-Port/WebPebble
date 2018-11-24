using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace WebPebble.WebSockets
{
    /// <summary>
    /// Emulates the WebSocketSharp interface using Kestrel.
    /// </summary>
    public class KestrelWebsocketEmulation
    {
        public static async Task StartSession(HttpContext context, WebSocket webSocket)
        {
            return;
        }
    }
}
