using System;
using System.Collections.Generic;
using System.Linq;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using UnityEngine;

namespace InputControllers
{
    public class MidiInputController: MonoBehaviour, IInputController
    {
        private HashSet<int> _notesOn = new HashSet<int>();
        private InputDevice _inputDevice;

        /// <inheritdoc/>
        public string Name { get; private set; }

        /// <inheritdoc/>
        public IEnumerable<int> NotesBeingPlayed()
        {
            return _notesOn;
        }
    
        /// <inheritdoc/>
        public IInputDevice GetInputDevice()
        {
            return _inputDevice;
        }
        
        /// <inheritdoc/>
        public bool AnyNotesPlayed => _notesOn.Any();

        /// <summary>
        /// Initializes the MIDI input device and starts listening for events.
        /// </summary>
        public void Initialize(string deviceName)
        {
            Name = deviceName;
            try
            {
                _inputDevice = InputDevice.GetByName(Name);
                _inputDevice.EventReceived += OnMidiEventReceived;
                _inputDevice.StartEventsListening();
                Debug.Log($"Input device: {_inputDevice.Name} initialized.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"Input device was not initialized.");
                _inputDevice.StopEventsListening();
                _inputDevice.EventReceived -= OnMidiEventReceived;
                _inputDevice = null; 
                throw;
            }
        }

        /// <inheritdoc/>
        public bool IsInDeviceRange(int midiCode)
        {
            return midiCode >= Constants.FullMidiKeyboardNoteMidiRange.min &&
                   midiCode <= Constants.FullMidiKeyboardNoteMidiRange.max;
        }

        /// <summary>
        /// Handles incoming MIDI events, tracking note on/off states.
        /// </summary>
        private void OnMidiEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            var midiEvent = e.Event;

            switch (midiEvent)
            {
                case NoteOnEvent noteOnEvent:
                {
                    _notesOn.Add(noteOnEvent.NoteNumber);
                    Debug.Log($"Note On: {noteOnEvent.NoteNumber} (Velocity: {noteOnEvent.Velocity})");
                    break;
                }
                case NoteOffEvent noteOffEvent:
                {
                    _notesOn.Remove(noteOffEvent.NoteNumber);
                    Debug.Log($"Note Off: {noteOffEvent.NoteNumber}");
                    break;
                }
                default:
                    Debug.Log($"Other MIDI Event: {midiEvent}");
                    break;
            }
        }
    
        /// <summary>
        /// Cleanup method to release MIDI input device resources.
        /// </summary>
        private void OnDestroy()
        {
            Debug.Log("Releasing playback and device...");
        
            if (_inputDevice != null)
            {
                _inputDevice.StopEventsListening();
                _inputDevice.EventReceived -= OnMidiEventReceived;
                _inputDevice.Dispose();
                Debug.Log($"Input device({_inputDevice.Name}) released");
            }
        }
    }
}