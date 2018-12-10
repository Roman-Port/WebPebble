using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace WebPebble.Services.Me
{
    public static class MeService
    {
        public static async Task PollUserData(Microsoft.AspNetCore.Http.HttpContext e, Oauth.E_RPWS_User user, Entities.WebPebbleProject non_proj)
        {
            //Write out user settings and projects
            OutputData output = new OutputData();

            //Get the projects list from this user.
            var collect = Program.database.GetCollection<Entities.WebPebbleProject>("projects");
            var projects = collect.Find(x => x.authorId == user.uuid).ToArray();
            //Convert this to a smaller format.
            output.projects = new ProjectsListFormat[projects.Length];
            //Convert
            for (int i = 0; i < output.projects.Length; i++)
            {
                ProjectsListFormat f = new ProjectsListFormat();
                var proj = projects[i];
                f.href = "https://webpebble.get-rpws.com/project/" + proj.projectId + "/manage/";
                f.icon = "";
                f.id = proj.projectId;
                f.name = proj.name;
                output.projects[i] = f;
            }

            //Now, copy settings.
            output.settings = new OutputDataSettings
            {
                theme = user.webpebble_data.theme
            };

            //Write this
            await Program.QuickWriteJsonToDoc(e, output);
        }

        class OutputData
        {
            public ProjectsListFormat[] projects;
            public OutputDataSettings settings;
        }

        class ProjectsListFormat
        {
            public string id;
            public string name;
            public string icon;
            public string href;
        }

        class OutputDataSettings
        {
            public string theme;
        }


    }
}
