using Newtonsoft.Json;
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
            else if (action == "rename")
            {
                //We are just renaming an existing one.
                asset.nickname = e.Request.Query["name"];
                proj.SaveProject();
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

        public static void AppInfoJson(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            //Edit or serve appinfo.json.
            PebbleProject pp = new PebbleProject(proj.projectId);
            if(e.Request.Method.ToLower() == "get")
            {
                Program.QuickWriteJsonToDoc(e, pp.package);
            }
            if(e.Request.Method.ToLower() == "put")
            {
                Program.QuickWriteToDoc(e, "The put endpoint has been disabled due to a security hole.", "text/plain", 404);
            }
        }

        public static void AppInfoJson_DeleteResource(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            PebbleProject pp = new PebbleProject(proj.projectId);
            //Find the resource.
            if(!e.Request.Query.ContainsKey("id"))
            {
                Program.QuickWriteToDoc(e, "No resource ID specified.", "text/html", 404);
                return;
            }
            string resourceId = e.Request.Query["id"];
            var item = pp.package.pebble.resources.media.Find(x => x.x_webpebble_media_id == resourceId);
            if (item == null)
            {
                Program.QuickWriteToDoc(e, "That item didn't exist.", "text/html", 404);
                return;
            }
            //Remove and save.
            pp.package.pebble.resources.media.Remove(item);
            pp.SavePackage();
            Program.QuickWriteToDoc(e, "OK");
        }

        public static void AppInfoJson_AddResource(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            PebbleProject pp = new PebbleProject(proj.projectId);
            //Open the body.
            byte[] data = new byte[(int)e.Request.ContentLength];
            e.Request.Body.Read(data, 0, data.Length);
            Medium medium = JsonConvert.DeserializeObject<Medium>(Encoding.UTF8.GetString(data));
            //Get the asset that is specified.
            WebPebbleProjectAsset asset = proj.assets.Find(x => x.id == medium.x_webpebble_media_id);
            //Check to see if the asset is valid.
            if(asset == null)
            {
                Program.QuickWriteToDoc(e, "Invalid x_webpebble_media_id.", "text/html", 404);
                return;
            }
            //Set the filename in a secure matter.
            medium.file = asset.filename;
            //Check to see if the package already has this data.
            if(pp.package.pebble.resources.media.Find( x => x.x_webpebble_media_id == medium.x_webpebble_media_id) != null)
            {
                //Remove this from the package.
                pp.package.pebble.resources.media.Remove(pp.package.pebble.resources.media.Find(x => x.x_webpebble_media_id == medium.x_webpebble_media_id));
            }
            //Write this to the package and save it.
            pp.package.pebble.resources.media.Add(medium);
            pp.SavePackage();
            //Respond with OK.
            Program.QuickWriteToDoc(e, "OK");
        }

        public static void OnProjSettingsRequest(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            //Hide bandwidth-intensive bits.
            proj.builds = null;
            //Serve the project's data.
            Program.QuickWriteJsonToDoc(e, proj);
        }

        public static void UploadFile(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            //Get the file uploaded.
            var f = e.Request.Form.Files["data"];
            //Check if the file is valid.
            if(f.Length == 0)
            {
                //No file uploaded.
                Program.QuickWriteToDoc(e, "No file was uploaded.", "text/plain", 400);
                return;
            }
            //Get the asset params
            AssetType type = Enum.Parse<AssetType>(e.Request.Query["type"]);
            InnerAssetType innerType = Enum.Parse<InnerAssetType>(e.Request.Query["sub_type"]);
            string nickname = e.Request.Query["nickname"]; //The display name.
            //Create an ID.
            string id = DateTime.UtcNow.Ticks.ToString() + LibRpws.LibRpwsCore.GenerateRandomString(8);
            //Create the filename.
            string filename = type.ToString() + "/" + innerType.ToString()+"/";
            //Create directory
            Directory.CreateDirectory(Program.config.user_project_dir + proj.projectId + "/" + filename);
            //Finish filename
            filename += id;
            //Create the asset.
            WebPebbleProjectAsset asset = proj.AddAsset(filename, type, innerType,nickname);
            //Write to this file.
            using (FileStream fs = new FileStream(Program.config.user_project_dir + proj.projectId + "/" + filename, FileMode.CreateNew))
                f.OpenReadStream().CopyTo(fs);
            //Create a response.
            Program.QuickWriteJsonToDoc(e, asset);
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
