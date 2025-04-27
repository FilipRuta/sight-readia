using System.Collections.Generic;
using SheetMusic;

public static class Constants
{
    public const float LineSpacing = 1.0f;
    public const float SpaceBetweenStaves = 4.0f;
    public const float StemLength = 3.5f;
    public const float BeamWidth = 5.5f;
    public const float MinAllowedSymbolDistance = 2.5f;
    public const float MaxAllowedSymbolDistance = 8.0f;
    public const float BarLinePadding = 2;
    public const float AfterClefPadding = 2;
    public const float KeySignaturePadding = 1;
    public const float AfterKeySigntaurePadding = 1f;
    public const float AfterTimeSignaturePadding = 2;
    public const float AfterStaffHeadPadding = 3;
    public const float NoteToDotPadding = 1;
    public const float DotMutualPadding = 0.5f;
    public const float BeamHookLengthMultiplier = 0.3f;
    public const float BeamSlopeLimit = 0.3f;
    public const float AdjacentNoteheadOffset = 1.055f;
    public const float BarLineWidth = 3.0f;
    

    // Lists sharps in order from left to right (flats in reverse order)
    public static readonly List<NoteStep> FifthsToAccidentals = new()
    {
        NoteStep.F,
        NoteStep.C,
        NoteStep.G,
        NoteStep.D,
        NoteStep.A,
        NoteStep.E,
        NoteStep.B
    };


    public static readonly List<NoteStep> NoteSequence = new()
    {
        NoteStep.C,
        NoteStep.D,
        NoteStep.E,
        NoteStep.F,
        NoteStep.G,
        NoteStep.A,
        NoteStep.B
    };


    public static readonly Dictionary<NoteType, int> NoteToFlagsCount = new()
    {
        { NoteType.EIGHTH, 1 },
        { NoteType.NOTE16, 2 },
        { NoteType.NOTE32, 3 },
        { NoteType.NOTE64, 4 },
        { NoteType.NOTE128, 5 },
        { NoteType.NOTE256, 6 },
        { NoteType.NOTE512, 7 },
        { NoteType.NOTE1024, 8 },
    };

    public static readonly Dictionary<string, NoteType> MxlNoteToNoteType = new()
    {
        { "maxima", NoteType.MAXIMA },
        { "long", NoteType.LONG },
        { "breve", NoteType.BREVE },
        { "whole", NoteType.WHOLE },
        { "half", NoteType.HALF },
        { "quarter", NoteType.QUARTER },
        { "eighth", NoteType.EIGHTH },
        { "16th", NoteType.NOTE16 },
        { "32nd", NoteType.NOTE32 },
        { "64th", NoteType.NOTE64 },
        { "128th", NoteType.NOTE128 },
        { "256th", NoteType.NOTE256 },
        { "512th", NoteType.NOTE512 },
        { "1024th", NoteType.NOTE1024 },
    };

    public static (int min, int max) PCKeyboardNoteMidiRange = (60, 88);
    public static (int min, int max) FullMidiKeyboardNoteMidiRange = (0, 127);

    public static string DefaultInputDevice = "Computer Keyboard";
    public static string DefaultOutputDevice = "Computer Audio";
    public static List<string> IgnoredDevices = new() { "Microsoft GS Wavetable Synth" };
}