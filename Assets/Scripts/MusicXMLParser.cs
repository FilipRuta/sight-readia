using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SheetMusic;
using UnityEngine;

// Needs to be added to be able to use record and init.
namespace System.Runtime.CompilerServices
{
    class IsExternalInit
    {
    }
}

public class MusicXMLParser
{
    private int _divisions;
    private int _staves;
    private StaffHead _lastStaffHead;

    private record MeasureElement(string Name, string Duration);

    private record SymbolElement(
        string Name,
        XElement Rest,
        string Step,
        string Octave,
        string Duration,
        string Type,
        string Accidental,
        int DotCount,
        string Staff,
        IEnumerable<XElement> Beams,
        bool IsStemDown,
        bool IsChordNote,
        bool IsGraceNote
    ) : MeasureElement(Name, Duration);

    /// <summary>
    /// Attempts to parse an integer element from the XML element.
    /// </summary>
    /// <param name="field">The name of the XML element to parse.</param>
    /// <param name="element">The XML element containing the element.</param>
    /// <param name="result">The parsed integer value, if successful.</param>
    /// <returns>True if parsing succeeds; otherwise, false.</returns>
    private bool ParseIntElement(string field, XElement element, out int result)
    {
        var value = element.Descendants(field).FirstOrDefault()?.Value;
        return int.TryParse(value, out result);
    }

    /// <summary>
    /// Extracts staff head information (key signature, time signature, clefs) from a measure.
    /// </summary>
    /// <param name="measure">The XML element representing a measure.</param>
    /// <param name="parseGrandStaff">Indicates whether to parse grand staff notation (from 2 staves).</param>
    /// <returns>A new StaffHead instance if changes are detected; otherwise, null.</returns>
    private StaffHead GetStaffHeadFromMeasure(XElement measure, bool parseGrandStaff)
    {
        var headChanged = false;

        // Get key signature
        if (ParseIntElement("fifths", measure, out var newFifths))
            headChanged = true;
        else if (_lastStaffHead != null)
        {
            newFifths = _lastStaffHead.Fifths;
        }

        // Get beat
        var newTimeSignature = (beats: 0, beatType: 0);
        if (
            ParseIntElement("beats", measure, out newTimeSignature.beats) &&
            ParseIntElement("beat-type", measure, out newTimeSignature.beatType)
        )
            headChanged = true;
        else if (_lastStaffHead != null)
        {
            newTimeSignature = _lastStaffHead.TimeSignature;
        }

        //Get clef
        Clef newTopClef = null;
        Clef newBottomClef = null;
        if (_lastStaffHead != null)
        {
            newTopClef = _lastStaffHead.TopClef;
            newBottomClef = _lastStaffHead.BottomClef;
        }

        var clefs = measure.Descendants("clef");
        foreach (var clef in clefs)
        {
            var number = clef.Attribute("number")?.Value;
            var clefStep = clef.Element("sign")?.Value;
            if (!ParseIntElement("line", clef, out var clefLine) || string.IsNullOrEmpty(clefStep))
            {
                Debug.LogError("Unsupported clef");
                throw new FormatException("Unsupported clef");
            }

            if (!Enum.TryParse(clefStep.ToUpper(), out NoteStep clefStepParsed))
            {
                Debug.LogError($"Unsupported clef step {clefStep}");
                throw new FormatException($"Unsupported clef step {clefStep}");
            }

            if (number is null or "1")
            {
                newTopClef = new Clef(clefStepParsed, clefLine);
                headChanged = true;
            }
            else if (parseGrandStaff && number == "2")
            {
                newBottomClef = new Clef(clefStepParsed, clefLine);
                headChanged = true;
            }
        }

        return headChanged ? new StaffHead(newTopClef, newBottomClef, newFifths, newTimeSignature) : null;
    }

