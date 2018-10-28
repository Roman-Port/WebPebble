using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using WebPebble.Oauth;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace WebPebble.WebSockets
{
    public partial class WebPebbleClient : WebSocketBehavior
    {
        //This is the connection from WebPebble. It pretty much just calls 
        public bool authenticated = false;
        public string user_uuid = "";

        public WebSocketPair pair;

        protected override void OnMessage(MessageEventArgs e)
        {
            //Read the JSON.
            WebPebbleRequest request = JsonConvert.DeserializeObject<WebPebbleRequest>(e.Data);
            //If this isn't an auth request, ignore.
            if (!authenticated && request.type != WebPebbleRequestType.Auth)
                return;
            //Do what is needed based on the request.
            switch(request.type)
            {
                case WebPebbleRequestType.Auth:
                    DoAuth(request);
                    break;
                case WebPebbleRequestType.Screenshot:
                    DoGetScreenshot(request);
                    break;
                case WebPebbleRequestType.InstallApp:
                    InstallApp(request);
                    break;
                case WebPebbleRequestType.YcmdComplete:
                    OnYcmdRequest(request);
                    break;
            }
        }

        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            base.OnError(e);
        }

        protected override void OnOpen()
        {
            base.OnOpen();
        }

        protected override void OnClose(CloseEventArgs e)
        {
            //Send disconnect signal to each client.
            try
            {
                pair.phone.SetStatus(false);
            } catch
            {

            }
            try
            {
                pair.web.SetStatus(false);
            } catch
            {
                
            }
            pair.connected = false;
        }

        public void SendData(string data)
        {
            Send(data);
        }

        public void QuickReply(int id, WebPebbleRequestType type, Dictionary<string,object> dict)
        {
            WebPebbleRequest req = new WebPebbleRequest
            {
                requestid = id,
                type = type,
                data = dict
            };
            SendData(JsonConvert.SerializeObject(req));
        }

        public bool CheckIfConnected(WebPebbleRequest req)
        {
            if(!pair.connected)
            {
                //Tell client we are disconnected.
                QuickReply(req.requestid, WebPebbleRequestType.Auth, new Dictionary<string, object>() { { "error", "Phone not connected." } });
                return false;
            }
            return true;
        }

        /* Api */
        private void DoAuth(WebPebbleRequest req)
        {
            //Get the token that was offered.
            string token = (string)req.data["token"];
            //Check this against the RPWS server.
            E_RPWS_User user = Oauth.RpwsAuth.AuthenticateUser(token);
            if(user == null)
            {
                //Respond with failure.
                QuickReply(req.requestid, WebPebbleRequestType.Auth, new Dictionary<string, object>() { { "ok","false"} });
            } else
            {
                user_uuid = user.uuid;
                authenticated = true;
                //Respond with ok.
                QuickReply(req.requestid, WebPebbleRequestType.Auth, new Dictionary<string, object>() { { "ok", "true" } });
                //Add myself to the list of clients.
                if (WebPebble.WebSockets.WebSocketServer.connectedClients.ContainsKey(user_uuid))
                {
                    pair = WebPebble.WebSockets.WebSocketServer.connectedClients[user_uuid];
                    //If the phone is connected, tell it we have connected.
                    try
                    {
                        pair.phone.SetStatus(true);
                        
                        pair.connected = true;
                    } catch
                    {
                        //Remain in "disconnected" state.
                        pair.connected = false;
                    }
                    //If another WebPebble session is connected, boot them.
                    try
                    {
                        pair.web.QuickReply(-1, WebPebbleRequestType.CloseOldClient, new Dictionary<string, object>() {  });
                    } catch
                    {

                    }
                    //Replace old WebPebble user, if any.
                    WebPebble.WebSockets.WebSocketServer.connectedClients[user_uuid].web = this;
                    //Set our connection status.
                    SetStatus(pair.connected);
                }
                else
                {
                    //Add ourself.
                    WebSocketPair new_pair = new WebSocketPair
                    {
                        web = this
                    };
                    WebPebble.WebSockets.WebSocketServer.connectedClients.Add(user_uuid, new_pair);
                    this.pair = WebPebble.WebSockets.WebSocketServer.connectedClients[user_uuid];
                }
            }
        }

        public void SetStatus(bool connected)
        {
            QuickReply(-1, WebPebbleRequestType.ConnectionStatus, new Dictionary<string, object>() { { "connected", connected.ToString().ToLower() } });
        }

        private int pendingScreenshotRequestId = -2;
        private void DoGetScreenshot(WebPebbleRequest req)
        {
            pendingScreenshotRequestId = req.requestid;
            //If we're not connected, tell them so.
            if (!CheckIfConnected(req))
                return;
            //Ask the client for a screenshot.
            pair.phone.GetScreenshot((byte[] data) =>
           {
               //Create the reply and encode the image data as base64.
               string output = Convert.ToBase64String(data);
               QuickReply(pendingScreenshotRequestId, WebPebbleRequestType.Screenshot, new Dictionary<string, object>() { { "data", output },{"img_header", "data:image/png;base64," }, {"download_header", "data:application/octet-stream;base64," } });
           });
        }

        public void InstallApp(WebPebbleRequest req)
        {
            //If we're not connected, tell them so.
            if (!CheckIfConnected(req))
                return;
            //Download the PBW file prompted.
            using (var client = new WebClient())
            {
                byte[] pbw = client.DownloadData((string)req.data["url"]);
                pair.phone.InstallApp(pbw);
            }
        }

        public class WebPebbleRequest
        {
            public int requestid; //This will be echoed back to the client. -1 if this is a event and not a reply.
            public WebPebbleRequestType type;

            public Dictionary<string, object> data = new Dictionary<string, object>(); //Optional data.
        }

        public enum WebPebbleRequestType
        {
            Reply = 0,
            Auth = 1,
            Screenshot = 2,
            ConnectionStatus = 3,
            PebbleProtocolMsg = 4,
            InstallApp = 5,
            YcmdComplete = 6,
            CloseOldClient = 7 //Sent to a WebPebble session when another WebSession client connects.
        }
    }
}
