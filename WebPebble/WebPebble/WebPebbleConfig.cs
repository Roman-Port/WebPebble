using System;
using System.Collections.Generic;
using System.Text;

namespace WebPebble
{
    public class WebPebbleConfig
    {
        public string user_project_dir;
        public string database_file;
        public string user_project_build_dir;
        public string static_files_dir;
        public string pebble_sdk_dir;
        public string media_dir; //Root of the binary data.
        public string ssl_cert;

        public string ycmd_binary;
        public string temp_files;
    }
}
