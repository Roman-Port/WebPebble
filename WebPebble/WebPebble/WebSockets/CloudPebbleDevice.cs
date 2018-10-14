﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace WebPebble.WebSockets
{
    public class CloudPebbleDevice : WebSocketBehavior
    {
        public bool authenticated = false;
        public string user_uuid = "";

        protected override void OnMessage(MessageEventArgs e)
        {
            MemoryStream ms = new MemoryStream(e.RawData);
            CloudPebbleCode code = GetCode(ms);
            //Reject all but signin if not authorized.
            if (code != CloudPebbleCode.AUTH_TOKEN && !authenticated)
                return;
            //Decide what to do based on this code.
            switch (code)
            {
                case CloudPebbleCode.AUTH_TOKEN:
                    //This will contain a login request.
                    DoAuth(ms);
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

        }

        public void SendData(byte[] data)
        {
            Send(data);
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
                SendData(new byte[] { 0x01 });
            //Accept and set up.
            user_uuid = user.uuid;
            authenticated = true;
            Console.WriteLine("User with UUID " + user_uuid + " connected.");
            //Add myself to the list of clients.
            if(WebPebble.WebSockets.WebSocketServer.connectedClients.ContainsKey(user_uuid))
            {
                //Disconnect old user.
                WebPebble.WebSockets.WebSocketServer.connectedClients.Remove(user_uuid);
            }
            //Add ourself.
            WebPebble.WebSockets.WebSocketServer.connectedClients.Add(user_uuid, this);
            //Accept.
            SendData(new byte[] { 0x00 });
        }

        /* External API */
        public void InstallApp(byte[] pbwData)
        {
            byte[] buf = new byte[pbwData.Length + 1];
            buf[0] = 0x04;
            pbwData.CopyTo(buf, 1);
            SendData(buf);
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
