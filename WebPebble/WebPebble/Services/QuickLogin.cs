using System;
using System.Collections.Generic;
using System.Text;
using WebPebble.Entities;
using WebPebble.Oauth;

namespace WebPebble.Services
{
    public class QuickLogin
    {
        public static void Serve(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            e.Response.Headers.Add("Set-Cookie", "access-token="+e.Request.Query["token"]+"; Path=/");
            Program.QuickWriteToDoc(e, "ok");
        }
    }
}
