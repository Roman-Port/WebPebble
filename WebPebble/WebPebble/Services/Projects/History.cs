using System;
using System.Collections.Generic;
using System.Text;
using WebPebble.Entities;
using WebPebble.Oauth;
using System.Linq;

namespace WebPebble.Services.Projects
{
    class History
    {
        public static void OnRequest(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            //Check if we're listing all, or one build.
            string[] split = e.Request.Path.ToString().Split('/');
            bool showList = split.Length >= 5;
            if (showList)
                showList = split[4].Length == 0;
            if(showList)
            {
                //Print the build history. Remove the log data because it is expensive to bandwidth, and convert the time to a readable format.
                OutputBuild[] builds = new OutputBuild[proj.builds.Count];
                //Go through each.
                for (int i = 0; i < proj.builds.Count; i++)
                {
                    var b = proj.builds[i];
                    DateTime time = new DateTime(b.time);
                    OutputBuild ob = new OutputBuild
                    {
                        id = b.id,
                        passed = b.passed
                    };
                    ob.api_log = "build_history/" + ob.id + "/";
                    ob.api_pbw = "pbw_media/" + ob.id + "/";
                    ob.time = time.ToShortDateString() + " at " + time.ToLongTimeString();
                    builds[i] = ob;
                }
                Program.QuickWriteJsonToDoc(e, builds);
            } else
            {
                //Show just one item. Check if it exists.
                string id = split[4];
                var build = proj.builds.Find(x => x.id == id);
                if(build == null)
                {
                    //Not found.
                    Program.QuickWriteToDoc(e, "Not Found", "text/plain",404);
                } else
                {
                    //Write this one file.
                    Program.QuickWriteJsonToDoc(e, build);
                }
            }
            
        }

        class OutputBuild
        {
            public string id;
            public string api_log;
            public string api_pbw;
            public bool passed;
            public string time;
        }
    }
}
