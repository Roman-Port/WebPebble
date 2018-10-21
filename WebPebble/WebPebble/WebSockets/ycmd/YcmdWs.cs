using System;
using System.Collections.Generic;
using System.Text;
using WebPebble.Entities;
using WebSocketSharp.Server;
using System.Linq;

namespace WebPebble.WebSockets
{
    public partial class WebPebbleClient : WebSocketBehavior
    {
        private void OnYcmdRequest(WebPebbleRequest data)
        {
            //Get the data passed in.
            string assetId = (string)data.data["asset_id"];
            string projectId = (string)data.data["project_id"];
            int lineNo = (int)data.data["line_no"];
            int colNo = (int)data.data["col_no"];
            //Get the project requested.
            WebPebbleProject proj = null;
            var collect = Program.database.GetCollection<WebPebbleProject>("projects");
            var projects = collect.Find(x => x.projectId == projectId && x.authorId == user_uuid);
            if (projects.Count() == 1)
                proj = projects.ToArray()[0];
            if (proj == null)
                return;
            //Now, find the asset inside the project.
            var asset = proj.assets.Find(x => x.id == assetId);
            if (asset == null)
                return;
            //Now that we have the pathname, prompt the proxy.
            var reply = ycmd.YcmdCodeComplete.GetCodeComplete(asset.filename, colNo, lineNo);
            //Reply with this data.
            QuickReply(data.requestid, data.type, new Dictionary<string, object>() { { "ycmd", reply } });
        }
    }
}
