using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;

namespace LibRpws
{
    public static class LibRpwsCore
    {
        public static Random rand = new Random();

        public static T GetObjectHttp<T>(string endpoint, string token = null)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
                request.Headers.Clear();
                request.UserAgent = "RPWS";
                request.Headers.Add("Accept", "*/*");
                if (token != null)
                    request.Headers.Add("Authorization", "Bearer " + token);

                string reply;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    reply = reader.ReadToEnd();
                }
                //Deserialize
                return JsonConvert.DeserializeObject<T>(reply);
            }
            catch (WebException wex)
            {
                string reply;
                using (Stream s = wex.Response.GetResponseStream())
                using (StreamReader reader = new StreamReader(s))
                {
                    reply = reader.ReadToEnd();
                }
                throw new Exception("Remote server returned an error.");
            }
        }

        

        public static string GenerateRandomString(int length)
        {
            string output = "";
            char[] chars = "qwertyuiopasdfghjklzxcvbnm1234567890QWERTYUIOPASDFGHJKLZXCVBNM".ToCharArray();
            for (int i = 0; i < length; i++)
            {
                output += chars[rand.Next(0, chars.Length)];
            }
            return output;
        }

        public static string GenerateRandomHexString(int length, Random r = null)
        {
            string output = "";
            if (r == null)
                r = rand;
            char[] chars = "1234567890abcde".ToCharArray();
            for (int i = 0; i < length; i++)
            {
                output += chars[r.Next(0, chars.Length)];
            }
            return output;
        }

        public static string GenerateStringInFormat(string format)
        {
            //&: Hex
            //*: Random
            char[] output = format.ToCharArray();
            for (int i = 0; i < output.Length; i++)
            {
                char ii = output[i];
                if (ii == '&')
                    output[i] = GenerateRandomHexString(1)[0];
                if (ii == '*')
                    output[i] = GenerateRandomString(1)[0];
            }
            return new string(output);
        }
        
    }

    public enum RpwsLogLevel
    {
        Status,
        Standard,
        High,
        Critical,
        Analytics
    }
}
