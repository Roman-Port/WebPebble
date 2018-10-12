using System;
using System.Collections.Generic;
using System.Text;
using WebPebble.Entities;
using WebPebble.Oauth;

namespace WebPebble.Services.Landing
{
    class Landing
    {
        public static void OnRequest(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject nonProject)
        {
            //For now, serve the signed out page.
            Program.QuickWriteToDoc(e, TemplateManager.GetTemplate("Services/Landing/landing_signedout.html", new string[] { }, new string[] { }));
        }
    }
}
