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
            //Check to see if we need to fill the Pebble SDK.
            if (cmd.StartsWith("pebble"))
                cmd = GetPebble() + cmd.Substring("pebble".Length);
            //Escape
            var escapedArgs = cmd.Replace("\"", "\\\"");
            //Now, run it and redirect output.
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    WorkingDirectory = pathname,
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            Console.WriteLine(result);
            return result;
        }

        public static string GetPebble()
        {
            return "~/webpebble/pebble_sdk/pebble-sdk-4.5-linux64/bin/pebble";
        }
    }
}
