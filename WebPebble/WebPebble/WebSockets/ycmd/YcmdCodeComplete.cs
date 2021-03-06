﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WebPebble.WebSockets.ycmd.YcmdEntities;

namespace WebPebble.WebSockets.ycmd
{
    public static class YcmdCodeComplete
    {
        public static CompletionResponse GetCodeComplete(string filename, int col, int line, string data, YcmdProcesses p, out string commands)
        {
            //Create the request data to send.
            SimpleRequest req = new SimpleRequest();
            req.column_num = col;
            req.line_num = line;
            req.filepath = filename;
            req.force_semantic = true;
            req.file_data = new Dictionary<string, FileData>();
            req.file_data.Add(filename, GenerateFileData(data));
            //Send this data to the server and get a reply.
            CompletionResponse reply = YcmdController.SendYcmdRequest<CompletionResponse>("/completions", req, YcmdProcess.GetServer(p));
            commands = YcmdController.SendYcmdRequestRaw("/defined_subcommands", req, YcmdProcess.GetServer(p));
            return reply;
        }

        private static FileData GenerateFileData(string data)
        {
            var fs = new FileData();
            fs.contents = data;
            fs.filetypes = new string[] { "c" };
            return fs;
        }
    }
}
