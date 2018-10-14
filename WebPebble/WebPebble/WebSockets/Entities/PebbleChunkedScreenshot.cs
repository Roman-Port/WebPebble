
using FastBitmapLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
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
                    buf.CopyTo(bmp_buffer, buffer_pos);
                    buffer_pos += buf.Length;
                }
            } else {
                //Copy this content to the buffer.
                msg.data.CopyTo(bmp_buffer, buffer_pos);
                buffer_pos += msg.data.Length;
                Console.WriteLine("(Debug) Buffer completeness: " + buffer_pos.ToString() + "/" + bmp_buffer.Length.ToString());
            }
            //If the buffer is complete, convert this into an actual image.
            if(buffer_pos == bmp_buffer.Length)
            {
                Console.WriteLine("Buffer full. Creating image!");
                FinalizeImage();
            }
            chunkCount++;
            return AfterInterruptAction.PreventDefault_ContinueInterrupt;
        }



        public void FinalizeImage()
        {
            //Called after the buffer is complete.
            bool oneBppMode = version == 1;
            BitArray ba = new BitArray(bmp_buffer);

            FastBitmap f = new FastBitmap(width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int pos = (x * width) + y;
                    if (false)
                    {
                        //Read the bit instead of the byte.
                        Console.WriteLine(x);
                        bool on = ba[pos];

                        if (on)
                            f.SetPixel(x, y, new FastColor(0,0,0));
                        else
                            f.SetPixel(x, y, new FastColor(255, 255, 255));
                    }
                    else
                    {
                        //This is going to be slow....
                        byte color_id = bmp_buffer[pos];
                        //Convert this to an RGB color.
                        PebbleColorMap value = (PebbleColorMap)color_id;
                        string hex_code = value.ToString().Substring(1);
                        byte[] data = StringToByteArray(hex_code);
                        f.SetPixel(x, y, new FastColor(data[0], data[1], data[2]));
                    }
                }
            }

            f.Save("/home/roman/img.bmp");

        }

        private int ReadInt32(MemoryStream ms)
        {
            byte[] buf = new byte[4];
            ms.Read(buf, 0, 4);
            Array.Reverse(buf);
            return BitConverter.ToInt32(buf,0);
        }

        //thanks to https://stackoverflow.com/questions/321370/how-can-i-convert-a-hex-string-to-a-byte-array for helping me at 2:30 AM
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        enum PebbleColorMap
        {
            A000000,
            A001e41,
            A004387,
            A0068ca,
            A2b4a2c,
            A27514f,
            A16638d,
            A007dce,
            A5e9860,
            A5c9b72,
            A57a5a2,
            A4cb4db,
            A8ee391,
            A8ee69e,
            A8aebc0,
            A84f5f1,
            A4a161b,
            A482748,
            A40488a,
            A2f6bcc,
            A564e36,
            A545454,
            A4f6790,
            A4180d0,
            A759a64,
            A759d76,
            A71a6a4,
            A69b5dd,
            A9ee594,
            A9de7a0,
            A9becc2,
            A95f6f2,
            A99353f,
            A983e5a,
            A955694,
            A8f74d2,
            A9d5b4d,
            A9d6064,
            A9a7099,
            A9587d5,
            Aafa072,
            Aaea382,
            Aababab,
            Aa7bae2,
            Ac9e89d,
            Ac9eaa7,
            Ac7f0c8,
            Ac3f9f7,
            Ae35462,
            Ae25874,
            Ae16aa3,
            Ade83dc,
            Ae66e6b,
            Ae6727c,
            Ae37fa7,
            Ae194df,
            Af1aa86,
            Af1ad93,
            Aefb5b8,
            Aecc3eb,
            Affeeab,
            Afff1b5,
            Afff6d3,
            Affffff,
        }
    }
}
