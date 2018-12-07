using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using WebPebble.Entities;
using WebPebble.Oauth;

namespace WebPebble.Services.Projects
{
    public class ProjectZipper
    {
        public static async Task ZipProjectDownload(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            //Zip all of the project content and send it to the client encoded in base 64.
            using (MemoryStream ms = new MemoryStream())
            {
                using (ZipArchive zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
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
    }
}
