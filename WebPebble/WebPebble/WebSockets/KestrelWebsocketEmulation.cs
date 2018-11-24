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

        public async Task StartSession(HttpContext context, WebSocket ws)
        {
            //Generate our class and start listening.
            this.ws = ws;

            await WaitForMessage();
        }

        public async Task WaitForMessage()
        {
            while (ws.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buf), System.Threading.CancellationToken.None);
                if(result.MessageType == WebSocketMessageType.Binary)
                {
                    //Copy buffer and call the callback
                    byte[] msg = new byte[result.Count];
                    Array.Copy(buf, msg, result.Count);
                    OnBinaryMessage(msg);
                } else if (result.MessageType == WebSocketMessageType.Text)
                {
                    //Read this in as a string and let our code handle it.
                    string msg = Encoding.UTF8.GetString(buf, 0, result.Count);
                    Console.WriteLine("got " + msg);
                    //Let our code handle this.
                    OnMessage(msg);
                }
                
            }
            //Ended.
            OnClose();
        }

        //Voids that the code will call
        /// <summary>
        /// Send string content to the endpoint.
        /// </summary>
        /// <param name="content"></param>
        public void Send(string content)
        {
            Console.WriteLine("sending " + content);
            byte[] outBuf = Encoding.UTF8.GetBytes(content);
            ws.SendAsync(new ArraySegment<byte>(outBuf), WebSocketMessageType.Text, false, System.Threading.CancellationToken.None);
        }

        /// <summary>
        /// Send binary content.
        /// </summary>
        /// <param name="content"></param>
        public void Send(byte[] content)
        {
            ws.SendAsync(new ArraySegment<byte>(content), WebSocketMessageType.Binary, false, System.Threading.CancellationToken.None);
        }

        //Virtual voids
        /// <summary>
        /// Called on a new WebSocket message.
        /// </summary>
        public virtual void OnMessage(string content)
        {
            Console.WriteLine("Unhandled text data.");
        }

        /// <summary>
        /// Called on a new binary WebSocket message.
        /// </summary>
        /// <param name="content"></param>
        public virtual void OnBinaryMessage(byte[] content)
        {
            Console.WriteLine("Unhandled binary content."); 
        }

        /// <summary>
        /// Called on disconnect.
        /// </summary>
        public virtual void OnClose()
        {

        }
    }
}
