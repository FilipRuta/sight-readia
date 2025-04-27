namespace SheetMusic
{
    /// <summary>
    /// Accidental values with integer values representing shift of the pitch
    /// </summary>
    public enum AccidentalType
    {
        FLAT = -1,
        NATURAL = 0,
        SHARP = 1
    }

    public enum NoteType
    {
        MAXIMA = 8192,
        LONG = 4096,
        BREVE = 2048,
        WHOLE = 1024,
        HALF = 512,
        QUARTER = 256,
        EIGHTH = 128,
        NOTE16 = 64,
        NOTE32 = 32,
        NOTE64 = 16,
        NOTE128 = 8,
        NOTE256 = 4,
        NOTE512 = 2,
        NOTE1024 = 1
    }

    /// <summary>
    /// Note step values with integer values representing number of semitones in a scale
    /// </summary>
    public enum NoteStep
    {
        C = 0,
        D = 2,
        E = 4,
        F = 5,
        G = 7,
        A = 9,
        B = 11
    }
    
    public enum StaffPosition
    {
        TOP,
        BOTTOM
    }
    
    public enum BeamValues
    {
        BEGIN,
        CONTINUE,
        END,
        FORWARD_HOOK,
        BACKWARD_HOOK
    }

    public enum ChordNoteOrientation
    {
        LEFT,
        RIGHT
    }
}