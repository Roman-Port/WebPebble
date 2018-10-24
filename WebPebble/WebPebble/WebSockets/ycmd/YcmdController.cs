using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace WebPebble.WebSockets.ycmd
{
    public static class YcmdController
    {
        public const string YCMD_HOSTNAME = "localhost";

        public static string GenerateUri(string pathname, YcmdProcess p)
        {
            return "http://" + YCMD_HOSTNAME + ":" + p.port.ToString() + pathname;
        }

        public static T SendYcmdRequest<T>(string path, object requestData, YcmdProcess p )
        {
            string jsonString = SendYcmdRequestRaw(path, requestData, p);
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        public static string SendYcmdRequestRaw(string path, object requestData, YcmdProcess p)
        {
            var request = (HttpWebRequest)WebRequest.Create(GenerateUri(path, p));
            string requestJson = JsonConvert.SerializeObject(requestData);
            var data = Encoding.UTF8.GetBytes(requestJson);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = data.Length;

            //Add x-ycm-hmac header.
            string hmac = GenerateHmac("POST", path, requestJson, p);
            request.Headers.Add("X-Ycm-Hmac", hmac);
            Console.WriteLine(hmac);

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var r = (HttpWebResponse)request.GetResponse();
            var jsonString = new StreamReader(r.GetResponseStream()).ReadToEnd();

            return (jsonString);
        }

        public static string GenerateHmac(string method, string path, string body, YcmdProcess p)
        {            
            //Convert all to text
            byte[] b_method = Encoding.UTF8.GetBytes(method);
            byte[] b_path = Encoding.UTF8.GetBytes(path);
            byte[] b_body = Encoding.UTF8.GetBytes(body);

            byte[] joined;

            using(MemoryStream ms = new MemoryStream())
            {
                WriteHmacToMs(b_method, ms,p);
                WriteHmacToMs(b_path, ms,p);
                WriteHmacToMs(b_body, ms,p);

                ms.Position = 0;
                joined = new byte[ms.Length];
                ms.Read(joined, 0, joined.Length);
            }

            return Convert.ToBase64String(CalculateHmac(joined,p));
        }

        private static void WriteHmacToMs(byte[] data, Stream s, YcmdProcess p)
        {
            byte[] buf = CalculateHmac(data, p);
            s.Write(buf, 0, buf.Length);
        }

        private static byte[] CalculateHmac(byte[] data, YcmdProcess p)
        {
            HMAC h = new HMACSHA256(p.secret_key);
            return h.ComputeHash(data);
        }
    }
}
