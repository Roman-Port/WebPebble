using System;
using System.Collections.Generic;
using System.Text;
using WebPebble.Entities;
using WebPebble.Entities.PebbleProject;
using WebPebble.Oauth;

namespace WebPebble.Services.Projects
{
    public static class FileManager
    {
        public static void OnRequest(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            string[] split = e.Request.Path.ToString().Split('/');
            string fileType = split[4];
            string fileId = split[5];
            string action = split[6];
            string mimeType = split[7].Replace('_','/');
            if(action == "get")
            {
                //Fetch the file.
                PebbleProject pp = new PebbleProject(proj.projectId);
                string fileName = fileType+"/" + fileId.Replace(".", "").Replace("/", "");
                //Check if the file exists
                bool exists = pp.CheckIfExists(fileName);
                if(!exists)
                {
                    Program.QuickWriteToDoc(e, "Not Found", "text/plain", 404);
                    return;
                }
                //Serialize this, if json.
                if(mimeType == "application/json")
                {
                    //Use JSON.
                    FileReply reply = new FileReply();
                    reply.type = "c_cpp";
                    string content = pp.ReadFile(fileName);
                    reply.saveUrl = e.Request.Protocol + "://" + e.Request.Host + "/project/" + proj.projectId + "/media/" + fileType + "/" + fileId + "/put/";
                    reply.content = content;
                    //Respond with JSON string.
                    Program.QuickWriteJsonToDoc(e, reply);
                } else
                {
                    //Write the contents of the file only.
                    byte[] content = pp.ReadFileBytes(fileName);
                    Program.QuickWriteBytesToDoc(e, content, mimeType);
                }

            }
        }

        private class FileReply
        {
            public string type;
            public string content;
            public string saveUrl;
        }
    }
}
