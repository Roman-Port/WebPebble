using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WebPebble.WebSockets.Entities;

namespace WebPebble.WebSockets
{
    public partial class CloudPebbleDevice
    {
        //This file manages the "endpoints" as old CloudPebble called them.
        public delegate AfterInterruptAction EndpointMsgCallback(PebbleProtocolMessage data);
        public void SendEndpointMsg(PebbleEndpointType type, byte[] data, EndpointMsgCallback callback)
        {
            byte[] buf = new byte[data.Length + 5];
            //The first byte is the message type, which is PEBBLE_PROTOCOL_PHONE_TO_WATCH
            buf[0] = (byte)CloudPebbleCode.PEBBLE_PROTOCOL_PHONE_TO_WATCH;
            byte[] length = FromShort((short)data.Length);
            byte[] typeBytes = FromShort((short)type);
            //Next two bytes are length of the message.
            length.CopyTo(buf, 1);
            //Next two bytes are the type.
            typeBytes.CopyTo(buf, 3);
            //The remainder is the message itself
            data.CopyTo(buf, 5);
            //Send this message and wait for a reply.
            SendDataGetReplyType(buf, CloudPebbleCode.PEBBLE_PROTOCOL_WATCH_TO_PHONE, (byte[] reply, object user) =>
            {
                //Read this in and see if the reply type matches the one we requested.
                using(MemoryStream ms = new MemoryStream(reply))
                {
                    ms.Position += 1;
                    PebbleProtocolMessage msg = new PebbleProtocolMessage(ms, CloudPebbleCode.PEBBLE_PROTOCOL_WATCH_TO_PHONE);
                    if(msg.id == type)
                    {
                        //This is what we wanted. Prevent the default and call callback.
                        return callback(msg);
                    } else
                    {
                        //Not what we wanted.
                        return AfterInterruptAction.NoPreventDefault_ContinueInterrupt;
                    }
                }
            }, null);
        }

        public byte[] FromShort(short data)
        {
            byte[] buf = BitConverter.GetBytes(data);
            Array.Reverse(buf); //Pebble is Big-Endian
            return buf;
        }

        public PebbleChunkedScreenshot active_receive_img;

        public void GetScreenshot(OnImageDone callback)
        {
            //Screenshots are sent in chunks. The first chunk has a header of 12 bytes.
            SendEndpointMsg(PebbleEndpointType.SCREENSHOT, new byte[] { 0x00 }, (PebbleProtocolMessage msg) =>
            {
                //Use the object instead. Create one if it doesn't exist.
                if (active_receive_img == null)
                    active_receive_img = new PebbleChunkedScreenshot(callback);
                return active_receive_img.OnGotData(msg);
            });
        }

        public byte[] GetScreenshotAndWait()
        {
            byte[] output_buffer = null;
            GetScreenshot((byte[] end_data) =>
            {
                //Reset the active image and return the output buffer.
                active_receive_img = null;
                output_buffer = end_data;
            });
            //Wait
            while (output_buffer == null) ;
            //Return now.
            return output_buffer;
        }
    }

    public enum PebbleEndpointType
    {
        TIME = (11),
        VERSIONS = (16),
        PHONE_VERSION = (17),
        SYSTEM_MESSAGE = (18),
        MUSIC_CONTROL = (32),
        PHONE_CONTROL = (33),
        APP_MESSAGE = (48),
        APP_CUSTOMIZE = (50),
        APP_RUN_STATE = (52),
        LOGS = (2000),
        PING = (2001),
        LOG_DUMP = (2002),
        RESET = (2003),
        APP_LOGS = (2006),
        SYS_REG = (5000),
        FCT_REG = (5001),
        APP_FETCH = (6001),
        PUT_BYTES = (48879),
        DATA_LOG = (6778),
        SCREENSHOT = (8000),
        FILE_INSTALL_MANAGER = (8181),
        GET_BYTES = (9000),
        AUDIO_STREAMING = (10000),
        APP_REORDER = (43981),
        BLOBDB_V1 = (45531),
        BLOBDB_V2 = (45787),
        TIMELINE_ACTIONS = (11440),
        VOICE_CONTROL = (11000),
        HEALTH_SYNC = (911),
    }
}
