/*using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebPebble.WebSockets
{
    public class CloudPebblePhoneConnection
    {
        public readonly WebSocket ws;
        public string user_uuid = "not authorized";

        public CloudPebblePhoneConnection(WebSocket _ws)
        {
            ws = _ws;
        }

        public async Task<byte[]> WaitForData(int timeout = 2000, int bufferSize = 512)
        {
            //Ugly. Wait for data and lock thread.
            byte[] buf = new byte[bufferSize];
            var task = ws.ReceiveAsync(new ArraySegment<byte>(buf), CancellationToken.None);
            if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
            {
                return buf;
            }
            else
            {
                //Timed out.
                throw new Exception("Timeout");
            }
        }

        public async Task<MemoryStream> WaitForStream(int timeout = 2000, int bufferSize = 512)
        {
            //Just wait for data and then open a MemoryStream on the data.
            byte[] buf = await WaitForData(timeout, bufferSize);
            return new MemoryStream(buf);
        }

        public async Task SendData(byte[] data, WebSocketMessageType type = WebSocketMessageType.Binary)
        {
            await ws.SendAsync(new ArraySegment<byte>(data, 0, data.Length), type, true, CancellationToken.None);
        }

        public async Task<byte[]> SendDataAndWaitForReply(byte[] data, WebSocketMessageType type = WebSocketMessageType.Binary, int timeout = 2000, int bufferSize = 512)
        {
            await SendData(data, type);
            return await WaitForData(timeout, bufferSize);
        }

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

        public async Task<Oauth.E_RPWS_User> DoAuth()
        {
            //Await the data from the client.
            MemoryStream ms = await WaitForStream();
            //Get the code to verify this is a auth request.
            CloudPebbleCode code = GetCode(ms);
            if(code == CloudPebbleCode.AUTH_TOKEN)
            {
                //Get the length of the token.
                int tokenLength = (int)GetByte(ms);
                //Get this many bytes and convert it into the string.
                string token = Encoding.ASCII.GetString(GetBytes(ms, tokenLength));
                //Use the RPWS Oauth to get data.
                var user = Oauth.RpwsAuth.AuthenticateUser(token);
                //Create a reply.
                if (user == null)
                    await SendData(new byte[] { 0x01 }, WebSocketMessageType.Binary);
                //Accept and return.
                await SendData(new byte[] { 0x00 }, WebSocketMessageType.Binary);
                return user;
            } else
            {
                //Hmmm....weird
                Console.WriteLine("Rejecting CloudPebble connection because got " + code.ToString() + " when CloudPebbleCode.AUTH_TOKEN was expected.");
                await SendData(new byte[] { 0x01 }, WebSocketMessageType.Binary);
                return null;
            }
        }

        public async Task<bool> InstallApp(byte[] pbwData)
        {
            byte[] buf = new byte[pbwData.Length + 1];
            buf[0] = 0x04;
            pbwData.CopyTo(buf, 1);
            await SendData(buf, WebSocketMessageType.Binary);
            //Give it time to install.
            byte[] returnedData = await WaitForData(20000, 5);
            return returnedData[4] == 0x00;
        }

        public async Task Disconnect(string reason)
        {
            Console.WriteLine("Closing connection to " + user_uuid);
            try
            {
                await ws.CloseAsync(WebSocketCloseStatus.Empty, reason, CancellationToken.None);
            } catch
            {

            }
            ws.Dispose();
        }
    }

    
}*/