    /// <summary>
    /// Parses main song attributes such as divisions and staves from the MusicXML document.
    /// </summary>
    /// <param name="mxlDoc">The MusicXML document to parse.</param>
    private void ParseMainSongAttributes(XDocument mxlDoc)
    {
        // Division specifies duration of quarter note
        if (!ParseIntElement("divisions", mxlDoc.Root, out _divisions))
        {
            _divisions = 1;
            Debug.LogWarning("Divisions not specified");
        }

        if (!ParseIntElement("staves", mxlDoc.Root, out _staves))
            _staves = 1;
    }


    /// <summary>
    /// Parses beam elements from a symbol and converts them into BeamValues.
    /// </summary>
    /// <param name="symbolElement">The symbol containing beam elements.</param>
    /// <returns>A list of parsed BeamValues.</returns>
    private List<BeamValues> ParseBeam(SymbolElement symbolElement)
    {
        // MusicXML does not specify the order of beams
        var beamValuesParsed = symbolElement.Beams
            .OrderBy(n =>
                (int?)n.Attribute("number") ??
                int.MaxValue) // Sort by "number" attribute; defaulting to max value if missing
            .Select(n =>
            {
                var formattedValue = n.Value.Replace(" ", "_"); // to parse to enum correctly
                return Enum.TryParse<BeamValues>(formattedValue, true, out var bt)
                    ? bt
                    : (BeamValues?)null;
            }).ToList();

        if (beamValuesParsed.Any(n => n == null))
            Debug.LogWarning($"Invalid beam-value {symbolElement.Beams}");
        return beamValuesParsed.Where(bt => bt.HasValue).Select(bt => bt.Value).ToList();
    }
    
    /// <summary>
    /// Infers the note type based on duration and division values
    /// </summary>
    /// <param name="duration">The duration value from the MusicXML note element</param>
    /// <returns>The inferred note type as an enum value</returns>
    private NoteType InferNoteType(int duration)
    {
        // Calculate scaled duration (where a quarter note = 256)
        var scaledDuration = ((long)duration * 256) / _divisions;
    
        // Find the closest note type by comparing with enum values
        var closestType = NoteType.QUARTER; // Default
        var minDifference = int.MaxValue;
    
        foreach (NoteType noteType in Enum.GetValues(typeof(NoteType)))
        {
            var noteTypeValue = (int)noteType;
            var difference = Math.Abs((int)scaledDuration - noteTypeValue);
        
            if (difference < minDifference)
            {
                minDifference = difference;
                closestType = noteType;
            }
        }
    
        return closestType;
    }
    

    /// <summary>
    /// Creates a Rest object from a SymbolElement.
    /// </summary>
    /// <param name="rest">The SymbolElement representing a rest.</param>
    /// <param name="duration">The duration of the rest in ticks.</param>
    /// <param name="startPosition">The start position of the rest within the measure.</param>
    /// <returns>A Rest object representing the parsed rest.</returns>
    private Rest CreateRest(SymbolElement rest, int duration, int startPosition)
    {
        var isWholeMeasure = rest.Rest.Attribute("measure")?.Value == "yes";
        var restType = NoteType.WHOLE;
        if (rest.Type == null)
        {
            restType = InferNoteType(duration);
        }
        else if (!isWholeMeasure)
        {
            if (string.IsNullOrEmpty(rest.Type) ||
                !Constants.MxlNoteToNoteType.TryGetValue(rest.Type, out NoteType parsedType))
            {
                Debug.LogError($"Unsupported rest type {rest.Type}");
                throw new FormatException($"Unsupported rest type {rest.Type}");
            }

            restType = parsedType;
        }

        var staff = int.TryParse(rest.Staff, out var number) ? number : 1;
        return new Rest(
            duration,
            startPosition,
            restType,
            staff,
            isWholeMeasure,
            rest.DotCount
        );
    }

