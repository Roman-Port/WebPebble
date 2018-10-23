using System;
using System.Collections.Generic;
using System.Text;

namespace WebPebble.WebSockets.ycmd.YcmdEntities
{
    public class SimpleRequest
    {
        public int column_num;
        public int line_num;
        public string filepath;
        public Dictionary<string, FileData> file_data;
        public bool force_semantic;
    }

    public class FileData
    {
        public FileDataPath path;
    }

    public class FileDataPath
    {
        public string contents;
        public string[] filetypes;
    }
}
