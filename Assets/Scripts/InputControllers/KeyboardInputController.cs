using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using SheetMusic;
using UnityEngine;

namespace InputControllers
{
    public class KeyboardInputController : MonoBehaviour, IInputController, IInputDevice
    {
        /// <summary>
        /// Event triggered when a MIDI event is received (note on/off).
        /// </summary>
        public event EventHandler<MidiEventReceivedEventArgs> EventReceived;

        /// <summary>
        /// The underlying input device (in this case, the keyboard).
        /// </summary>
        private IInputDevice _inputDevice; 

        /// <summary>
        /// Tracks the currently pressed notes using their MIDI note numbers.
        /// </summary>
        private HashSet<int> _notesOn = new HashSet<int>();

        /// <inheritdoc/>
        public bool AnyNotesPlayed => _notesOn.Any();

        /// <inheritdoc/>
        public string Name { get; private set; }

        /// <inheritdoc/>
        public void Initialize(string deviceName)
        {
            Name = deviceName;
            StartEventsListening();
        }
        
        /// <inheritdoc/>
        public bool IsInDeviceRange(int midiCode)
        {
            return midiCode >= Constants.PCKeyboardNoteMidiRange.min &&
                   midiCode <= Constants.PCKeyboardNoteMidiRange.max;
        }

        /// <summary>
        /// Starts listening for keyboard input events.
        /// </summary>
        public void StartEventsListening()
        {
            IsListeningForEvents = true;
        }

        /// <summary>
        /// Stops listening for keyboard input events.
        /// </summary>
        public void StopEventsListening()
        {
            IsListeningForEvents = false;
        }

        /// <summary>
        /// Indicates whether the controller is currently listening for input events.
        /// </summary>
        public bool IsListeningForEvents { get; private set; }
        
        /// <inheritdoc/>
        public IInputDevice GetInputDevice()
        {
            return this;
        }
        
        private readonly Dictionary<KeyCode, string> _inputMap = new()
        {
            // Lower row (Z-M) for natural notes C4-B4
            { KeyCode.Z, "C4" },
            { KeyCode.X, "D4" },
            { KeyCode.C, "E4" },
            { KeyCode.V, "F4" },
            { KeyCode.B, "G4" },
            { KeyCode.N, "A4" },
            { KeyCode.M, "B4" },
    
            // Row above (A-K) for sharps/flats
            { KeyCode.S, "C#4" },
            { KeyCode.D, "D#4" },
            { KeyCode.G, "F#4" },
            { KeyCode.H, "G#4" },
            { KeyCode.J, "A#4" },
    
            // Upper row (Q-P) for natural notes C5-E6
            { KeyCode.Q, "C5" },
            { KeyCode.W, "D5" },
            { KeyCode.E, "E5" },
            { KeyCode.R, "F5" },
            { KeyCode.T, "G5" },
            { KeyCode.Y, "A5" },
            { KeyCode.U, "B5" },
            { KeyCode.I, "C6" },
            { KeyCode.O, "D6" },
            { KeyCode.P, "E6" },
    
            // Row above (2-0) for sharps/flats
            { KeyCode.Alpha2, "C#5" },
            { KeyCode.Alpha3, "D#5" },
            { KeyCode.Alpha5, "F#5" },
            { KeyCode.Alpha6, "G#5" },
            { KeyCode.Alpha7, "A#5" },
            { KeyCode.Alpha9, "C#6" },
            { KeyCode.Alpha0, "D#6" },
        };
        
        private static int GetNoteMidi(string noteName)
        {
            if (string.IsNullOrEmpty(noteName))
                throw new ArgumentException("Note name cannot be null or empty.", nameof(noteName));

            // Regular expression to parse note name
            var match = Regex.Match(noteName, @"^([A-Ga-g])([#b]?)(-?\d+)$");
            if (!match.Success)
                throw new ArgumentException(
                    "Invalid note format. Expected format: Note[Sharp/Flat]Octave (e.g., C#4, Bb3, G5)",
                    nameof(noteName));

            var note = match.Groups[1].Value.ToUpper();
            var accidental = match.Groups[2].Value;
            var octave = int.Parse(match.Groups[3].Value);

            if (!Enum.TryParse(note.ToUpper(), out NoteStep noteStep))
                throw new ArgumentException($"Invalid note step: {note}");
            
            var semitones = (int)noteStep;

            if (!string.IsNullOrEmpty(accidental))
            {
                switch (accidental)
                {
                    case "b":
                        semitones -= 1;
                        break;
                    case "#":
                        semitones += 1;
                        break;
                }
            }
            
            // Calculate MIDI number
            var midiCode = (octave + 1) * 12 + semitones;

            if (midiCode is < 0 or > 127)
                throw new ArgumentOutOfRangeException(nameof(noteName),
                    "Resulting MIDI note must be between 0 and 127.");

            return midiCode;
        }
        
        public void Update()
        {
            if (!IsListeningForEvents)
                return;

            foreach (var keyNote in _inputMap)
            {
                if (Input.GetKeyDown(keyNote.Key))
                {
                    var midiCode = GetNoteMidi(keyNote.Value); 
                    _notesOn.Add(midiCode);
                    EventReceived?.Invoke(this, new MidiEventReceivedEventArgs(
                        new NoteOnEvent((SevenBitNumber)midiCode, SevenBitNumber.MaxValue)));                
                }
                else if (Input.GetKeyUp(keyNote.Key))
                {
                    var midiCode = GetNoteMidi(keyNote.Value); 
                    _notesOn.Remove(midiCode);
                    EventReceived?.Invoke(this, new MidiEventReceivedEventArgs(
                        new NoteOffEvent((SevenBitNumber)midiCode, SevenBitNumber.MinValue)));
                }
            }
        }

        /// <inheritdoc/>
        public IEnumerable<int> NotesBeingPlayed()
        {
            return _notesOn;
        }
        
        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}