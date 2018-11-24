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
        public static Dictionary<string, WebSocketPair> connectedClients = new Dictionary<string, WebSocketPair>();

        public static WebSocketSharp.Server.WebSocketServer wssv;

        public static void StartServer()
        {
            Console.WriteLine("Starting WebSocket server...");
            wssv = new WebSocketSharp.Server.WebSocketServer(IPAddress.Any, 43187, false);
            wssv.ReuseAddress = true;

            //SSL
            wssv.SslConfiguration.ServerCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(Program.config.ssl_cert);
            wssv.SslConfiguration.ClientCertificateRequired = true;
            wssv.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            //End SSL

            wssv.AddWebSocketService<CloudPebbleDevice>("/device");
            wssv.AddWebSocketService<WebPebbleClient>("/webpebble");
            wssv.Start();
            Console.WriteLine("Started WebSocket server.");
        }
    }

    public class WebSocketPair
    {
        public CloudPebbleDevice phone;
        public WebPebbleClient web;

        public bool connected = false;
    }
}
