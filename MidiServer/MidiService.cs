using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using netplug;
using netserver;
using netmidi;

namespace MidiServer
{
    public class MidiService : IService, IDispatch, IMidiListener
    {
        private const int MAX_QUEUE_SIZE = 1024;
        private const int MAX_RESPONSE_SIZE = 128;

        private Midi midi;

        private ConcurrentQueue<uint> queue = new ConcurrentQueue<uint>();
        private uint[] outputBuffer;

        public MidiService()
        {
            outputBuffer = new uint[MAX_RESPONSE_SIZE + 1];
        }

        public void start()
        {
            IConfiguration configuration = ServiceManager.get<IConfiguration>(netplug.Services.Configuration);

            string outputDevice = configuration.getProperty("arg0");
            int outputDeviceId = (outputDevice.Length > 0) ? int.Parse(outputDevice) : -1;

            if (null == midi)
            {
                midi = new Midi(this);
                midi.Open(-1, outputDeviceId);
                midi.Start();
            }
        }

        public void stop()
        {
            if (null != midi)
            {
                midi.Stop();
                midi.Close();
                midi = null;
            }
        }

        public void onMidiData(uint data1, uint data2)
        {
            byte byte0 = (byte)((data1 & 0x000000ff));
            if (byte0 == Midi.MIDISystemActiveSensing ||
                byte0 == Midi.MIDISystemTimingClock)
            {
                // filter
                return;
            }

            if (queue.Count < MAX_QUEUE_SIZE)
            {
                queue.Enqueue(data1);
            }
        }

        public void clearQueue()
        {
            uint data;
            while (queue.TryDequeue(out data)) ;
        }

        public bool getStatus()
        {
            return midi.isOpen();
        }

        private uint controllerValue = 0;
        public uint[] getQueuedElements(out int elementCount)
        {
            if (!midi.isOpen())
            {
                onMidiData(0x000007B0 | (controllerValue << 16), 0x0);
                controllerValue = (controllerValue + 1) % 128;
            }

            int idx = 0;
            uint data;
            while (idx < MAX_RESPONSE_SIZE && queue.TryDequeue(out data))
            {
                /*
                byte byte3 = (byte)((data & 0xff000000) >> 24);
                byte byte2 = (byte)((data & 0x00ff0000) >> 16);
                byte byte1 = (byte)((data & 0x0000ff00) >> 8);
                byte byte0 = (byte)((data & 0x000000ff));

                Console.Out.WriteLine("MIDI DATA: 0x" + byte0.ToString("x") + " 0x" + byte1.ToString("x") + " 0x" + byte2.ToString("x") + " 0x" + byte3.ToString("x"));
                */

                outputBuffer[idx++] = data;
            }

            elementCount = idx;
            outputBuffer[idx] = 0x0; // End-Of-Data

            return outputBuffer;
        }

        public Object dispatch(string command, string args)
        {
            if (command == "clearData")
            {
                //clearQueue();
            }
            else if (command == "getStatus")
            {
                return (getStatus() ? "ok" : "error");
            }
            else if (command == "send") 
            {
                if (args.Length < 8)
                {
                    return "invalid args";
                }

                int idx = 0;
                byte byte0 = Convert.ToByte(args.Substring(idx, 2), 16); idx += 2;
                byte byte1 = Convert.ToByte(args.Substring(idx, 2), 16); idx += 2;
                byte byte2 = Convert.ToByte(args.Substring(idx, 2), 16); idx += 2;
                byte byte3 = Convert.ToByte(args.Substring(idx, 2), 16); idx += 2;

                uint msg = ((uint)byte3 << 24) | ((uint)byte2 << 16) | ((uint)byte1 << 8) | (uint)byte0;

                midi.Send(msg);
            }
            else if (command == "getData")
            {

                int elementCount;
                uint[] elements = getQueuedElements(out elementCount);

                if (elementCount < 1)
                {
                    return "";
                }

                /*
                int byteLength = elementCount * 4;
                byte[] buffer = new byte[byteLength];
                Buffer.BlockCopy(elements, 0, buffer, 0, byteLength);
                return buffer;
                */

                StringBuilder sb = new StringBuilder(elementCount * 8);
                for (int i = 0; i < elementCount; i++)
                {
                    uint data = elements[i];
                    sb.AppendFormat("{0,8:X8}", data);
                }

                string midiData = sb.ToString();

                //Console.Out.WriteLine("MIDI DATA: " + midiData);


                return midiData;
            }
            else
            {
                return "unsupported operation: " + command;
            }

            return "ok";
        }

    }
}
