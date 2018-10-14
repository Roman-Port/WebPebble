using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WebPebble.Oauth;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace WebPebble.WebSockets
{
    public class WebPebbleClient : WebSocketBehavior
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

        public void QuickReply(int id, WebPebbleRequestType type, Dictionary<string,string> dict)
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
                QuickReply(req.requestid, WebPebbleRequestType.Auth, new Dictionary<string, string>() { { "error", "Phone not connected." } });
                return false;
            }
            return true;
        }

        /* Api */
        private void DoAuth(WebPebbleRequest req)
        {
            //Get the token that was offered.
            string token = req.data["token"];
            //Check this against the RPWS server.
            E_RPWS_User user = Oauth.RpwsAuth.AuthenticateUser(token);
            if(user == null)
            {
                //Respond with failure.
                QuickReply(req.requestid, WebPebbleRequestType.Auth, new Dictionary<string, string>() { { "ok","false"} });
            } else
            {
                user_uuid = user.uuid;
                authenticated = true;
                //Respond with ok.
                QuickReply(req.requestid, WebPebbleRequestType.Auth, new Dictionary<string, string>() { { "ok", "true" } });
                //Add myself to the list of clients.
                if (WebPebble.WebSockets.WebSocketServer.connectedClients.ContainsKey(user_uuid))
                {
                    //Replace old WebPebble user, if any.
                    WebPebble.WebSockets.WebSocketServer.connectedClients[user_uuid].web = this;
                    pair = WebPebble.WebSockets.WebSocketServer.connectedClients[user_uuid];
                    //If the phone is connected, tell it we have connected.
                    SetStatus(true);
                    try
                    {
                        pair.phone.SetStatus(true);
                        
                        pair.connected = true;
                    } catch
                    {
                        //Remain in "disconnected" state.
                    }
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
            QuickReply(-1, WebPebbleRequestType.ConnectionStatus, new Dictionary<string, string>() { { "connected", connected.ToString().ToLower() } });
        }

        private void DoGetScreenshot(WebPebbleRequest req)
        {
            //If we're not connected, tell them so.
            if (!CheckIfConnected(req))
                return;
            //Ask the client for a screenshot.
            pair.phone.GetScreenshot((byte[] data) =>
           {
               //Create the reply and encode the image data as base64.
               string output = "data:image/png;base64," + Convert.ToBase64String(data);
               QuickReply(req.requestid, WebPebbleRequestType.Screenshot, new Dictionary<string, string>() { { "data", output } });
           });
        }

        public class WebPebbleRequest
        {
            public int requestid; //This will be echoed back to the client. -1 if this is a event and not a reply.
            public WebPebbleRequestType type;

            public Dictionary<string, string> data = new Dictionary<string, string>(); //Optional data.
        }

        public enum WebPebbleRequestType
        {
            Reply = 0,
            Auth = 1,
            Screenshot = 2,
            ConnectionStatus = 3
        }
    }
}
