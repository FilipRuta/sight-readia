using System;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using MidiPlayerTK;
using UnityEngine;

namespace OutputDeviceHandling
{
    public class ComputerOutputDevice : MonoBehaviour, IOutputDevice
    {
        private MidiStreamPlayer _midiStreamPlayer;
    
        public event EventHandler<MidiEventSentEventArgs> EventSent;

        public void Start()
        {
            _midiStreamPlayer = GetComponent<MidiStreamPlayer>();
        }

        public void PrepareForEventsSending()
        {
        }

        /// <summary>
        /// Converts DryWetMIDI events to MIDI Player Toolkit events for playback.
        /// </summary>
        public void SendEvent(MidiEvent midiEvent)
        {
            switch (midiEvent)
            {
                case NoteOnEvent noteOnEvent:
                {
                    int noteNumber = noteOnEvent.NoteNumber;
                    int velocity = noteOnEvent.Velocity;
                    int channel = noteOnEvent.Channel;
        
                    // If velocity is > 0, it's a note on; if velocity is 0, it's actually a note off
                    if (velocity > 0)
                    {
                        Debug.Log($"Note On: {noteNumber}, Velocity: {velocity}, Channel: {channel}");
                        _midiStreamPlayer.MPTK_PlayEvent(new MPTKEvent()
                        {
                            Command = MPTKCommand.NoteOn,
                            Value = noteNumber,
                            Channel = channel,
                            // Velocity = velocity,  // Don't use original Velocity, note playback is too silent. Defaults to max
                        });
                    }
                    else
                    {
                        Debug.Log($"Note Off (via Note On with zero velocity): {noteNumber}, Channel: {channel}");
                        _midiStreamPlayer.MPTK_PlayEvent(new MPTKEvent()
                        {
                            Command = MPTKCommand.NoteOff,
                            Value = noteNumber,
                            Channel = channel,
                            Velocity = velocity,
                        });
                    }

                    break;
                }
                case NoteOffEvent noteOffEvent:
                {
                    int noteNumber = noteOffEvent.NoteNumber;
                    int channel = noteOffEvent.Channel;
                    int velocity = noteOffEvent.Velocity;

                    Debug.Log($"Note Off: {noteNumber}, Channel: {channel}");
                    _midiStreamPlayer.MPTK_PlayEvent(new MPTKEvent()
                    {
                        Command = MPTKCommand.NoteOff,
                        Value = noteNumber,
                        Channel = channel,
                        Velocity = velocity,
                    });
                    break;
                }
            }
        }
    
        public void Dispose()
        {
        }
    }
}