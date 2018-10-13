using System;
using System.Collections.Generic;
using System.Text;

namespace WebPebble.Entities.PebbleProject
{
    public class Medium
    {
        public string file { get; set; }
        public string name { get; set; }
        public List<string> targetPlatforms { get; set; }
        public bool menuIcon { get; set; }
        public string type { get; set; }

        //Font specific
        public int trackingAdjust { get; set; }
        public string characterRegex { get; set; }
        public string compatibility { get; set; }

        //Bitmap specific
        public string memoryFormat { get; set; }
        public string spaceOptimization { get; set; }
        public string storageFormat { get; set; }
    }

    public class Resources
    {
        public List<Medium> media { get; set; }
    }

    public class Watchapp
    {
        public bool watchface { get; set; }
    }

    public class Pebble
    {
        public Dictionary<string, int> appKeys { get; set; }
        public List<string> capabilities { get; set; }
        public string companyName { get; set; }
        public bool enableMultiJS { get; set; }
        public string longName { get; set; }
        public string projectType { get; set; }
        public Resources resources { get; set; }
        public string sdkVersion { get; set; }
        public string shortName { get; set; }
        public List<string> targetPlatforms { get; set; }
        public string uuid { get; set; }
        public string versionLabel { get; set; }
        public Watchapp watchapp { get; set; }
    }

    public class PackageJson
    {
        public string author { get; set; }
        public List<object> keywords { get; set; }
        public string name { get; set; }
        public Pebble pebble { get; set; }
        public string version { get; set; }
    }
}
