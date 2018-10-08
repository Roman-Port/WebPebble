using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace WebPebble
{
    /* Everything for communicating with Linux. */
    public static class LinuxInterface
    {
        public static string ExecuteCommand(string cmd, string pathname)
        {
            //Run the Linux command.
            //Thanks to https://loune.net/2017/06/running-shell-bash-commands-in-net-core/ for telling me how to do this in the .NET Core world.
            //First, CD into what we'd like.
            cmd = "cd " + pathname + "\n";
            //Now, escape it.
            var escapedArgs = cmd.Replace("\"", "\\\"");
            //Now, run it and redirect output.
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result;
        }
    }
}