    /// <summary>
    /// Creates a Note object from a SymbolElement.
    /// </summary>
    /// <param name="note">The SymbolElement representing a note.</param>
    /// <param name="beamValues">A list of BeamValues associated with the note.</param>
    /// <param name="duration">The duration of the note in ticks.</param>
    /// <param name="startPosition">The start position of the note within the measure.</param>
    /// <returns>A Note object representing the parsed note.</returns>
    private Note CreateNote(SymbolElement note, List<BeamValues> beamValues, int duration, int startPosition)
    {
        if (!int.TryParse(note.Octave, out var octave) || octave is > 10 or < 0)
        {
            Debug.LogError($"Invalid octave {note.Octave}");
            throw new FormatException($"Invalid octave {note.Octave}");
        }
        var staff = note.Staff != null ? int.Parse(note.Staff) : 1;

        NoteType noteType;
        if (note.Type == null)
        {
            noteType = InferNoteType(duration);
        }
        else if (!Constants.MxlNoteToNoteType.TryGetValue(note.Type, out noteType))
        {
            Debug.LogError($"Unsupported note type {note.Type}");
            throw new FormatException($"Unsupported note type {note.Type}");
        }

        if (!Enum.TryParse(note.Step?.ToUpper(), out NoteStep step))
        {
            Debug.LogError($"Unsupported step {step}");
            throw new FormatException($"Unsupported step {step}");
        }

        AccidentalType? accidental = null;
        if (!string.IsNullOrEmpty(note.Accidental))
        {
            if (Enum.TryParse(note.Accidental.ToUpper(), out AccidentalType parsedAccidental))
            {
                accidental = parsedAccidental;
            }
            else
            {
                Debug.LogError($"Unsupported accidental {note.Accidental}");
                throw new FormatException($"Unsupported accidental {note.Accidental}");
            }
        }

        return new Note(
            duration,
            startPosition,
            noteType,
            staff,
            step,
            accidental,
            octave,
            note.IsStemDown,
            note.DotCount,
            beamValues
        );
    }

    /// <summary>
    /// Parses measure elements into symbols and beam groups.
    /// </summary>
    /// <param name="measureElements">A list of MeasureElements to parse.</param>
    /// <param name="parseGrandStaff">Indicates whether to parse grand staff notation (from 2 staves).</param>
    /// <returns>A tuple containing a list of symbols and beam groups in the measure.</returns>
    private (List<Symbol> measureSymbols, List<BeamGroup> beamGroupsInMeasure) ParseMeasure(
        IEnumerable<MeasureElement> measureElements, bool parseGrandStaff
    )
    {
        var measureSymbols = new List<Symbol>();
        var currentSymbolStart = 0;

        Note lastNote = null;
        var beamGroupsInMeasure = new List<BeamGroup>();
        BeamGroup currentBeamGroup = null;
        var beamCnt = 0;
        foreach (var measureElement in measureElements)
        {
            if (currentSymbolStart < 0)
            {
                Debug.LogWarning($"Current symbol start is negative!");
                currentSymbolStart = 0;
            }
            var hasDuration = int.TryParse(measureElement.Duration, out var parsedDuration);
            if (measureElement.Name == "forward" && hasDuration)
            {
                currentSymbolStart += parsedDuration;
                continue;
            }

            if (measureElement.Name == "backup" && hasDuration)
            {
                currentSymbolStart -= parsedDuration;
                continue;
            }

            var symbolElement = (SymbolElement)measureElement;
            if (symbolElement.IsGraceNote)
                continue;
            if (!parseGrandStaff && symbolElement.Staff != null && symbolElement.Staff != "1")
                continue;

            if (!hasDuration)
                throw new FormatException($"Missing duration for {measureElement}");


            List<BeamValues> beamValues = null;
            if (!symbolElement.IsChordNote && symbolElement.Beams.Any())
            {
                beamValues = ParseBeam(symbolElement);

                // create new beaming group if not null
                currentBeamGroup ??= new BeamGroup();

                var beamBegins = beamValues.Count(b => b == BeamValues.BEGIN);
                var beamEnds = beamValues.Count(b => b == BeamValues.END);
                beamCnt = beamCnt + beamBegins - beamEnds;
            }

            if (symbolElement.Rest?.Value != null)
            {
                measureSymbols.Add(CreateRest(symbolElement, parsedDuration, currentSymbolStart));
            }
            else
            {
                var newNote = CreateNote(symbolElement, beamValues, parsedDuration, currentSymbolStart);

                if (symbolElement.IsChordNote && lastNote != null)
                {
                    // Add the new note as a chord note to first non-chord note
                    lastNote.AddChordNote(newNote);
                }
                else // update last note
                {
                    lastNote = newNote;
                    measureSymbols.Add(newNote);
                }

                if (currentBeamGroup != null && beamValues != null && !symbolElement.IsChordNote)
                {
                    currentBeamGroup.AddNote(newNote);
                    if (beamCnt == 0)
                    {
                        // end of beam group
                        beamGroupsInMeasure.Add(currentBeamGroup);
                        currentBeamGroup = null;
                    }
                }
            }

            if (!symbolElement.IsChordNote) // Don't add duration for each chord note
                currentSymbolStart += parsedDuration;
        }

        if (currentBeamGroup != null)
        {
            Debug.Log($"Beam group wasn't correctly ended");
        }

        measureSymbols.Sort((s1, s2) => s1.StartPosition.CompareTo(s2.StartPosition));
        return (measureSymbols, beamGroupsInMeasure);
    }

