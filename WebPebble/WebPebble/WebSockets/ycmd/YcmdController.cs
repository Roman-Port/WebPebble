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
        public const string YCMD_HOSTNAME = "localhost.localdomain";
        public const int YCMD_PORT = 43585;

        public const string YCMD_SECRET = "EL739LziGnBRxH3j";

        public static string GenerateUri(string pathname)
        {
            return "http://" + YCMD_HOSTNAME + ":" + YCMD_PORT.ToString() + pathname;
        }

        public static T SendYcmdRequest<T>(string path, object requestData )
        {
            var request = (HttpWebRequest)WebRequest.Create(GenerateUri(path));
            string requestJson = JsonConvert.SerializeObject(requestData);
            requestJson = "test";
            var data = Encoding.UTF8.GetBytes(requestJson);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = data.Length;

            //Add x-ycm-hmac header.
            string hmac = GenerateHmac("POST", path.TrimStart('/'), requestJson);
            request.Headers.Add("x-ycm-hmac", hmac);
            Console.WriteLine(hmac);

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var r = (HttpWebResponse)request.GetResponse();
            var jsonString = new StreamReader(r.GetResponseStream()).ReadToEnd();

            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        public static string GenerateHmac(string method, string path, string body)
        {

            Console.WriteLine("DATA:");
            Console.WriteLine(method);
            Console.WriteLine(path);
            Console.WriteLine(body);
            Console.WriteLine("");
            
            //Convert all to text
            byte[] b_method = Encoding.UTF8.GetBytes(method);
            byte[] b_path = Encoding.UTF8.GetBytes(path);
            byte[] b_body = Encoding.UTF8.GetBytes(body);

            byte[] joined;

            using(MemoryStream ms = new MemoryStream())
            {
                WriteHmacToMs(b_method, ms);
                WriteHmacToMs(b_path, ms);
                WriteHmacToMs(b_body, ms);

                ms.Position = 0;
                joined = new byte[ms.Length];
                ms.Read(joined, 0, joined.Length);
            }

            return Convert.ToBase64String(CalculateHmac(joined));
        }

        private static void WriteHmacToMs(byte[] data, Stream s)
        {
            byte[] buf = CalculateHmac(data);
            s.Write(buf, 0, buf.Length);
        }

        private static byte[] CalculateHmac(byte[] data)
        {
            HMAC h = new HMACSHA256(Encoding.ASCII.GetBytes(YCMD_SECRET));
            return h.ComputeHash(data);
        }
    }
}
