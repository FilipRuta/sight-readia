using System.Collections.Generic;
using Melanchall.DryWetMidi.Multimedia;

namespace InputControllers
{
    /// <summary>
    /// Defines an interface for handling MIDI input device interactions in a piano learning application.
    /// </summary>
    public interface IInputController
    {
        /// <summary>
        /// Initializes the input device with the specified device name.
        /// </summary>
        /// <param name="deviceName">The name of the MIDI input device to be initialized.</param>
        public void Initialize(string deviceName);
    
        /// <summary>
        /// Gets a collection of MIDI note numbers currently being played on the input device.
        /// </summary>
        /// <returns>An enumerable collection of MIDI note numbers.</returns>
        public IEnumerable<int> NotesBeingPlayed();
    
        /// <summary>
        /// Retrieves the underlying MIDI input device.
        /// </summary>
        /// <returns>The MIDI input device associated with this controller.</returns>
        public IInputDevice GetInputDevice();
    
        /// <summary>
        /// Gets a value indicating whether any notes are currently being played.
        /// </summary>
        public bool AnyNotesPlayed { get; }

        /// <summary>
        /// Check if given note is within playable device range
        /// </summary>
        /// <param name="midiCode">Midi code of the note</param>
        /// <returns></returns>
        public bool IsInDeviceRange(int midiCode);
    
        /// <summary>
        /// Gets the name of the input device.
        /// </summary>
        public string Name { get; }
    }
}