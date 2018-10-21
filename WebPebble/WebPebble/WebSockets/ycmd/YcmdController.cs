using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace WebPebble.WebSockets.ycmd
{
    public static class YcmdController
    {
        public const string YCMD_HOSTNAME = "localhost.localdomain";
        public const int YCMD_PORT = 43585;

        public static string GenerateUri(string pathname)
        {
            return "http://" + YCMD_HOSTNAME + ":" + YCMD_PORT.ToString() + pathname;
        }

        public static T SendYcmdRequest<T>(string path, object requestData )
        {
            var request = (HttpWebRequest)WebRequest.Create(GenerateUri(path));
            var data = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(requestData));
            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = data.Length;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var r = (HttpWebResponse)request.GetResponse();
            var jsonString = new StreamReader(r.GetResponseStream()).ReadToEnd();

            return JsonConvert.DeserializeObject<T>(jsonString);
        }
    }
}
