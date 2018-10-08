using System;
using System.Collections.Generic;
using System.Text;

namespace WebPebble.Entities.PebbleProject
{
    public class Dependencies
    {
    }

    public class Medium
    {
        public string characterRegex { get; set; }
        public string file { get; set; }
        public string name { get; set; }
        public List<string> targetPlatforms { get; set; }
        public string type { get; set; }
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
        public string displayName { get; set; }
        public bool enableMultiJS { get; set; }
        public List<object> messageKeys { get; set; }
        public string projectType { get; set; }
        public Resources resources { get; set; }
        public string sdkVersion { get; set; }
        public List<string> targetPlatforms { get; set; }
        public string uuid { get; set; }
        public Watchapp watchapp { get; set; }
    }

    public class PackageJson
    {
        public string author { get; set; }
        public Dependencies dependencies { get; set; }
        public List<object> keywords { get; set; }
        public string name { get; set; }
        public Pebble pebble { get; set; }
        public string version { get; set; }
    }
}
