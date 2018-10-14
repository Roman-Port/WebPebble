using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace WebPebble.WebSockets
{
    public static class WebSocketServer
    {
        public static Dictionary<string, CloudPebbleDevice> connectedClients = new Dictionary<string, CloudPebbleDevice>();

        public static void StartServer()
        {
            Console.WriteLine("Starting WebSocket server...");
            var wssv = new WebSocketSharp.Server.WebSocketServer(IPAddress.Any, 43187, false);
            wssv.AddWebSocketService<CloudPebbleDevice>("/device");
            wssv.Start();
            Console.WriteLine("Started WebSocket server.");
        }
    }
}
