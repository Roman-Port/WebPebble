using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace WebPebble.WebSockets.ycmd
{
    public class YcmdProcess
    {
        public static Dictionary<YcmdProcesses, YcmdProcess> process_dict = new Dictionary<YcmdProcesses, YcmdProcess>();

        public Process p;
        public YcmdProcesses name;
        public int port;
        public string extra_config_path;

        public byte[] secret_key;

        public static YcmdProcess StartServer(YcmdProcesses name, string extra_config)
        {
            YcmdProcess pp = new YcmdProcess();
            pp.name = name;
            pp.port = (int)name;
            pp.extra_config_path = Program.config.media_dir + "WebSockets/ycmd/YcmdConfig/" + extra_config;

            Console.WriteLine("Starting YCMD server '"+name.ToString()+"'...");
            //Generate the secret key.
            byte[] secretKey = new byte[16];
            LibRpws.LibRpwsCore.rand.NextBytes(secretKey);
            pp.secret_key = secretKey;
            //Create the config file.
            string conf = File.ReadAllText(Program.config.media_dir + "WebSockets/ycmd/DefaultYcmdSettings.json");
            conf = conf.Replace("%KEY%", Convert.ToBase64String(secretKey));
            conf = conf.Replace("%CONF%", pp.extra_config_path);
            Console.WriteLine(pp.extra_config_path);
            //Save to a temporary directory.
            string tempFile = Program.config.temp_files + "ycmd_temp_config_" +name.ToString() + ".json";
            File.WriteAllText(tempFile, conf);
            //Run Python and execute this file.
            ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "/usr/bin/python3", Arguments = Program.config.ycmd_binary+ "  --options_file "+tempFile+" --port "+pp.port, };
            pp.p = new Process() { StartInfo = startInfo, };
            pp.p.Start();

            Console.WriteLine("YCMD server '"+name.ToString()+"' started.");
            //Add process
            process_dict.Add(name, pp);
            return GetServer(name);
        }

        public static YcmdProcess GetServer(YcmdProcesses name)
        {
            return process_dict[name];
        }

        public static void KillAll()
        {
            foreach(var o in process_dict.Values)
            {
                o.KillServer();
            }
        }

        public void KillServer()
        {
            p.Kill();
            Console.WriteLine("YCMD server '" + name + "' Killed.");
        }
    }

    public enum YcmdProcesses
    {
        Sdk2Aplite =    43585,
        Sdk3Aplite =    43586,
        Sdk3Basalt =    43587,
        Sdk3Chalk  =    43588
    }
}
