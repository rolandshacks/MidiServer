using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace netmidi
{
    public class Midi
    {
        public const byte MIDIEventNoteOn = 0x90;
        public const byte MIDIEventNoteOff = 0x80;
        public const byte MIDIEventPolyphonPressure = 0xA0;
        public const byte MIDIEventController = 0xB0;
        public const byte MIDIEventProgChange = 0xC0;
        public const byte MIDIEventChannelPressure = 0xD0;
        public const byte MIDIEventPitchBend = 0xE0;
        public const byte MIDIEventSystemMessage = 0xF0;

        public const byte MIDICtrlBankSelectMSB = 0x00;
        public const byte MIDICtrlBankSelectLSB = 0x20;

        public const byte MIDICtrlModulation = 0x01;
        public const byte MIDICtrlVolume = 0x07;
        public const byte MIDICtrlPan = 0x0A;
        public const byte MIDICtrlSustain = 0x40;
        public const byte MIDICtrlReverb = 0x5B;
        public const byte MIDICtrlChorus = 0x5D;
        public const byte MIDICtrlAllNotesOff = 0x7B;

        public const byte MIDISystemTimingClock = 0xF8;
        public const byte MIDISystemActiveSensing = 0xFE;
        public const byte MIDISystemSongPositionPointer = 0xF2;
        public const byte MIDISystemSongSelect = 0xF3;
        public const byte MIDISystemStart = 0xFA;
        public const byte MIDISystemContinue = 0xFB;
        public const byte MIDISystemStop = 0xFC;

        private List<string> outputDeviceNames = new List<string>();

        private NativeMethods.MidiInProc midiInProc;
        private IntPtr midiInHandle;
        private IntPtr midiOutHandle;

        private IMidiListener midiListener = null;

        public Midi(IMidiListener midiListener)
        {
            this.midiListener = midiListener;

            midiInProc = new NativeMethods.MidiInProc(MidiProc);
            midiInHandle = IntPtr.Zero;
            midiOutHandle = IntPtr.Zero;
        }

        public static int InputCount
        {
            get { return NativeMethods.midiInGetNumDevs(); }
        }

        public static int OutputCount
        {
            get { return NativeMethods.midiOutGetNumDevs(); }
        }

        public void listOutputDevices()
        {
            int numberOfDevices = NativeMethods.midiOutGetNumDevs();

            outputDeviceNames.Clear();

            if (numberOfDevices > 0)
            {
                for (Int32 i = 0; i < numberOfDevices; i++)
                {
                    NativeMethods.MIDIOUTCAPS caps = new NativeMethods.MIDIOUTCAPS();
                    if (NativeMethods.midiOutGetDevCaps(i, ref caps,
                       (UInt32)Marshal.SizeOf(caps)) == NativeMethods.MMSYSERR_NOERROR)
                    {
                        string pname = caps.szPname;
                        Console.Out.WriteLine("Found output device #" + i + ": " + pname);

                        outputDeviceNames.Add(pname);
                    }
                }
            }
        }

        public bool Open(int inputDevice, int outputDevice)
        {
            if (NativeMethods.midiInOpen(
                out midiInHandle,
                (inputDevice != -1) ? inputDevice : 0,
                midiInProc,
                IntPtr.Zero,
                NativeMethods.CALLBACK_FUNCTION)
                    != NativeMethods.MMSYSERR_NOERROR)
            {
                midiInHandle = IntPtr.Zero;
                Console.Out.WriteLine("Could not open midi input device");
            }

            if (outputDevice == -1)
            {
                listOutputDevices();
            }

            if (NativeMethods.midiOutOpen(
                out midiOutHandle,
                (outputDevice != -1) ? outputDevice : NativeMethods.MIDI_MAPPER,
                null,
                IntPtr.Zero,
                NativeMethods.CALLBACK_NULL)
                    != NativeMethods.MMSYSERR_NOERROR)
            {
                midiOutHandle = IntPtr.Zero;
                Console.Out.WriteLine("Could not open midi output device");
            }
            else
            {
                if (outputDevice != -1)
                {
                    Console.Out.WriteLine("Using output device: #" + outputDevice + ": " + outputDeviceNames.ElementAt(outputDevice));
                }
                else
                {
                    Console.Out.WriteLine("Using default output device (MIDI Mapper)");
                }
            }

            return true;
        }

        public void Close()
        {
            if (IntPtr.Zero != midiInHandle)
            {
                NativeMethods.midiInClose(midiInHandle);
                midiInHandle = IntPtr.Zero;
            }

            if (IntPtr.Zero != midiOutHandle)
            {
                NativeMethods.midiOutClose(midiOutHandle);
                midiOutHandle = IntPtr.Zero;
            }

        }

        public bool isOpen()
        {
            return (IntPtr.Zero != midiInHandle);
        }

        public bool Start()
        {
            int status = NativeMethods.midiInStart(midiInHandle);
            if (NativeMethods.MMSYSERR_NOERROR != status) return false;

            return true;
        }

        public void Stop()
        {
            if (IntPtr.Zero != midiInHandle)
            {
                NativeMethods.midiInStop(midiInHandle);
            }
        }

        public bool Send(uint dwParam)
        {
            if (IntPtr.Zero != midiOutHandle)
            {
                int status = NativeMethods.midiOutShortMsg(midiOutHandle, dwParam);
                return (status == NativeMethods.MMSYSERR_NOERROR);
            }
            else
            {
                return false;
            }
        }

        private void MidiProc(IntPtr hMidiIn,
            int wMsg,
            IntPtr dwInstance,
            uint dwParam1,
            uint dwParam2)
        {
            // Receive messages here

            if (wMsg == NativeMethods.MIM_DATA)
            {
                if (null != midiListener)
                {
                    midiListener.onMidiData(dwParam1, dwParam2);
                }
            }
        }

        internal static class NativeMethods
        {
            // Taken from mmsystem.h

            // ERRORS

            internal const int MMSYSERR_BASE         = 0;
            internal const int MIDIERR_BASE          = 64;

            internal const int MMSYSERR_NOERROR      = 0;                    /* no error */
            internal const int MMSYSERR_ERROR        = (MMSYSERR_BASE + 1);  /* unspecified error */
            internal const int MMSYSERR_BADDEVICEID  = (MMSYSERR_BASE + 2);  /* device ID out of range */
            internal const int MMSYSERR_NOTENABLED   = (MMSYSERR_BASE + 3);  /* driver failed enable */
            internal const int MMSYSERR_ALLOCATED    = (MMSYSERR_BASE + 4);  /* device already allocated */
            internal const int MMSYSERR_INVALHANDLE  = (MMSYSERR_BASE + 5);  /* device handle is invalid */
            internal const int MMSYSERR_NODRIVER     = (MMSYSERR_BASE + 6);  /* no device driver present */
            internal const int MMSYSERR_NOMEM        = (MMSYSERR_BASE + 7);  /* memory allocation error */
            internal const int MMSYSERR_NOTSUPPORTED = (MMSYSERR_BASE + 8);  /* function isn't supported */
            internal const int MMSYSERR_BADERRNUM    = (MMSYSERR_BASE + 9);  /* error value out of range */
            internal const int MMSYSERR_INVALFLAG    = (MMSYSERR_BASE + 10); /* invalid flag passed */
            internal const int MMSYSERR_INVALPARAM   = (MMSYSERR_BASE + 11); /* invalid parameter passed */
            internal const int MMSYSERR_HANDLEBUSY   = (MMSYSERR_BASE + 12); /* handle being used */
                                                                             /* simultaneously on another */
                                                                             /* thread = (eg callback); */
            internal const int MMSYSERR_INVALIDALIAS = (MMSYSERR_BASE + 13); /* specified alias not found */
            internal const int MMSYSERR_BADDB        = (MMSYSERR_BASE + 14); /* bad registry database */
            internal const int MMSYSERR_KEYNOTFOUND  = (MMSYSERR_BASE + 15); /* registry key not found */
            internal const int MMSYSERR_READERROR    = (MMSYSERR_BASE + 16); /* registry read error */
            internal const int MMSYSERR_WRITEERROR   = (MMSYSERR_BASE + 17); /* registry write error */
            internal const int MMSYSERR_DELETEERROR  = (MMSYSERR_BASE + 18); /* registry delete error */
            internal const int MMSYSERR_VALNOTFOUND  = (MMSYSERR_BASE + 19); /* registry value not found */
            internal const int MMSYSERR_NODRIVERCB   = (MMSYSERR_BASE + 20); /* driver does not call DriverCallback */
            internal const int MMSYSERR_MOREDATA     = (MMSYSERR_BASE + 21); /* more data to be returned */
            internal const int MMSYSERR_LASTERROR    = (MMSYSERR_BASE + 21); /* last error in range */

            internal const int MIDIERR_UNPREPARED    = (MIDIERR_BASE + 0);   /* header not prepared */
            internal const int MIDIERR_STILLPLAYING  = (MIDIERR_BASE + 1);   /* still something playing */
            internal const int MIDIERR_NOMAP         = (MIDIERR_BASE + 2);   /* no configured instruments */
            internal const int MIDIERR_NOTREADY      = (MIDIERR_BASE + 3);   /* hardware is still busy */
            internal const int MIDIERR_NODEVICE      = (MIDIERR_BASE + 4);   /* port no longer connected */
            internal const int MIDIERR_INVALIDSETUP  = (MIDIERR_BASE + 5);   /* invalid MIF */
            internal const int MIDIERR_BADOPENMODE   = (MIDIERR_BASE + 6);   /* operation unsupported w/ open mode */
            internal const int MIDIERR_DONT_CONTINUE = (MIDIERR_BASE + 7);   /* thru device 'eating' a message */
            internal const int MIDIERR_LASTERROR     = (MIDIERR_BASE + 7);   /* last error in range */

            // OTHER CONSTS
            internal const int MAXPNAMELEN           = 32;                   /* max product name length (including NULL) */


            // DEVICES

            //internal const int MM_MIDI_MAPPER = 1;
            internal const int MIDI_MAPPER    = -1;


            // FLAGS

            internal const int CALLBACK_TYPEMASK   = 0x00070000;    /* callback type mask */
            internal const int CALLBACK_NULL       = 0x00000000;    /* no callback */
            internal const int CALLBACK_WINDOW     = 0x00010000;    /* dwCallback is a HWND */
            internal const int CALLBACK_TASK       = 0x00020000;    /* dwCallback is a HTASK */
            internal const int CALLBACK_FUNCTION   = 0x00030000;    /* dwCallback is a FARPROC */
            internal const int CALLBACK_THREAD     = CALLBACK_TASK;  /* thread ID replaces 16 bit task */
            internal const int CALLBACK_EVENT      = 0x00050000;    /* dwCallback is an EVENT Handle */

            public const int MIM_OPEN = 961;
            public const int MIM_CLOSE = 962;
            public const int MIM_DATA = 963;
            public const int MIM_LONGDATA = 964;
            public const int MIM_ERROR = 965;
            public const int MIM_LONGERROR = 966;
            public const int MOM_OPEN = 967;
            public const int MOM_CLOSE = 968;
            public const int MOM_DONE = 969;
            public const int MIM_MOREDATA = 972;

            internal delegate void MidiInProc(
                IntPtr hMidiIn,
                int wMsg,
                IntPtr dwInstance,
                uint dwParam1,
                uint dwParam2);

            internal delegate void MidiOutProc(
                IntPtr hMidiOut,
                int wMsg,
                IntPtr dwInstance,
                uint dwParam1,
                uint dwParam2);

            [StructLayout(LayoutKind.Sequential)]
            public struct MIDIOUTCAPS
            {
                public UInt16 wMid;
                public UInt16 wPid;
                public UInt32 vDriverVersion;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAXPNAMELEN)]
                public string szPname;
                public UInt16 wTechnology;
                public UInt16 wVoices;
                public UInt16 wNotes;
                public UInt16 wChannelMask;
                public UInt32 dwSupport;
            }

            [DllImport("winmm.dll")]
            internal static extern int midiInGetNumDevs();

            [DllImport("winmm.dll")]
            internal static extern int midiInClose(
                IntPtr hMidiIn);

            [DllImport("winmm.dll")]
            internal static extern int midiInOpen(
                out IntPtr lphMidiIn,
                int uDeviceID,
                MidiInProc dwCallback,
                IntPtr dwCallbackInstance,
                int dwFlags);

            [DllImport("winmm.dll")]
            internal static extern int midiInStart(
                IntPtr hMidiIn);

            [DllImport("winmm.dll")]
            internal static extern int midiInStop(
                IntPtr hMidiIn);

            [DllImport("winmm.dll")]
            internal static extern int midiOutGetNumDevs();

            [DllImport("winmm.dll")]
            internal static extern UInt32 midiOutGetDevCaps(
                Int32 uDeviceID,
                ref MIDIOUTCAPS lpMidiOutCaps, UInt32 cbMidiOutCaps);

            [DllImport("winmm.dll")]
            internal static extern int midiOutClose(
                IntPtr hMidiOut);

            [DllImport("winmm.dll")]
            internal static extern int midiOutOpen(
                out IntPtr lphMidiOut,
                int uDeviceID,
                MidiOutProc dwCallback,
                IntPtr dwCallbackInstance,
                int dwFlags);

            [DllImport("winmm.dll")]
            internal static extern int midiOutShortMsg(
                IntPtr hMidiOut,
                uint dwMsg);

            [DllImport("winmm.dll")]
            internal static extern int midiOutMessage(
                IntPtr hMidiOut,
                uint dwParam1,
                uint dwParam2);
        }
    }
}
