using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace WebPebble.Services
{
    public static class LoginService
    {
        public static void BeginLogin(Microsoft.AspNetCore.Http.HttpContext e, Oauth.E_RPWS_User user, Entities.WebPebbleProject non_proj)
        {
            //Redirect the user to the RPWS oauth screen.
            string returnUri = "https://api.webpebble.get-rpws.com/complete_login";
            if (e.Request.Query.ContainsKey("return"))
                returnUri += $"?return={System.Web.HttpUtility.UrlEncode(e.Request.Query["return"])}";
            else
                returnUri += $"?return={System.Web.HttpUtility.UrlEncode("https://webpebble.get-rpws.com/")}";

            string redir = $"https://blue.api.get-rpws.com/v1/oauth2/?returnuri={System.Web.HttpUtility.UrlEncode(returnUri)}";
            e.Response.Headers.Add("Location", redir);
            Program.QuickWriteToDoc(e, $"You should've been redirected to {redir}.", "text/html", 302);
        }

        public static void FinishLogin(Microsoft.AspNetCore.Http.HttpContext e, Oauth.E_RPWS_User user, Entities.WebPebbleProject non_proj)
        {
            //Called when the user finishes the RPWS oauth. Get the token from the endpoint.
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(e.Request.Query["endpoint"]);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            string payload;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                payload = reader.ReadToEnd();
            }

            //Decode payload
            RpwsPayload p = JsonConvert.DeserializeObject<RpwsPayload>(payload);

            if (p.ok == false)
                throw new Exception($"Failed to authenticate with RPWS; {p.message}");

            //Get the token and set it as a cookie. We'll worry about csrf before release.
            string token = p.access_token;

            e.Response.Cookies.Append("access-token", token, new Microsoft.AspNetCore.Http.CookieOptions
            {
                Expires = new DateTimeOffset(DateTime.UtcNow.AddYears(8), TimeSpan.Zero),
                IsEssential = true,
                Path = "/"
            });

            //Redirect to the redirect path.
            string redir = e.Request.Query["return"];
            e.Response.Headers.Add("Location", redir);
            Program.QuickWriteToDoc(e, $"You should've been redirected to {redir}.", "text/html", 302);
        }

        class RpwsPayload
        {
            public bool ok;
            public string message;
            public string access_token;
        }
    }
}
