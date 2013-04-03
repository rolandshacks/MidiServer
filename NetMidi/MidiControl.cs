using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netmidi
{
    class MidiControl : IMidiListener
    {
        private int lastBankSelectMSB = -1;
        private int lastBankSelectLSB = -1;

        public MidiControl()
        {
        }

        public void midiNoteOn(int channel, int key, float velocity)
        {
            Console.Out.WriteLine("Note On: " + channel + " / " + key + " (" + velocity.ToString("0.000") + ")");
        }

        public void midiNoteOff(int channel, int key)
        {
            Console.Out.WriteLine("Note Off: " + channel + " / " + key);
        }

        public void midiPolyphonPressure(int channel, int key, float velocity)
        {
            Console.Out.WriteLine("Polyphon pressure: " + channel + " / " + key + " (" + velocity.ToString("0.000") + ")");
        }

        public void midiProgramChange(int channel, int program)
        {
            if (lastBankSelectMSB != -1)
            {
                midiBankSelect(channel, lastBankSelectMSB, (lastBankSelectLSB != -1) ? lastBankSelectLSB : 0, program);
                lastBankSelectMSB = lastBankSelectLSB = -1;
            }
            else
            {
                Console.Out.WriteLine("Program change: " + channel + " / " + program);
            }
        }

        public void midiPitchBend(int channel, float value)
        {
            Console.Out.WriteLine("Pitch bend: " + channel + " / " + value.ToString("0.000"));
        }

        public void midiControlChange(int channel, int controller, int value)
        {
            Console.Out.WriteLine("Control change: " + channel + " / " + controller.ToString("X") + " / " + value);
        }

        public void midiBankSelect(int channel, int bankMSB, int bankLSB, int program)
        {
            Console.Out.WriteLine("Program change: " + channel + " / " + bankMSB + " / " + bankLSB + " / " + program);
        }

        public void midiChannelPressure(int channel, float value)
        {
            Console.Out.WriteLine("Channel pressure: " + channel + " / " + value.ToString("0.000"));
        }

        public void midiSystemClock()
        {
            // timing sync 24 ppq (pulse per quarter note)
        }

        public void midiSystemActiveSensing()
        {
            // alive message each 300 milliseconds
        }

        public void midiSystemStart()
        {
            Console.Out.WriteLine("SYSTEM Start");
        }

        public void midiSystemContinue()
        {
            Console.Out.WriteLine("SYSTEM Continue");
        }

        public void midiSystemStop()
        {
            Console.Out.WriteLine("SYSTEM Stop");
        }

        public void midiSystemSongPointer(int songPosition)
        {
            Console.Out.WriteLine("SYSTEM Song Position Pointer: " + songPosition);
        }

        public void midiSystemSongSelect(int songNumber)
        {
            Console.Out.WriteLine("SYSTEM Song Select: " + songNumber);
        }

        public void onMidiData(uint data1, uint data2)
        {
            byte byte3 = (byte)((data1 & 0xff000000) >> 24);
            byte byte2 = (byte)((data1 & 0x00ff0000) >> 16);
            byte byte1 = (byte)((data1 & 0x0000ff00) >> 8);
            byte byte0 = (byte)((data1 & 0x000000ff));

            byte eventId = byte0;

            if (0x0 == eventId) // non-channel messages
            {
                lastBankSelectMSB = lastBankSelectLSB = -1;
                return;
            }

            if (eventId >= 0xF0)
            {
                lastBankSelectMSB = lastBankSelectLSB = -1;

                // system data

                //midiSystemData(uint data1, uint data2);
                if (Midi.MIDISystemTimingClock == eventId)
                {
                    midiSystemClock();
                }
                else if (Midi.MIDISystemActiveSensing == eventId)
                {
                    midiSystemActiveSensing();
                }
                else if (Midi.MIDISystemStart == eventId)
                {
                    midiSystemStart();
                }
                else if (Midi.MIDISystemContinue == eventId)
                {
                    midiSystemContinue();
                }
                else if (Midi.MIDISystemStop == eventId)
                {
                    midiSystemStop();
                }
                else if (Midi.MIDISystemSongPositionPointer == eventId)
                {
                    int songPosition = (int)byte1 + (int)byte2 * 128;
                    midiSystemSongPointer(songPosition);
                }
                else if (Midi.MIDISystemSongSelect == eventId)
                {
                    int songNumber = (int)byte1;
                    midiSystemSongSelect(songNumber);
                }
                else
                {
                    Console.Out.WriteLine("SYSDATA: 0x" + byte0.ToString("x") + " 0x" + byte1.ToString("x") + " 0x" + byte2.ToString("x") + " 0x" + byte3.ToString("x"));
                }

                return;
            }

            int message = (eventId & 0xF0);
            int channel = (eventId & 0x0F);

            // if (channel != 0) return;    // just process MIDI channel 0

            if (message != Midi.MIDIEventController && message != Midi.MIDIEventProgChange)
            {
                lastBankSelectMSB = lastBankSelectLSB = -1;
            }

            switch (message)
            {
                case Midi.MIDIEventPolyphonPressure:
                    {
                        midiPolyphonPressure(channel, (int)byte1, (float)byte2 / 127.0f);
                        break;
                    }
                case Midi.MIDIEventNoteOn:
                    {
                        if (0 != byte2)
                        {
                            midiNoteOn(channel, (int)byte1, (float)byte2 / 127.0f);
                        }
                        else
                        {
                            midiNoteOff(channel, (int)byte1);
                        }

                        break;
                    }
                case Midi.MIDIEventNoteOff:
                    {
                        midiNoteOff(channel, (int)byte1);
                        break;
                    }
                case Midi.MIDIEventChannelPressure:
                    {
                        midiChannelPressure(channel, (float)byte1 / 127.0f);
                        break;
                    }
                case Midi.MIDIEventProgChange:
                    {
                        midiProgramChange(channel, (int)byte1);

                        lastBankSelectMSB = lastBankSelectLSB = -1;

                        break;
                    }
                case Midi.MIDIEventPitchBend:
                    {
                        int intValue = ((int)byte1) + ((int)byte2 - 0x40) * 128;
                        float value = (intValue >= 0) ? (float)intValue / 8191.0f : (float)intValue / 8192.0f;
                        midiPitchBend(channel, value);
                        break;
                    }
                case Midi.MIDIEventController:
                    {
                        int controller = (int)byte1;
                        int value = (int)byte2;

                        // filter bank select
                        if (Midi.MIDICtrlBankSelectMSB == controller)
                        {
                            lastBankSelectMSB = value;
                        }
                        else if (Midi.MIDICtrlBankSelectLSB == controller)
                        {
                            lastBankSelectLSB = value;
                        }
                        else
                        {
                            lastBankSelectMSB = lastBankSelectLSB = -1;

                            // standard control change
                            midiControlChange(channel, controller, value);
                        }
                        
                        break;
                    }
                default:
                    {
                        //Console.Out.WriteLine("MIDI DATA: 0x" + byte1.ToString("x") + " 0x" + byte2.ToString("x"));
                        Console.Out.WriteLine("MIDI DATA: 0x" + byte0.ToString("x") + " 0x" + byte1.ToString("x") + " 0x" + byte2.ToString("x") + " **0x" + byte3.ToString("x"));
                        break;
                    }
            }
        }
    }
}
