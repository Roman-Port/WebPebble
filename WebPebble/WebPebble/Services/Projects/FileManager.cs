using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using WebPebble.Entities;
using WebPebble.Entities.PebbleProject;
using WebPebble.Oauth;

namespace WebPebble.Services.Projects
{
    public static class FileManager
    {
        public static async Task OnRequest(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            string[] split = e.Request.Path.ToString().Split('/');
            string fileId = split[4];
            string action = split[5];
            //Get the Pebble project
            PebbleProject pp = new PebbleProject(proj.projectId);
            //Get the asset.
            var asset = proj.assets.Find(x => x.id == fileId);
            if(asset == null)
            {
                await Program.QuickWriteToDoc(e, "Not Found", "text/plain", 500);
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
                    await Program.QuickWriteJsonToDoc(e, reply);
                } else
                {
                    //Write the contents of the file only.
                    byte[] content = pp.ReadFileBytes(fileName);
                    await Program.QuickWriteBytesToDoc(e, content, mimeType);
                }

            } else if(action == "put")
            {
                //Get the bytes.
                byte[] data = new byte[(int)e.Request.ContentLength];
                e.Request.Body.Read(data, 0, data.Length);
                //Save
                pp.WriteFile(fileName, data);
                await Program.QuickWriteToDoc(e, "OK");
            }
            else if (action == "rename")
            {
                //We are just renaming an existing one.
                asset.nickname = e.Request.Query["name"];
                proj.SaveProject();
                await Program.QuickWriteToDoc(e, "OK");
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
                    await Program.QuickWriteToDoc(e, "OK");
                } else
                {
                    //Verification failed.
                    throw new Exception("Challenge failed.");
                }
                
            }
            else
            {
                await Program.QuickWriteToDoc(e, "Invalid action get/put.", "text/html", 500);
                
            }
        }

        public static async Task DeleteProject(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            //Confirm the challenge
            if (e.Request.Query["c"] != proj.projectId)
                return;

            //Delete the folder.
            Directory.Delete(proj.GetAbsolutePathname(), true);

            //Remove records 
            Program.database.GetCollection<WebPebbleProject>("projects").Delete(proj._id);

            //Respond
            await Program.QuickWriteJsonToDoc(e, new Dictionary<string, bool>
            {
                {"ok", true }
            });
        }

        public static async Task ZipProjectDownload(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            //Zip all of the project content and send it to the client encoded in base 64.
            using(MemoryStream ms = new MemoryStream())
            {
                using(ZipArchive zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    //Loop through every file in the data.
                    string root = proj.GetAbsolutePathname();
                    ZipEntireDirectory(zip, root, root.Length);
                }
                //Copy this stream to create base64 data.
                byte[] buf = new byte[ms.Length];
                ms.Position = 0;
                ms.Read(buf, 0, buf.Length);
                await Program.QuickWriteToDoc(e, Convert.ToBase64String(buf), "text/plain", 200);
            }
        }

        private static void ZipEntireDirectory(ZipArchive zip, string directory, int subStr)
        {
            //Add all files in this path first.
            foreach (string file in Directory.GetFiles(directory))
                ZipEntireFile(zip, file, subStr);
            //Add all directories
            foreach (string dir in Directory.GetDirectories(directory))
                ZipEntireDirectory(zip, dir, subStr);
        }

        private static void ZipEntireFile(ZipArchive zip, string path, int subStr)
        {
            //Add this as an entry
            zip.CreateEntryFromFile(path, path.Substring(subStr));
        }

        public static async Task CreateFileRequest(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            //Determine where to place these files.
            string filename = e.Request.Query["filename"];
            AssetType type = Enum.Parse<AssetType>(e.Request.Query["major_type"]);
            InnerAssetType inner = Enum.Parse<InnerAssetType>(e.Request.Query["minor_type"]);
            //Create
            var file = proj.CreateSafeAsset(filename, type, inner, new byte[] { });
            //Respond with JSON string.
            await Program.QuickWriteJsonToDoc(e, file);
        }

        public static async Task AppInfoJson(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            //Edit or serve appinfo.json.
            PebbleProject pp = new PebbleProject(proj.projectId);
            if(e.Request.Method.ToLower() == "get")
            {
                await Program.QuickWriteJsonToDoc(e, pp.package);
            }
            if(e.Request.Method.ToLower() == "put")
            {
                await Program.QuickWriteToDoc(e, "The put endpoint has been disabled due to a security hole.", "text/plain", 404);
            }
        }

        public static async Task AppInfoJson_DeleteResource(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            PebbleProject pp = new PebbleProject(proj.projectId);
            //Find the resource.
            if(!e.Request.Query.ContainsKey("id"))
            {
                await Program.QuickWriteToDoc(e, "No resource ID specified.", "text/html", 404);
                return;
            }
            string resourceId = e.Request.Query["id"];
            var item = pp.package.pebble.resources.media.Find(x => x.x_webpebble_pebble_media_id == resourceId);
            if (item == null)
            {
                await Program.QuickWriteToDoc(e, "That item didn't exist.", "text/html", 404);
                return;
            }
            //Remove and save.
            pp.package.pebble.resources.media.Remove(item);
            pp.SavePackage();
            await Program.QuickWriteToDoc(e, "OK");
        }

        public static async Task AppInfoJson_AddResource(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
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
                await Program.QuickWriteToDoc(e, "Invalid x_webpebble_media_id.", "text/html", 404);
                return;
            }
            //Set the filename in a secure matter. Remove the "resources/" at the beginning.
            medium.file = asset.filename.Substring("resources/".Length);
            //Check if the Pebble media id was sent. If not, generate it. If it was, we use that and replace.
            if(medium.x_webpebble_pebble_media_id == null)
            {
                medium.x_webpebble_pebble_media_id = LibRpws.LibRpwsCore.GenerateRandomString(8);
                while (pp.package.pebble.resources.media.Find(x => x.x_webpebble_pebble_media_id == medium.x_webpebble_pebble_media_id) != null)
                    medium.x_webpebble_pebble_media_id = LibRpws.LibRpwsCore.GenerateRandomString(8);
            }
            //Check to see if the package already has this data.
            if (pp.package.pebble.resources.media.Find( x => x.x_webpebble_pebble_media_id == medium.x_webpebble_pebble_media_id) != null)
            {
                //Remove this from the package.
                pp.package.pebble.resources.media.Remove(pp.package.pebble.resources.media.Find(x => x.x_webpebble_media_id == medium.x_webpebble_media_id));
            }
            //Write this to the package and save it.
            pp.package.pebble.resources.media.Add(medium);
            pp.SavePackage();
            //Respond with OK.
            await Program.QuickWriteJsonToDoc(e,medium);
        }

        public static async Task CheckIfIdentifierExists(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            //Get the project file and check if the identifier exists.
            PebbleProject pp = new PebbleProject(proj.projectId);
            bool exists = pp.package.pebble.resources.media.Find(x => x.name == e.Request.Query["resrc_id"]) != null;
            CheckIfIdentifierExistsReply reply = new CheckIfIdentifierExistsReply
            {
                exists = exists,
                request_id = e.Request.Query["request_id"]
            };
            await Program.QuickWriteJsonToDoc(e, reply);
        }

        class CheckIfIdentifierExistsReply
        {
            public bool exists;
            public string request_id;
        }

        public static async Task OnProjSettingsRequest(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            //Hide bandwidth-intensive bits.
            proj.builds = null;
            //Serve the project's data.
            await Program.QuickWriteJsonToDoc(e, proj);
        }

        public static async Task UploadFile(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            //Get the file uploaded.
            var f = e.Request.Form.Files["data"];
            //Check if the file is valid.
            if(f.Length == 0 || f.OpenReadStream() == null)
            {
                //No file uploaded.
                await Program.QuickWriteToDoc(e, "No file was uploaded.", "text/plain", 400);
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
            await Program.QuickWriteJsonToDoc(e, asset);
        }

        /*public static void DeleteUploadedFile(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            //Take in the ID to the media and see if exists.
            if (!e.Request.Query.ContainsKey("id"))
            {
                Program.QuickWriteToDoc(e, "Invalid ID.", "text/html", 404);
                return;
            }
            var asset = proj.assets.Find(x => x.id == e.Request.Query["id"]);
            if (asset == null)
            {
                Program.QuickWriteToDoc(e, "Invalid ID.", "text/html", 404);
                return;
            }
            //Delete the file for this media.
            string absolutePath = proj.GetAbsolutePathname() + asset.filename;
            File.Delete(absolutePath);
            //Remove the asset from the database.
            proj.assets.Remove(asset);
            proj.SaveProject();
            //Respond with the OK.
            Program.QuickWriteToDoc(e, "OK");
        }*/
        //I made the above and then realized that it already exisited.

        private class FileReply
        {
            public string type;
            public string content;
            public string saveUrl;
            public string id;
        }
    }
}
