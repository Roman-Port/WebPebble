using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static WebPebble.WebSockets.CloudPebbleDevice;

namespace WebPebble.WebSockets.Entities
{
    public class PebbleChunkedScreenshot
    {
        public int chunkCount = 0;

        public AfterInterruptAction OnGotData(PebbleProtocolMessage msg)
        {
            Console.WriteLine("Got screenshot chunk of size " + msg.data.Length);
            File.WriteAllBytes("/home/roman/tmp/chunk_" + chunkCount.ToString() + ".bin",msg.data);
            chunkCount++;
            return AfterInterruptAction.PreventDefault_ContinueInterrupt;
        }
    }
}
