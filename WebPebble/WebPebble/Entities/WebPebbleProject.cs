using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebPebble.Entities
{
    public class WebPebbleProject
    {
        public int _id { get; set; } //Internal ID for the database.

        public string projectId { get; set; } //Actual ID to use.
        public string authorId { get; set; } //RPWS author id.

        public List<WebPebbleProjectAsset> assets { get; set; }

        //Functions for creation.
        public static WebPebbleProject CreateProject(string name, string authorName, string authorId, bool isWatchface)
        {
            //First, generate an ID to use.
            var collect = Program.database.GetCollection<WebPebbleProject>("projects");
            string id = LibRpws.LibRpwsCore.GenerateRandomHexString(16);
            while (collect.Find(x => x.projectId == id).ToArray().Length != 0)
                id = LibRpws.LibRpwsCore.GenerateRandomHexString(16);
            //Now that we have a unique ID, create the files for it.
            PebbleProject.PebbleProject proj = PebbleProject.PebbleProject.CreateProjectFiles(id, name, authorName, isWatchface);
            //Now, create the object and save it to the database.
            WebPebbleProject wpp = new WebPebbleProject
            {
                authorId = authorId,
                projectId = id
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

        //Assets
        
        public WebPebbleProjectAsset AddAsset(string filename, AssetType type, InnerAssetType inner)
        {
            WebPebbleProjectAsset a = new WebPebbleProjectAsset();
            a.filename = filename;
            a.type = type;
            a.innerType = inner;
            //Generate an ID.
            string id = LibRpws.LibRpwsCore.GenerateRandomHexString(8);
            //Save
            a.id = id;
            assets.Add(a);
            SaveProject();
            return a;
        }
    }

    public class WebPebbleProjectAsset
    {
        public string id { get; set; }
        public string filename { get; set; }

        public AssetType type { get; set; }
        public InnerAssetType innerType { get; set; }
    }

    public enum AssetType
    {
        src,
        resources,
    }

    public enum InnerAssetType
    {
        c,
        fonts,
        images
    }
}
