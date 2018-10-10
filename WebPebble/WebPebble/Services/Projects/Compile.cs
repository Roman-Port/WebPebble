using System;
using System.Collections.Generic;
using System.Text;
using WebPebble.Entities;
using WebPebble.Entities.PebbleProject;
using WebPebble.Oauth;
using System.Linq;
using System.IO;

namespace WebPebble.Services.Projects
{
    public static class Compile
    {
        public static void DoCompile(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            //Compile this Pebble project.
            PebbleProject pp = new PebbleProject(proj.projectId);
            //Compile
            bool ok = pp.DoBuild(out string log);
            //Generate an ID.
            if (proj.builds == null)
                proj.builds = new List<WebPebbleProjectBuild>();
            string id = LibRpws.LibRpwsCore.GenerateRandomHexString(8);
            while(proj.builds.Where(x => x.id == id).ToArray().Length != 0)
                id = LibRpws.LibRpwsCore.GenerateRandomHexString(8);
            //Create the object.
            WebPebbleProjectBuild b = new WebPebbleProjectBuild();
            b.id = id;
            b.log = log;
            b.passed = ok;
            b.time = DateTime.UtcNow.Ticks;
            //Add this and save
            proj.builds.Add(b);
            proj.SaveProject();
            //If this build passed, move the pbw into a project folder.
            Directory.CreateDirectory(Program.config.user_project_build_dir + proj.projectId + "/" + id + "/");
            File.Move(pp.pathnane + "build/" + proj.projectId + ".pbw", Program.config.user_project_build_dir + proj.projectId + "/" + id + "/" + "build.pbw");
            //Clean up.
            Directory.Delete(pp.pathnane + "build/", true);
            //Create a reply.
            Program.QuickWriteJsonToDoc(e, b);
        }
    }
}
