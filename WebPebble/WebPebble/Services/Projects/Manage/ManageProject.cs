using System;
using System.Collections.Generic;
using System.Text;
using WebPebble.Entities;
using WebPebble.Oauth;

namespace WebPebble.Services.Projects.Manage
{
    public static class ManageProject
    {
        public static void HandleRequest(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject project)
        {
            string d = TemplateManager.GetTemplate("Services/Projects/Manage/main_template.html", new string[] { }, new string[] { });
            Program.QuickWriteToDoc(e, d);
        }
    }
}
