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
        public byte[] bmp_buffer;
        public int buffer_pos = 0;

        public int version;
        public int width;
        public int height;

        public AfterInterruptAction OnGotData(PebbleProtocolMessage msg)
        {
            Console.WriteLine("Got screenshot chunk of size " + msg.data.Length);
            if(chunkCount == 0)
            {
                //This is a header chunk. Open it.
                using(MemoryStream ms = new MemoryStream(msg.data))
                {
                    ms.Position += 1;
                    version = ReadInt32(ms);
                    width = ReadInt32(ms);
                    height = ReadInt32(ms);
                    //Create the buffer based on the width and height.
                    bmp_buffer = new byte[width * height];
                    Console.WriteLine("(Debug) Created image of size " + width.ToString() + "x" + height.ToString());
                    buffer_pos = 0;
                    //Copy the remainder of this content to the buffer.
                    byte[] buf = new byte[ms.Length - ms.Position];
                    ms.Read(buf, 0, buf.Length);
                    msg.data.CopyTo(buf, buffer_pos);
                    buffer_pos += buf.Length;
                }
            } else {
                //Copy this content to the buffer.
                msg.data.CopyTo(bmp_buffer, buffer_pos);
                buffer_pos += msg.data.Length;
                Console.WriteLine("(Debug) Buffer completeness: " + buffer_pos.ToString() + "/" + bmp_buffer.Length.ToString());
            }
            chunkCount++;
            return AfterInterruptAction.PreventDefault_ContinueInterrupt;
        }

        private int ReadInt32(MemoryStream ms)
        {
            byte[] buf = new byte[4];
            ms.Read(buf, 0, 4);
            Array.Reverse(buf);
            return BitConverter.ToInt32(buf,0);
        }
    }
}