    /// <summary>
    /// Parses a MusicXML file into a MusicScore object.
    /// </summary>
    /// <param name="file">The MusicXML file content as a string.</param>
    /// <param name="parseGrandStaff">Indicates whether to parse grand staff notation (from 2 staves).</param>
    /// <returns>A MusicScore object representing the parsed score; null if parsing fails.</returns>
    public MusicScore Parse(string file, bool parseGrandStaff = false)
    {
        if (file == null)
            throw new Exception("File not provided");

        var mxlDoc = XDocument.Parse(file);

        ParseMainSongAttributes(mxlDoc);

        var measureList = new List<Measure>();
        var docMeasures = mxlDoc.Descendants("measure");
        foreach (var measure in docMeasures)
        {
            var measureElements = new List<MeasureElement>();
            foreach (var measureElement in measure.Elements())
            {
                if (measureElement.Name == "note")
                    measureElements.Add(
                        new SymbolElement(
                            measureElement.Name.ToString(),
                            measureElement.Element("rest"),
                            measureElement.Element("pitch")?.Element("step")?.Value,
                            measureElement.Element("pitch")?.Element("octave")?.Value,
                            measureElement.Element("duration")?.Value,
                            measureElement.Element("type")?.Value,
                            measureElement.Element("accidental")?.Value,
                            measureElement.Descendants("dot").Count(),
                            measureElement.Element("staff")?.Value,
                            measureElement.Elements("beam"),
                            measureElement.Element("stem")?.Value == "down",
                            measureElement.Element("chord")?.Value != null,
                            measureElement.Element("grace")?.Value != null
                        )
                    );
                else if (measureElement.Name == "forward" || measureElement.Name == "backup")
                    measureElements.Add(new MeasureElement(measureElement.Name.ToString(),
                        measureElement.Element("duration")?.Value));
            }

            var newStaffHead = GetStaffHeadFromMeasure(measure, parseGrandStaff);
            // Extract notes and rests from current measure
            var (measureSymbols, beamGroupsInMeasure) = ParseMeasure(measureElements, parseGrandStaff);
            // Create new measure holding the symbols
            measureList.Add(
                new Measure(
                    measureSymbols,
                    beamGroupsInMeasure,
                    newStaffHead ?? _lastStaffHead
                    , _lastStaffHead
                )
            );
            if (newStaffHead != null)
                _lastStaffHead = newStaffHead;
        }

        return new MusicScore(measureList, _staves);
    }
}