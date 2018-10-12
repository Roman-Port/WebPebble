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
            //Serve the form if this is a get request.
            if(e.Request.Method.ToLower() == "get")
            {
                //Serve form.
                Program.QuickWriteToDoc(e,TemplateManager.GetTemplate("Services/CreateProject/form_template.html", new string[] { }, new string[] { }));
            } else if(e.Request.Method.ToLower() == "post")
            {
                //Create the project.
                WebPebbleProject project = WebPebbleProject.CreateProject(e.Request.Form["title"], user.appDevName, user.uuid, false, e.Request.Form["sdk_version"]);
                //Redirect the user to this project.
                e.Response.Headers.Add("Location", "/project/" + project.projectId + "/manage/");
                Program.QuickWriteToDoc(e, "Redirecting....", "text/plain", 302);
            }
            
        }
    }
}
