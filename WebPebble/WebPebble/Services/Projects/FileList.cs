using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WebPebble.Entities;
using WebPebble.Oauth;

namespace WebPebble.Services.Projects
{
    public static class FileList
    {
        public static async Task ListFiles(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            await Program.QuickWriteJsonToDoc(e, proj.assets);
        }
    }
}
