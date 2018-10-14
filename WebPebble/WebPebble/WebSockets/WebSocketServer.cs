using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebSockets;

namespace WebPebble.WebSockets
{
    public static class WebSocketServer
    {
        private static HttpListener listener;

        public static Dictionary<string, CloudPebblePhoneConnection> connectedClients = new Dictionary<string, CloudPebblePhoneConnection>();

        public static void StartServer()
        {
            Console.WriteLine("Starting WebSocket server...");
            listener = new HttpListener();
            listener.Prefixes.Add("http://cloudpebble-developer-proxy.get-rpws.com:43187/");
            listener.Prefixes.Add("ws://cloudpebble-developer-proxy.get-rpws.com:43187/");
            listener.Start();
            
            listener.BeginGetContext(OnGetClient, null);
            Console.WriteLine("Started WebSocket server.");
        }

        private static void OnGetClient(IAsyncResult ar)
        {
            //Get the context offered.
            HttpListenerContext context = listener.EndGetContext(ar);
            //Begin listening again.
            listener.BeginGetContext(OnGetClient, null);
            Console.WriteLine("Got WebPebble Phone request from " + context.Request.RemoteEndPoint.ToString() + ".");
            //Check if this is a WebSocket request.
            if (!context.Request.IsWebSocketRequest)
            {
                //Close this
                byte[] ex_buf = Encoding.UTF8.GetBytes("this is a websocket endpoint for webpebble. killed.");
                context.Response.OutputStream.Write(ex_buf, 0, ex_buf.Length);
                context.Response.Close();
                return;
            }
            //Get the WebSocket object
            var wsContext = context.AcceptWebSocketAsync(null).GetAwaiter().GetResult();
            var webSocket = wsContext.WebSocket;
            CloudPebblePhoneConnection conn = new CloudPebblePhoneConnection(webSocket);
            //Do auth
            var user = conn.DoAuth().GetAwaiter().GetResult();
            //If this login was okay, add it to the dictonary for use when the API uses it.
            if(user != null)
            {
                if(connectedClients.ContainsKey(user.uuid))
                {
                    //Disconnect this user.
                    connectedClients[user.uuid].Disconnect("Another client connected in your name.").GetAwaiter().GetResult();
                    connectedClients.Remove(user.uuid);
                }
                conn.user_uuid = user.uuid;
                Console.WriteLine("(Debug) Installing app...");
                conn.InstallApp(File.ReadAllBytes("/home/roman/app.pbw")).GetAwaiter().GetResult();
                Console.WriteLine("(Debug) Finished installing app.");
                connectedClients.Add(conn.user_uuid, conn);
            }
        }
    }
}
