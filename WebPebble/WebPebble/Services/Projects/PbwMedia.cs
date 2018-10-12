using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WebPebble.Entities;
using WebPebble.Oauth;

namespace WebPebble.Services.Projects
{
    class PbwMedia
    {
        public static void OnRequest(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            //Just return the PBW that was asked for.
            string id = e.Request.Path.ToString().Split('/')[4];
            var build = proj.builds.Find(x => x.id == id);
            if (build == null)
            {
                //Not found.
                Program.QuickWriteToDoc(e, "Not Found", "text/plain", 404);
            }
            else
            {
                //Check if this build was okay.
                if(build.passed)
                {
                    //Safe to load file.
                    byte[] data = File.ReadAllBytes(Program.config.user_project_build_dir + proj.projectId + "/" + build.id + "/build.pbw");
                    Program.QuickWriteBytesToDoc(e, data, "application/octet-stream", 200);
                } else
                {
                    //There won't be a pbw. complain.
                    Program.QuickWriteToDoc(e, "This build failed and no PBW file was created.", "text/plain", 404);
                }
            }
        }
    }
}
