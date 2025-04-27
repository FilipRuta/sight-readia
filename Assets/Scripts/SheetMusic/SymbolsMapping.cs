using System.Collections.Generic;

namespace SheetMusic
{
    public static class SymbolsMapping
    {
        public const string Brace = "\uE000";
        public const string AugmentationDot = "\uE1E7";

        public static readonly Dictionary<NoteType, string> NoteHeads = new()
        {
            { NoteType.LONG, "\uE0A0" },
            { NoteType.BREVE, "\uE0A1" },
            { NoteType.WHOLE, "\uE0A2" }, // Empty note head
            { NoteType.HALF, "\uE0A3" }, // Empty note head leaning
        };

        public const string FullNoteHead = "\uE0A4"; 

        public const string FlagFirst = "\uE240";
        
        public const string TimeSignatureSymbolOne = "\uE080";


        public static readonly Dictionary<AccidentalType, string> Accidentals = new()
        {
            { AccidentalType.SHARP, "\uE262" },
            { AccidentalType.FLAT, "\uE260" },
            { AccidentalType.NATURAL, "\uE261" },
        };
        
        public static readonly Dictionary<NoteType, string> Rests = new()
        {
            {NoteType.MAXIMA, "\uE4E0"},
            {NoteType.LONG, "\uE4E1"},
            {NoteType.BREVE, "\uE4E2"},
            { NoteType.WHOLE,"\uE4E3" },
            { NoteType.HALF, "\uE4E4" },
            { NoteType.QUARTER, "\uE4E5" },
            { NoteType.EIGHTH, "\uE4E6" },
            { NoteType.NOTE16, "\uE4E7" },
            { NoteType.NOTE32, "\uE4E8" },
            { NoteType.NOTE64, "\uE4E9" },
            { NoteType.NOTE128, "\uE4EA" },
            { NoteType.NOTE256, "\uE4EB" },
            { NoteType.NOTE512, "\uE4EC" },
            { NoteType.NOTE1024, "\uE4ED" },
        };
        
        public static readonly Dictionary<NoteStep, string> Clefs = new()
        {
            { NoteStep.G,"\uE050" },
            { NoteStep.C, "\uE05C" },
            { NoteStep.F, "\uE062" },
        };
    }
}