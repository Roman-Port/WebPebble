//I spent two hours on this file only to realize that I just wasn't giving Chrome the right info. crap.

/*using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace WebPebble.Services.Projects
{
    public static class TtfToWoff
    {
        private const bool IS_FILE_LITTLE_ENDIAN = true;
        
        public static byte[] ConvertFont(byte[] ttf)
        {
            //Convert this font to the more standard WOFF format.
            using (MemoryStream s = new MemoryStream())
            using (MemoryStream compressed = new MemoryStream())
            {

            }

            return null;
        }

        private static void WriteWoffHeader(Stream s, int metaOffset, int metaLength, int privOffset, int privLength)
        {
            //THIS SECTION HAS BEEN WRITTEN FROM THE SPEC AT https://www.w3.org/TR/2012/REC-WOFF-20121213/

            WriteBytes(s, new byte[] { 0x77, 0x4F, 0x46, 0x46 }); //Write constant signature.
            WriteBytes(s, new byte[] { 0x00, 0x01, 0x00, 0x00 }); //Flavor version. This shouldn't be needed.
            WriteUInt32(s, 0); //This is the length of this file in bytes. This will be written to later on when the file is finished.
            WriteUInt16(s, 1); //This is the number of enteries in the font table. There is only one.
            WriteUInt16(s, 0); //This is unused in the spec.
            WriteUInt32(s, 0); //This is the space required to unpack this file. We don't know this yet, so it'll be set later.
            WriteUInt16(s, 0); //This is the version tag. This doesn't matter.
            WriteUInt16(s, 0); //This is the version tag. This doesn't matter.
            WriteUInt32(s, (UInt32)metaOffset);
            WriteUInt32(s, (UInt32)metaLength);
            WriteUInt32(s, (UInt32)privOffset);
            WriteUInt32(s, (UInt32)privLength);
        }

        private static void WriteTableDir(Stream s, byte[] uncompressedData, Stream compressedOutput, int offset)
        {
            //First, compress the TTF data.
            using (GZipStream gzip = new GZipStream(compressedOutput, CompressionMode.Compress, true))
            {
                gzip.Write(uncompressedData, 0, uncompressedData.Length);
            }
            compressedOutput.Position = 0;
            //Write data.
            WriteBytes(s, new byte[] { 0x54, 0x46, 0x49, 0x4C }); //Write the TTF identifier "TFIL"
            WriteUInt32(s, (UInt32)offset); //Offset of the data.
            WriteUInt32(s, (UInt32)compressedOutput.Length); //Length of the compressed data.
            WriteUInt32(s, (UInt32)uncompressedData.Length); //Length of original data.
        }

        //Helpers
        private static void WriteBytes(Stream s, byte[] b)
        {
            s.Write(b, 0, b.Length);
        }

        private static void WriteUInt32(Stream s, UInt32 i)
        {
            byte[] b = BitConverter.GetBytes(i);
            if (IS_FILE_LITTLE_ENDIAN != BitConverter.IsLittleEndian)
                Array.Reverse(b);
            WriteBytes(s, b);
        }

        private static void WriteUInt16(Stream s, UInt16 i)
        {
            byte[] b = BitConverter.GetBytes(i);
            if (IS_FILE_LITTLE_ENDIAN != BitConverter.IsLittleEndian)
                Array.Reverse(b);
            WriteBytes(s, b);
        }
    }
}*/


