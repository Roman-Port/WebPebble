using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebPebble.Oauth;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace WebPebble.WebSockets
{
    public static class WebSocketServer
    {
        public static Dictionary<string, WebSocketPair> connectedClients = new Dictionary<string, WebSocketPair>();

        public static string url;

        public static void StartServer()
        {
            Console.WriteLine("Starting WebSocket server...");
            //Generate the URL.
            int port = LibRpws.LibRpwsCore.rand.Next(43100, 43300);
            url = "ws://cloudpebble-developer-proxy.get-rpws.com:"+port.ToString()+"/";
            var wssv = new WebSocketSharp.Server.WebSocketServer(IPAddress.Any, port, false);
            wssv.AddWebSocketService<CloudPebbleDevice>("/device");
            wssv.AddWebSocketService<WebPebbleClient>("/webpebble");
            wssv.Start();
            Console.WriteLine("Started WebSocket server.");
        }

        public static void HandleHttpSetup(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebble.Entities.WebPebbleProject project)
        {

        }
    }

    public class WebSocketPair
    {
        public CloudPebbleDevice phone;
        public WebPebbleClient web;

        public bool connected = false;
    }
}
