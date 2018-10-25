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
            long lineNo = (long)data.data["line_no"];
            long colNo = (long)data.data["col_no"];
            string unsavedBuffer = (string)data.data["buffer"];
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
            YcmdCodeCompleteReplyWs reply = new YcmdCodeCompleteReplyWs();
            /*if(new WebPebble.Entities.PebbleProject.PebbleProject(proj.projectId).package.pebble.sdkVersion == "2")
            {
                //Prompt only SDK 2.
                reply.sdks.Add("sdk2_aplite", ycmd.YcmdCodeComplete.GetCodeComplete(asset.GetAbsolutePath(projectId), (int)colNo, (int)lineNo, unsavedBuffer, ycmd.YcmdProcesses.Any));
            } else
            {
                //Prompt all platforms on SDK 3.
                reply.sdks.Add("sdk3_aplite", ycmd.YcmdCodeComplete.GetCodeComplete(asset.GetAbsolutePath(projectId), (int)colNo, (int)lineNo, unsavedBuffer, ycmd.YcmdProcesses.Any));
                reply.sdks.Add("sdk3_diorite", ycmd.YcmdCodeComplete.GetCodeComplete(asset.GetAbsolutePath(projectId), (int)colNo, (int)lineNo, unsavedBuffer, ycmd.YcmdProcesses.Any));
                reply.sdks.Add("sdk3_chalk", ycmd.YcmdCodeComplete.GetCodeComplete(asset.GetAbsolutePath(projectId), (int)colNo, (int)lineNo, unsavedBuffer, ycmd.YcmdProcesses.Any));
            }*/
            reply.sdks.Add("sdk", ycmd.YcmdCodeComplete.GetCodeComplete(asset.GetAbsolutePath(projectId), (int)colNo, (int)lineNo, unsavedBuffer, ycmd.YcmdProcesses.Any, out string commands));
            //Reply with this data.
            QuickReply(data.requestid, data.type, new Dictionary<string, object>() { { "ycmd", reply }, {"ycmd_commands",commands } });
        }

        class YcmdCodeCompleteReplyWs
        {
            public Dictionary<string, ycmd.YcmdEntities.CompletionResponse> sdks = new Dictionary<string, ycmd.YcmdEntities.CompletionResponse>();
        }
    }
}
