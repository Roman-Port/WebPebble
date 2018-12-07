﻿using Newtonsoft.Json;
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
        /// <summary>
        /// List of templates. Key is the name, value is the path.
        /// </summary>
        private static readonly Dictionary<string, string> templateMap = new Dictionary<string, string>
        {
            {"blank", "blank.txt" },
            {"default_c", "main.c" },
            {"pkjs", "pkjs.js" }
        };

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

        private static async Task ThrowError(Microsoft.AspNetCore.Http.HttpContext e, string error, int errorCode, int httpErrorCode = 500)
        {
            await Program.QuickWriteJsonToDoc(e, new ErrorReply
            {
                ok = false,
                message = error,
                code = errorCode
            }, httpErrorCode);
        }

        private class ErrorReply
        {
            public bool ok;
            public string message;
            public int code;
        }

        public static async Task OnMediaCreateRequest(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            //Validate that the request method is indeed POST.
            RequestHttpMethod method = Program.FindRequestMethod(e);
            if(method != RequestHttpMethod.post)
            {
                await ThrowError(e, "Unknown method.", 2);
                return;
            }
            //Read the JSON data from the stream.
            MediaCreateRequestBody request = Program.GetPostBodyJson<MediaCreateRequestBody>(e);
            //Validate
            if(request.filename == null || request.name == null || request.sub_type == null || request.type == null)
            {
                await ThrowError(e, "Missing one or more required values in the JSON payload.", 3);
                return;
            }
            //Generate a unique ID
            string id = LibRpws.LibRpwsCore.GenerateRandomString(16);
            if (proj.media == null)
                proj.media = new Dictionary<string, WebPebbleProjectAsset>();
            while(proj.media.ContainsKey(id))
                id = LibRpws.LibRpwsCore.GenerateRandomString(16);
            //Create the object to save to disk.
            var media = new WebPebbleProjectAsset();
            //Find types from URL.
            media.type = request.type;
            media.innerType = request.sub_type;
            media.nickname = request.name;
            media.id = id;
            //Ensure directory is created.
            media.filename = "";
            Directory.CreateDirectory(media.GetAbsolutePath(proj.projectId));
            //Append filename
            media.filename = WebPebbleProject.CreateSafeFilename(request.filename);
            //If this was a template, load it.
            if(request.template != null)
            {
                //Try to find the template ID.
                if(templateMap.ContainsKey(request.template))
                {
                    //Write this template to the location.
                    File.Copy(Program.config.media_dir + "Templates/" + templateMap[request.template], media.GetAbsolutePath(proj.projectId));
                } else
                {
                    //Invalid template!
                    await ThrowError(e, "Invalid template name.", 6);
                    return;
                }
            }
            //Save
            proj.media.Add(id, media);
            proj.SaveProject();
            //Write to user
            await Program.QuickWriteJsonToDoc(e, media);
        }

        private class MediaCreateRequestBody
        {
            public AssetType type;
            public InnerAssetType sub_type;
            public string name;
            public string filename;

            //Optional

            public string template;
        }

        private static async Task WriteOkReply(Microsoft.AspNetCore.Http.HttpContext e)
        {
            await Program.QuickWriteJsonToDoc(e, new Dictionary<string, bool>
            {
                {"ok", true }
            });
        }

        public static async Task OnMediaRequest(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            //Get the request method
            RequestHttpMethod method = Program.FindRequestMethod(e);
            //Get the params
            string[] urlParams = Program.GetUrlPathRequestFromInsideProject(e);
            string id = urlParams[0];
            //If the ID is create, pass this request to the creation.
            if(id == "create")
            {
                await OnMediaCreateRequest(e, user, proj);
                return;
            }
            //Try to find the asset. It's ok if it doesn't exist.
            WebPebbleProjectAsset media = null;
            if (proj.media.ContainsKey(id))
                media = proj.media[id];
            //Decide what to do.
            //If it doesn't exist.
            if(media == null)
            {
                //Requesting a non-existing asset.
                await ThrowError(e, "This asset ID does not exist.", 1);
                return;
            }
            //Handle object name editing (POST).
            if(method == RequestHttpMethod.post)
            {
                //Get new name from URL.
                media.nickname = e.Request.Query["name"];
                proj.media[id] = media;
                proj.SaveProject();
                await WriteOkReply(e);
                return;
            }
            //Handle object uploading.
            if(method == RequestHttpMethod.put)
            {
                //Check the upload type in the query. 
                FileUploadType uploadType = Enum.Parse<FileUploadType>(e.Request.Query["upload_method"]);
                byte[] buf;
                if(uploadType == FileUploadType.Binary)
                {
                    //Read body
                    buf = new byte[(int)e.Request.ContentLength];
                    e.Request.Body.Read(buf, 0, buf.Length);
                } else
                {
                    //TODO
                    throw new NotImplementedException();
                }
                //Remove an existing file if it exists.
                if (File.Exists(media.GetAbsolutePath(proj.projectId)))
                    File.Delete(media.GetAbsolutePath(proj.projectId));
                //Save
                File.WriteAllBytes(media.GetAbsolutePath(proj.projectId), buf);
                //Tell the user it is ok
                await WriteOkReply(e);
                return;
            }
            //Handle object downloading
            if(method == RequestHttpMethod.get)
            {
                Console.WriteLine("get");
                //Check if the file has been created yet
                string path = media.GetAbsolutePath(proj.projectId);
                if(!File.Exists(path))
                {
                    await ThrowError(e, "This asset exists, but has not been uploaded yet.", 4);
                    return;
                }
                //Get the MIME type from the query.
                if(!e.Request.Query.ContainsKey("mime"))
                {
                    await ThrowError(e, "No MIME type sent in query.", 5);
                    return;
                }
                //Set content type.
                e.Response.ContentType = e.Request.Query["mime"];
                //Just load the data and copy it to the output stream.
                using (FileStream fs = new FileStream(path, FileMode.Open)) {
                    e.Response.ContentLength = fs.Length;
                    e.Response.StatusCode = 200;
                    Console.WriteLine(fs.Length);
                    await fs.CopyToAsync(e.Response.Body);
                }
                return;
            }
            //Unknown.
            await ThrowError(e, $"Invalid method for requesting media '{media.id}'.", 5);
        } 

        private enum FileUploadType
        {
            Binary, //Just directly read from the input strema.
            Form //Read the body as form data.
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

        /*
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
        }*/
    }
}
