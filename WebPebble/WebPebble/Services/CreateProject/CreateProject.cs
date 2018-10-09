using System;
using System.Collections.Generic;
using System.Text;
using WebPebble.Entities;
using WebPebble.Oauth;

namespace WebPebble.Services.CreateProject
{
    public static class CreateProject
    {
        public static void OnRequest(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject nonProject)
        {
            //Usually, we'd serve a form. For now, to save time, we'll just create it.
            WebPebbleProject project = WebPebbleProject.CreateProject("Testing " + LibRpws.LibRpwsCore.GenerateRandomString(4), user.appDevName, user.uuid, false);
            //Redirect the user to this project.
            e.Response.Headers.Add("Location", "/projects/" + project.projectId + "/manage/");
            Program.QuickWriteToDoc(e, "Redirecting....", "text/plain", 302);
        }
    }
}
