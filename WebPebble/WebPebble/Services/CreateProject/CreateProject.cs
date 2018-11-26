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
            string devName = user.appDevName;
            if (!user.isAppDev)
                devName = user.name.Split(' ')[0];

            string name = e.Request.Query["title"];
            if (name.Length > 24 || name.Length < 4)
            {
                await Program.QuickWriteJsonToDoc(e, new Reply
                {
                    data = $"Make sure your project name is between 4 - 24 characters. (It is {name.Length})",
                    ok = false
                });
                return;
            }

            if (Program.database.GetCollection<WebPebbleProject>("projects").FindOne( x => x.authorId == user.uuid && x.name == name) != null)
            {
                await Program.QuickWriteJsonToDoc(e, new Reply
                {
                    data = $"You already own a project with that name!",
                    ok = false
                });
                return;
            }

            WebPebbleProject project = WebPebbleProject.CreateProject(name, devName, user.uuid, e.Request.Query["watchface"]=="true", "3");
            //Return the new location
            await Program.QuickWriteJsonToDoc(e, new Reply
            {
                data = $"/project/{project.projectId}/manage/",
                ok = true
            });
        }

        class Reply
        {
            public bool ok;
            public string data;
        }
    }
}
