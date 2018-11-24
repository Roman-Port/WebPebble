using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WebPebble.Entities;
using WebPebble.Oauth;

namespace WebPebble.Services.CreateProject
{
    public static class CreateProject
    {
        public static async Task OnRequest(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject nonProject)
        {
            //Serve the form if this is a get request.
            if(e.Request.Method.ToLower() == "get")
            {
                //Serve form.
                await Program.QuickWriteToDoc(e,TemplateManager.GetTemplate("Services/CreateProject/form_template.html", new string[] { }, new string[] { }));
            } else if(e.Request.Method.ToLower() == "post")
            {
                //Create the project.
                WebPebbleProject project = WebPebbleProject.CreateProject(e.Request.Form["title"], user.appDevName, user.uuid, false, e.Request.Form["sdk_version"]);
                //Redirect the user to this project.
                e.Response.Headers.Add("Location", "/project/" + project.projectId + "/manage/");
                await Program.QuickWriteToDoc(e, "Redirecting....", "text/plain", 302);
            }
            
        }
    }
}
