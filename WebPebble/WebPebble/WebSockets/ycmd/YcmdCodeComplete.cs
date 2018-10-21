using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WebPebble.WebSockets.ycmd.YcmdEntities;

namespace WebPebble.WebSockets.ycmd
{
    public static class YcmdCodeComplete
    {
        public static CompletionResponse GetCodeComplete(string filename, int col, int line)
        {
            //Create the request data to send.
            SimpleRequest req = new SimpleRequest();
            req.column_num = col;
            req.line_num = line;
            req.filepath = filename;
            req.force_semantic = true;
            req.file_data = new FileData();
            req.file_data.path = new FileDataPath();
            req.file_data.path.contents = File.ReadAllText(filename);
            req.file_data.path.filetypes = new string[] { "c" };
            //Send this data to the server and get a reply.
            CompletionResponse reply = YcmdController.SendYcmdRequest<CompletionResponse>("/completions", req);
            return reply;
        }
    }
}
