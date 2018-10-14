using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Linq;

namespace WebPebble.WebSockets
{
    public partial class CloudPebbleDevice : WebSocketBehavior
    {
        public bool authenticated = false;
        public string user_uuid = "";
        public List<InterruptOnRequest> interrupts = new List<InterruptOnRequest>();

        public WebSocketPair pair;

        protected override void OnMessage(MessageEventArgs e)
        {
            MemoryStream ms = new MemoryStream(e.RawData);
            CloudPebbleCode code = GetCode(ms);
            //Reject all but signin if not authorized.
            if (code != CloudPebbleCode.AUTH_TOKEN && !authenticated)
                return;
            //Check if there is an interrupt for this.
            var inter_list = interrupts.FindAll(x => x.code == code);
            foreach(var inter in inter_list)
            {
                //Call the callback.
                AfterInterruptAction state = inter.callback(e.RawData, inter.d);
                if (state == AfterInterruptAction.PreventDefault_EndInterrupt || state == AfterInterruptAction.NoPreventDefault_EndInterrupt)
                {
                    //Remove this from the interrupts list.
                    interrupts.Remove(inter);
                }
                //Check if we should prevent default
                if (state == AfterInterruptAction.PreventDefault_ContinueInterrupt || state == AfterInterruptAction.PreventDefault_EndInterrupt)
                    return;
            }
            //Decide what to do based on this code.
            Console.WriteLine("Got message of type " + code.ToString() + " with length " + e.RawData.Length.ToString() + ".");
            switch (code)
            {
                case CloudPebbleCode.AUTH_TOKEN:
                    //This will contain a login request.
                    DoAuth(ms);
                    break;

                case CloudPebbleCode.PEBBLE_PROTOCOL_PHONE_TO_WATCH:
                    OnPPMMessage(new PebbleProtocolMessage(ms, code));
                    break;

                case CloudPebbleCode.PEBBLE_PROTOCOL_WATCH_TO_PHONE:
                    OnPPMMessage(new PebbleProtocolMessage(ms, code));
                    break;

                default:
                    Console.WriteLine("Got unknown CloudPebble proxy code " + code.ToString() + ".");
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
            }
            catch
            {

            }
            try
            {
                pair.web.SetStatus(false);
            }
            catch
            {

            }
            pair.connected = false;
        }

        public void SendData(byte[] data)
        {
            Send(data);
        }

        public void SendDataGetReplyType(byte[] data, CloudPebbleCode code, OnGetReply callback, object d)
        {
            //All this does is register a type with the reply from this for the first reply. It is not reliable.
            InterruptOnRequest interr = new InterruptOnRequest(code, callback, d);
            interrupts.Add(interr);
            //Send the data now.
            SendData(data);
        }

        /* Helpers */
        public CloudPebbleCode GetCode(MemoryStream ms)
        {
            //Read one byte and convert it to the code.
            return (CloudPebbleCode)GetByte(ms);
        }

        public byte GetByte(MemoryStream ms)
        {
            //Read one byte and convert it to the code.
            byte[] buf = new byte[1];
            ms.Read(buf, 0, 1);
            return buf[0];
        }

        public byte[] GetBytes(MemoryStream ms, int len)
        {
            //Read one byte and convert it to the code.
            byte[] buf = new byte[len];
            ms.Read(buf, 0, len);
            return buf;
        }

        /* Internal API */
        private void DoAuth(MemoryStream ms)
        {
            //Get the length of the token.
            int tokenLength = (int)GetByte(ms);
            //Get this many bytes and convert it into the string.
            string token = Encoding.ASCII.GetString(GetBytes(ms, tokenLength));
            //Use the RPWS Oauth to get data.
            var user = Oauth.RpwsAuth.AuthenticateUser(token);
            //Create a reply.
            if (user == null)
                SendData(new byte[] {0x09, 0x01 });
            //Accept and set up.
            user_uuid = user.uuid;
            authenticated = true;
            Console.WriteLine("User with UUID " + user_uuid + " connected.");
            //Accept.
            SendData(new byte[] { 0x09, 0x00 });
            //Add myself to the list of clients.
            if (WebPebble.WebSockets.WebSocketServer.connectedClients.ContainsKey(user_uuid))
            {
                //Replace old phone user, if any.
                WebPebble.WebSockets.WebSocketServer.connectedClients[user_uuid].phone = this;
                pair = WebPebble.WebSockets.WebSocketServer.connectedClients[user_uuid];
                //If the web is connected, tell it we have connected.
                
                try
                {
                    pair.web.SetStatus(true);
                    SetStatus(true);
                    pair.connected = true;
                }
                catch
                {

                }
            } else
            {
                //Add ourself.
                WebSocketPair pair = new WebSocketPair
                {
                    phone = this
                };
                WebPebble.WebSockets.WebSocketServer.connectedClients.Add(user_uuid, pair);
                pair = WebPebble.WebSockets.WebSocketServer.connectedClients[user_uuid];
            }
            //Now, WebPebble will deal with it.
        }

