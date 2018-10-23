using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace WebPebble.WebSockets.ycmd
{
    public static class YcmdProcess
    {
        public static Process p;

        public static void StartServer()
        {
            Console.WriteLine("Starting YCMD server...");
            //Generate the secret key.
            byte[] secretKey = new byte[16];
            LibRpws.LibRpwsCore.rand.NextBytes(secretKey);
            YcmdController.secret_key = secretKey;
            //Create the config file.
            string conf = File.ReadAllText(Program.config.media_dir + "WebSockets/ycmd/DefaultYcmdSettings.json");
            conf = conf.Replace("%KEY%", Convert.ToBase64String(secretKey));
            //Save to a temporary directory.
            string tempFile = Program.config.temp_files + "ycmd_conf.json";
            File.WriteAllText(tempFile, conf);
            //Run Python and execute this file.
            ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "/bin/bash", Arguments = "python3 "+Program.config.ycmd_binary+ "  --options_file "+tempFile+" --port "+YcmdController.YCMD_PORT, };
            p = new Process() { StartInfo = startInfo, };
            p.Start();
            Console.WriteLine("YCMD server started.");
        }

        public static void KillServer()
        {
            Console.WriteLine("Killing YCMD...");
            p.Kill();
            Console.WriteLine("YCMD Killed.");
        }
    }
}
