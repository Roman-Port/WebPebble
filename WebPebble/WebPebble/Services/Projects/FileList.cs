﻿using System;
using System.Collections.Generic;
using System.Text;
using WebPebble.Entities;
using WebPebble.Oauth;

namespace WebPebble.Services.Projects
{
    public static class FileList
    {
        public static void ListFiles(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            Program.QuickWriteJsonToDoc(e, proj.assets);
        }
    }
}