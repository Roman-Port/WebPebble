using System;
using System.Collections.Generic;
using System.IO;
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
            e.Response.Headers.Add("Access-Control-Allow-Origin", "*");

            string[] split = e.Request.Path.ToString().Split('/');
            string fileId = split[4];
            string action = split[5];
            //Get the Pebble project
            PebbleProject pp = new PebbleProject(proj.projectId);
            //Get the asset.
            var asset = proj.assets.Find(x => x.id == fileId);
            if(asset == null)
            {
                Program.QuickWriteToDoc(e, "Not Found", "text/plain", 500);
                return;
            }
            string fileName = asset.filename;
            //Console.WriteLine(fileName);
            if (action == "get")
            {
                string mimeType = split[6].Replace('_', '/');
                //Fetch the file
                //Serialize this, if json.
                if(mimeType == "application/json")
                {
                    //Use JSON.
                    FileReply reply = new FileReply();
                    reply.type = "c_cpp";
                    string content = pp.ReadFile(fileName);
                    string proto = "http";
                    if (e.Request.IsHttps)
                        proto = "https";
                    reply.saveUrl = proto + "://" + e.Request.Host + "/project/" + proj.projectId + "/media/" + fileId + "/put/";
                    reply.content = content;
                    reply.id = fileId;
                    //Respond with JSON string.
                    Program.QuickWriteJsonToDoc(e, reply);
                } else
                {
                    //Write the contents of the file only.
                    byte[] content = pp.ReadFileBytes(fileName);
                    Program.QuickWriteBytesToDoc(e, content, mimeType);
                }

            } else if(action == "put")
            {
                //Get the bytes.
                byte[] data = new byte[(int)e.Request.ContentLength];
                e.Request.Body.Read(data, 0, data.Length);
                //Save
                pp.WriteFile(fileName, data);
                Program.QuickWriteToDoc(e, "OK");
            }
            else if (action == "delete")
            {
                //Get the challenge bytes.
                byte[] data = new byte[(int)e.Request.ContentLength];
                e.Request.Body.Read(data, 0, data.Length);
                //Compare challenge.
                if(e.Request.Query["challenge"] == Encoding.UTF8.GetString(data))
                {
                    //Nuke the files
                    File.Delete(pp.pathnane + fileName);
                    //Nuke in the assets of the project.
                    proj.assets.Remove(asset);
                    proj.SaveProject();
                    Program.QuickWriteToDoc(e, "OK");
                } else
                {
                    //Verification failed.
                    throw new Exception("Challenge failed.");
                }
                
            }
            else
            {
                Program.QuickWriteToDoc(e, "Invalid action get/put.", "text/html", 500);
                
            }
        }

        public static void CreateFileRequest(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            //Determine where to place these files.
            string filename = e.Request.Query["filename"];
            AssetType type = Enum.Parse<AssetType>(e.Request.Query["major_type"]);
            InnerAssetType inner = Enum.Parse<InnerAssetType>(e.Request.Query["minor_type"]);
            //Create
            var file = proj.CreateSafeAsset(filename, type, inner, new byte[] { });
            //Respond with JSON string.
            Program.QuickWriteJsonToDoc(e, file);
        }

        private class FileReply
        {
            public string type;
            public string content;
            public string saveUrl;
            public string id;
        }
    }
}
