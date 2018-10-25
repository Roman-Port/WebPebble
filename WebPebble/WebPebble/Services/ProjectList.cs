using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace WebPebble.Services
{
    class ProjectList
    {
        public static void Serve(Microsoft.AspNetCore.Http.HttpContext e, Oauth.E_RPWS_User user, Entities.WebPebbleProject non_proj)
        {
            //Get the projects list from this user.
            var collect = Program.database.GetCollection<Entities.WebPebbleProject>("projects");
            var projects = collect.Find(x => x.authorId == user.uuid).ToArray();
            //Convert this to a smaller format.
            ProjectsListFormat[] output = new ProjectsListFormat[projects.Length];
            //Convert
            for(int i = 0; i<output.Length; i++)
            {
                ProjectsListFormat f = new ProjectsListFormat();
                var proj = projects[i];
                f.href = "/projects/" + proj.projectId + "/manage/";
                f.icon = "";
                f.id = proj.projectId;
                f.name = proj.name;
                output[i] = f;
            }
            //Output
            Program.QuickWriteJsonToDoc(e, output);
        }

        class ProjectsListFormat
        {
            public string id;
            public string name;
            public string icon;
            public string href;
        }
    }
}
