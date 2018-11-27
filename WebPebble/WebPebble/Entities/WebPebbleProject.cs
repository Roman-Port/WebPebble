using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WebPebble.Entities
{
    public class WebPebbleProject
    {
        public int _id { get; set; } //Internal ID for the database.

        public string projectId { get; set; } //Actual ID to use.
        public string authorId { get; set; } //RPWS author id.

        public string name { get; set; }

        public List<WebPebbleProjectAsset> assets { get; set; }
        public List<WebPebbleProjectBuild> builds { get; set; }

        //Functions for creation.
        public static WebPebbleProject CreateProject(string name, string authorName, string authorId, bool isWatchface, string sdk_version)
        {
            //First, generate an ID to use.
            var collect = Program.database.GetCollection<WebPebbleProject>("projects");
            string id = LibRpws.LibRpwsCore.GenerateRandomHexString(16);
            while (collect.Find(x => x.projectId == id).ToArray().Length != 0)
                id = LibRpws.LibRpwsCore.GenerateRandomHexString(16);
            //Now that we have a unique ID, create the files for it.
            PebbleProject.PebbleProject proj = PebbleProject.PebbleProject.CreateProjectFiles(id, name, authorName, isWatchface,sdk_version);
            //Now, create the object and save it to the database.
            WebPebbleProject wpp = new WebPebbleProject
            {
                authorId = authorId,
                projectId = id,
                name = name
            };
            wpp._id = collect.Insert(wpp);
            //Now, add the asset.
            wpp.AddAsset("src/c/main.c", AssetType.src, InnerAssetType.c);
            //Return the end product.
            return wpp;
        }

        public void SaveProject()
        {
            Program.database.GetCollection<WebPebbleProject>("projects").Update(this);
        }

        public string GetAbsolutePathname()
        {
            return Program.config.user_project_dir + projectId + "/";
        }

        //Assets
        
        public WebPebbleProjectAsset AddAsset(string filename, AssetType type, InnerAssetType inner, string nickname = null)
        {
            WebPebbleProjectAsset a = new WebPebbleProjectAsset();
            a.filename = filename;
            a.type = type;
            a.innerType = inner;
            a.nickname = nickname;
            //Generate an ID.
            string id = LibRpws.LibRpwsCore.GenerateRandomHexString(8);
            //Save
            a.id = id;
            if (assets == null)
                assets = new List<WebPebbleProjectAsset>();
            assets.Add(a);

            SaveProject();
            return a;
        }

        public WebPebbleProjectAsset CreateSafeAsset(string filename, AssetType type, InnerAssetType inner, byte[] data)
        {
            //Safely store a file somewhere. Protect the name against injection attacks.
            foreach(char invalidC in System.IO.Path.GetInvalidFileNameChars())
            {
                filename = filename.Replace(invalidC, '_');
            }
            //If it starts with a period, stop.
            if (filename.StartsWith('.'))
                throw new Exception("Rejected pathname.");
            //Create a relative pathname now.
            string relPath = type.ToString() + "/" + inner.ToString()+"/";
            //Save the file here.
            string absolutePath = Program.config.user_project_dir + projectId + "/"+relPath;
            //Create directory
            Directory.CreateDirectory(absolutePath);
            //Append filename
            relPath += filename;
            absolutePath += filename;
            //Check if it already exists.
            if(File.Exists(absolutePath))
            {
                throw new Exception("Already exists.");
            }
            File.WriteAllBytes(absolutePath, data);
            //Add it as an asset.
            return AddAsset(relPath, type, inner);
        }
    }

    public class WebPebbleProjectAsset
    {
        public string id { get; set; }
        public string filename { get; set; }

        public string nickname { get; set; } //Custom name.

        public AssetType type { get; set; }
        public InnerAssetType innerType { get; set; }

        public string GetAbsolutePath(string projectId)
        {
            return Program.config.user_project_dir + projectId + "/"+ filename; 
        }
    }

    public class WebPebbleProjectBuild
    {
        public string id { get; set; }
        public long time { get; set; }
        public bool passed { get; set; }
        public string log { get; set; }
    }

    public enum AssetType
    {
        src,
        resources,
    }

    public enum InnerAssetType
    {
        c,
        
        /* resources */
        images,
        fonts,
        data,
        /* js */
        pkjs
    }
}
