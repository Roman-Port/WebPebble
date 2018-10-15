using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
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

    public delegate void OnImageDone(byte[] data);
    public class PebbleChunkedScreenshot
    {
        public int chunkCount = 0;
        public byte[] bmp_buffer;
        public int buffer_pos = 0;

        public int version;
        public int width;
        public int height;

        public int target_buffer_size;

        public int bits_per_pixel;

        public const bool debug = true;

        public OnImageDone callback;

        public PebbleChunkedScreenshot(OnImageDone _callback)
        {
            callback = _callback;
        }

        private void Log(string msg)
        {
            if (debug)
                Console.WriteLine(msg);
        }

        public AfterInterruptAction OnGotData(PebbleProtocolMessage msg)
        {
            Log("Got screenshot chunk of size " + msg.data.Length);
            if(chunkCount == 0)
            {
                //This is a header chunk. Open it.
                using(MemoryStream ms = new MemoryStream(msg.data))
                {
                    ms.Position += 1;
                    version = ReadInt32(ms);
                    bits_per_pixel = 8;
                    if (version == 1)
                        bits_per_pixel = 1;
                    
                    width = ReadInt32(ms);
                    height = ReadInt32(ms);
                    target_buffer_size = (width * height);
                    if (version == 1)
                        target_buffer_size = target_buffer_size / 8; //Version 1 images are 1bpp
                    //Create the buffer based on the width and height.
                    bmp_buffer = new byte[width * height];
                    Log("(Debug) Created image of size " + width.ToString() + "x" + height.ToString());
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
                Log("(Debug) Buffer completeness: " + buffer_pos.ToString() + "/" + target_buffer_size.ToString());
            }
            //If the buffer is complete, convert this into an actual image.
            if(buffer_pos == target_buffer_size)
            {
                Log("Buffer full. Creating image!");
                FinalizeImage();
            }
            chunkCount++;
            return AfterInterruptAction.PreventDefault_ContinueInterrupt;
        }



        public void FinalizeImage()
        {
            //Called after the buffer is complete.
            BitArray ba = new BitArray(bmp_buffer);
            byte[] output_buffer;

            //Decode the image data.
            int[] expanded_data;

            if (bits_per_pixel == 8)
                expanded_data = decode_image_8bit_corrected();
            else
                expanded_data = decode_image_1bit();

            //The expanded data now consists of the colors channels. Place them in the image.
            using (Image<Rgba32> image = new Image<Rgba32>(width,height))
            {
                for(int pos = 0; pos < expanded_data.Length; pos+=4)
                {
                    var color = new Rgba32((byte)expanded_data[pos], (byte)expanded_data[pos + 1], (byte)expanded_data[pos + 2]);
                    int pixelPos = pos / 4;
                    int y = pixelPos / width;
                    int x = pixelPos % width;
                    Log(x.ToString() + " " + y.ToString()+"_"+pos.ToString());
                    image[x, y] = color;
                }
                //Save the image to an array.
                using (MemoryStream ms = new MemoryStream()) {
                    image.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                    //Copy to buffer.
                    output_buffer = new byte[ms.Length];
                    ms.Position = 0;
                    ms.Read(output_buffer, 0, output_buffer.Length);
                }
            }

            //Call the final callback.
            callback(output_buffer);
        }

        private int[] decode_image_8bit_corrected()
        {
            int[] expanded_data = new int[bmp_buffer.Length * 4];

            for (var i = 0; i < bmp_buffer.Length; i+=1)
            {
                var pixel = bmp_buffer[i];
                var pos = i * 4;
                var corrected = PebbleColorMap[pixel & 63];
                expanded_data[pos + 0] = (corrected >> 16) & 0xff;
                expanded_data[pos + 1] = (corrected >> 8) & 0xff;
                expanded_data[pos + 2] = (corrected >> 0) & 0xff;
                expanded_data[pos + 3] = 255; // always fully opaque.
            }

            return expanded_data;
        }

        private int[] decode_image_1bit()
        {
            Console.WriteLine("1");
            int[] expanded_data = new int[width * height * 4];
            Console.WriteLine("2");
            for (var i = 0; i < bmp_buffer.Length; ++i)
            {
                Console.WriteLine("3");
                for (var j = 0; j < 8; ++j)
                {
                    Console.WriteLine("4");
                    var pixel = (bmp_buffer[i] >> j) & 1;
                    var colour = pixel * 255;
                    var pos = ((i * 8) + j) * 4;
                    expanded_data[pos + 0] = colour;
                    expanded_data[pos + 1] = colour;
                    expanded_data[pos + 2] = colour;
                    expanded_data[pos + 3] = 255; // always fully opaque.
                }
            }
            return expanded_data;
        }

        private int ReadInt32(MemoryStream ms)
        {
            byte[] buf = new byte[4];
            ms.Read(buf, 0, 4);
            Array.Reverse(buf);
            return BitConverter.ToInt32(buf,0);
        }


        public static int[] PebbleColorMap = new int[]
        {
            0x000000,
            0x001e41,
            0x004387,
            0x0068ca,
            0x2b4a2c,
            0x27514f,
            0x16638d,
            0x007dce,
            0x5e9860,
            0x5c9b72,
            0x57a5a2,
            0x4cb4db,
            0x8ee391,
            0x8ee69e,
            0x8aebc0,
            0x84f5f1,
            0x4a161b,
            0x482748,
            0x40488a,
            0x2f6bcc,
            0x564e36,
            0x545454,
            0x4f6790,
            0x4180d0,
            0x759a64,
            0x759d76,
            0x71a6a4,
            0x69b5dd,
            0x9ee594,
            0x9de7a0,
            0x9becc2,
            0x95f6f2,
            0x99353f,
            0x983e5a,
            0x955694,
            0x8f74d2,
            0x9d5b4d,
            0x9d6064,
            0x9a7099,
            0x9587d5,
            0xafa072,
            0xaea382,
            0xababab,
            0xa7bae2,
            0xc9e89d,
            0xc9eaa7,
            0xc7f0c8,
            0xc3f9f7,
            0xe35462,
            0xe25874,
            0xe16aa3,
            0xde83dc,
            0xe66e6b,
            0xe6727c,
            0xe37fa7,
            0xe194df,
            0xf1aa86,
            0xf1ad93,
            0xefb5b8,
            0xecc3eb,
            0xffeeab,
            0xfff1b5,
            0xfff6d3,
            0xffffff
        };

        enum PebbleColorMapEnum
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
