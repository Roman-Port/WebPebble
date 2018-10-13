using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WebPebble.Entities.PebbleProject
{
    public class PebbleProject
    {
        public readonly string pathnane;
        public readonly string id;

        public PackageJson package;

        public PebbleProject(string _id)
        {
            //Get the pathname from the id.
            id = _id;
            pathnane = Program.config.user_project_dir + id + "/";
            //Load everything about this project from disk.
            //Read in the package json.
            package = JsonConvert.DeserializeObject<PackageJson>(ReadFile("package.json"));
        }

        public string ReadFile(string localPath)
        {
            return File.ReadAllText(pathnane + localPath);
        }

        public bool CheckIfExists(string localPath)
        {
            return File.Exists(pathnane + localPath);
        }

        public byte[] ReadFileBytes(string localPath)
        {
            return File.ReadAllBytes(pathnane + localPath);
        }

        public void WriteFile(string localPath, string data)
        {
            File.WriteAllText(pathnane + localPath,data);
        }

        public void WriteFile(string localPath, byte[] data)
        {
            File.WriteAllBytes(pathnane + localPath, data);
        }

        /* SAVING FUNCTIONS */
        public void SavePackage()
        {
            //Create our JSON settings.
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            //Save it to the disk.
            WriteFile("package.json", JsonConvert.SerializeObject(package, settings));
        }

        /* DOING FUNCTIONS */
        public bool DoBuild(out string log)
        {
            //Run the Pebble compile command.
            log = LinuxInterface.ExecuteCommand("pebble build", pathnane);
            return log.Contains("'build' finished successfully");
        }

        public string AddAsset(string name, string type, Medium template = null)
        {
            //Add an asset and return where it should be saved.
            //Check to see if the resources folder exists.
            if (!Directory.Exists(pathnane + "resources/"))
                Directory.CreateDirectory(pathnane + "resources/");
            //Write changes to the package.
            if (template == null)
                template = new Medium();
            //Generate an ID for this.
            string id = LibRpws.LibRpwsCore.GenerateRandomString(8);
            while (File.Exists(pathnane + "resources/" + id))
                id = LibRpws.LibRpwsCore.GenerateRandomString(8);
            //Edit the template with this new data.
            template.file = id;
            template.name = name;
            template.type = type;
            //Now, update the package json and save.
            package.pebble.resources.media.Add(template);
            SavePackage();
            //Now, return where this file should be saved.
            return pathnane + "resources/" + id;
        }

        /* CREATION FUNCTIONS */
        public static PebbleProject CreateProjectFiles(string id, string name, string author, bool isWatchface, string sdk_version)
        {
            //First, run the command to generate the files.
            LinuxInterface.ExecuteCommand("pebble new-project " + id, Program.config.user_project_dir);
            //Now, load the project from the files.
            PebbleProject proj = new PebbleProject(id);
            //Now, edit the package config file to apply the params offered.
            proj.package.name = name;
            proj.package.author = author;
            proj.package.pebble.companyName = author;
            proj.package.pebble.shortName = name;
            proj.package.pebble.displayName = name;
            proj.package.pebble.longName = name;
            proj.package.pebble.enableMultiJS = true;
            proj.package.pebble.projectType = "native";
            proj.package.pebble.watchapp.watchface = isWatchface;
            proj.package.pebble.sdkVersion = sdk_version;
            proj.package.pebble.resources = new Resources();
            proj.package.pebble.resources.media = new List<Medium>();
            //Rename the main.c. 
            try
            {
                File.Move(proj.pathnane + "src/c/" + id + ".c", proj.pathnane + "src/c/main.c");
            } catch
            {

            }
            //Save it
            proj.SavePackage();

            return proj;
        }
    }
}
