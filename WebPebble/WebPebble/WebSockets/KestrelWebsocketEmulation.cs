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
        public WebSocket ws;
        private byte[] buf = new byte[4096];

        public static async Task<KestrelWebsocketEmulation> StartSession(HttpContext context, WebSocket ws)
        {
            //Generate our class and start listening.
            KestrelWebsocketEmulation emu = new KestrelWebsocketEmulation();
            emu.ws = ws;

            await emu.WaitForMessage();

            return emu;
        }

        public async Task WaitForMessage()
        {
            while (ws.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buf), new System.Threading.CancellationToken());
                Console.WriteLine("Got message " + Encoding.UTF8.GetString(buf));
            }
        }
    }
}
