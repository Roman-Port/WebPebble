using System;
using System.Collections.Generic;
using System.Text;

namespace WebPebble.WebSockets.ycmd.YcmdEntities
{
    public class CompletionResponse
    {
        public int completion_start_column;
        public CompletionResponseCompletions[] completions;
    }

    public class CompletionResponseCompletions
    {
        public string insertion_text;
        public string menu_text;
        public string extra_menu_info;
        public string detailed_info;
        public string kind;
    }
}
