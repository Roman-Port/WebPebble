using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WebPebble
{
    public static class TemplateManager
    {
        public static string GetTemplate(string pathname, string[] keys, string[] values)
        {
            if (keys.Length != values.Length)
                throw new Exception("Failed to load template. Keys and values length do not match.");

            string template = File.ReadAllText(pathname);
            for (int i = 0; i < keys.Length; i++)
            {
                template = template.Replace(keys[i], values[i]);
            }
            return template;
        }
    }
}