        private void OnPPMMessage(PebbleProtocolMessage ppm)
        {
            //Send this on to the web client.
            Dictionary<string, string> data = new Dictionary<string, string>();
            data["msg"] = ppm.stringData;
            data["direction"] = ((int)ppm.direction).ToString();
            data["type"] = ppm.id.ToString();
            data["binary"] = "";
            //Send it to the web client if they are connected.
            if(pair.connected)
            {
                try
                {
                    pair.web.QuickReply(-1, WebPebbleClient.WebPebbleRequestType.PebbleProtocolMsg, data);
                } catch
                {

                }
            }
        }

        /* External API */
        public void InstallApp(byte[] pbwData)
        {
            byte[] buf = new byte[pbwData.Length + 1];
            buf[0] = 0x04;
            pbwData.CopyTo(buf, 1);
            SendData(buf);
        }

        public void SetStatus(bool connected)
        {
            byte flag = 0xFF;
            if (!connected)
                flag = 0x00;
            SendData(new byte[] { 8, flag });
        }

        /* Classes */
        public class PebbleProtocolMessage
        {
            public PebbleProtocolDirection direction;
            public PebbleEndpointType id;
            public byte[] data;
            public string stringData;
            public DateTime time;

            public PebbleProtocolMessage(MemoryStream ms, CloudPebbleCode code)
            {
                //Set direction.
                direction = PebbleProtocolDirection.PhoneToWatch;
                if (code == CloudPebbleCode.PEBBLE_PROTOCOL_WATCH_TO_PHONE)
                    direction = PebbleProtocolDirection.WatchToPhone;
                //Get length
                short length = ReadShort(ms);
                //Get the ID
                id = (PebbleEndpointType)ReadShort(ms);
                Console.WriteLine("Got type " + id.ToString());
                //Read the data.
                data = new byte[length];
                ms.Read(data, 0, length);
                //Convert that to a string.
                stringData = Encoding.ASCII.GetString(data);
                //Set time
                time = DateTime.UtcNow;
            }

            private short ReadShort(MemoryStream ms)
            {
                byte[] buf = new byte[2];
                ms.Read(buf, 0, 2);
                Array.Reverse(buf);
                return BitConverter.ToInt16(buf, 0);
            }

            public enum PebbleProtocolDirection
            {
                WatchToPhone,
                PhoneToWatch
            }
        }

        public delegate AfterInterruptAction OnGetReply(byte[] data, object d);

        public class InterruptOnRequest
        {
            public CloudPebbleCode code;
            public OnGetReply callback;
            public object d;

            public InterruptOnRequest(CloudPebbleCode _code, OnGetReply _callback, object _d)
            {
                code = _code;
                callback = _callback;
                d = _d;
            }
        }

        public enum AfterInterruptAction
        {
            PreventDefault_EndInterrupt, //Prevent the default and remove the interrupt
            PreventDefault_ContinueInterrupt, //Prevent the default and make the interrupt run the next time this comes in.
            NoPreventDefault_EndInterrupt, //Do not prevent the default and remove the interrupt
            NoPreventDefault_ContinueInterrupt, //Do not prevent the default and make the interrupt run the next time this comes in.
        }
    }

    public enum CloudPebbleCode
    {
        PEBBLE_PROTOCOL_WATCH_TO_PHONE = 0,
        PEBBLE_PROTOCOL_PHONE_TO_WATCH = 1,
        PHONE_APP_LOG = 2,
        PHONE_SERVER_LOG = 3,
        APP_INSTALL = 4,
        STATUS_CODE = 5,
        PHONE_INFO = 6,
        CONNECTION_STATUS = 7,
        PROXY_CONNECTION_STATUS = 8,
        AUTH_TOKEN = 9,
        TIMELINE_PIN_ACTION = 12
    }
}
