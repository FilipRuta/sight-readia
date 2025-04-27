using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

namespace SheetMusic
{
    public class Measure
    {
        private readonly List<Symbol> _topSymbols = new();
        private readonly List<Symbol> _bottomSymbols = new();
    
        private int _currentTopSymbolIdx = 0;
        private int _currentBottomSymbolIdx = 0;

        public StaffHead StaffHead { get; }
        public float MeasureWidth { get; private set; }
        public bool HasBeamGroups => _beamGroups is { Count: > 0 };
        
        public Func<float, float> DurationRemapFunc { get; private set; }
        public Func<float, float> PositionRemapFunc { get; private set; }

        private readonly List<BeamGroup> _beamGroups;
        private readonly HashSet<Note> _processedBeamNotes = new HashSet<Note>();
        private int _currentBeamGroupIndex = 0;
        
        private readonly GameObject _line;
        private readonly StaffHead _prevStaffHead;
    
        private GameObject _measureObject;
        private GameObject _topStaffObject;
        private GameObject _bottomStaffObject;
        private GameObject _braceObject;
    
        // stores accidental alteration of notes within measure (fifths, accidentals based on prev notes)
        private Dictionary<(NoteStep, int), int> _currentTopNoteAlterations = new ();
        private Dictionary<(NoteStep, int), int> _currentBottomNoteAlterations = new ();

        /// <summary>
        /// Creates a new measure with the given symbols, beam groups, and staff configuration
        /// </summary>
        /// <param name="symbols">List of symbols (notes, rests) in this measure</param>
        /// <param name="beams">Beam groups connecting notes in this measure</param>
        /// <param name="staffHead">Staff head information (clef, key, time signature) for this measure</param>
        /// <param name="prevStaffHead">Previous staff head (for determining if staff head should be drawn)</param>
        public Measure(List<Symbol> symbols, List<BeamGroup> beams, StaffHead staffHead, [CanBeNull] StaffHead prevStaffHead)
        {
            foreach (var s in symbols)
            {
                if (s.IsInTopStaff())
                    _topSymbols.Add(s);
                else
                    _bottomSymbols.Add(s);
            }

            StaffHead = staffHead;
            _beamGroups = beams;
            _prevStaffHead = prevStaffHead;
            _line = MainManager.Instance.PrefabLoader.GetPrefab("Line");
            PrepareAlterationsForKey();

            foreach (Symbol symbol in symbols)
            {
                symbol.SetParentMeasure(this);
                if (symbol is Note note)
                {
                    // Set parent measure also for all chord notes
                    if (note.IsChord)
                    {
                        foreach (var chordNote in note.GetChordNotes())
                        {
                            chordNote.SetParentMeasure(this);
                            PrecalculateMidiCode(chordNote);
                        }

                        note.OrderChordNotes();
                    }
                    else
                    {
                        PrecalculateMidiCode(note);
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the MIDI note value for a note based on its pitch, key and preceding accidentals
        /// <param name="note">Note with step and octave</param>
        /// </summary>
        private void PrecalculateMidiCode(Note note)
        {
            var alter = GetNoteAlterationInSemitones(note);
            var semitones = (int)note.Step + alter;
            var midi = 12 * (note.Octave + 1) + semitones;
            note.MidiCode = midi;
        }

        /// <summary>
        /// Sets up initial accidental state based on the key signature
        /// </summary>
        private void PrepareAlterationsForKey()
        {
            var fifths = StaffHead.Fifths;
            var maxOctaves = 10;
            
            for (var octave = 0; octave <= maxOctaves; octave++)
            {
                foreach (var key in Constants.NoteSequence)
                {
                    _currentTopNoteAlterations[(key, octave)] = 0;
                    _currentBottomNoteAlterations[(key, octave)] = 0;
                }
            }
            if (fifths == 0)
                return;
            
            var affectedKeys = fifths > 0 
                ? Constants.FifthsToAccidentals.Take(fifths)  // Take first 'fifths' elements (sharps)
                : Constants.FifthsToAccidentals.Skip(Constants.FifthsToAccidentals.Count + fifths); // Skip from the end for negative values (flats)

            var sign = Math.Sign(fifths);

            
            foreach (var key in affectedKeys)
            {
                for (var octave = 0; octave <= maxOctaves; octave++)
                {
                    _currentTopNoteAlterations[(key, octave)] += sign; // +1 for positive, -1 for negative
                    _currentBottomNoteAlterations[(key, octave)] += sign;
                }
            }
        }
        
        /// <summary>
        /// Determines the semitone alteration for a note based on key signature and previous accidentals in the measure
        /// </summary>
        /// <param name="note">Note instance</param>
        /// <returns>Semitone offset</returns>
        private int GetNoteAlterationInSemitones(Note note)
        {
            if (!note.Accidental.HasValue)
                return note.IsInTopStaff() ? _currentTopNoteAlterations[(note.Step, note.Octave)] : _currentBottomNoteAlterations[(note.Step, note.Octave)];
            var alter = (int) note.Accidental;
            if (note.IsInTopStaff())
                _currentTopNoteAlterations[(note.Step, note.Octave)] = alter;
            else
                _currentBottomNoteAlterations[(note.Step, note.Octave)] = alter;
            return alter;
        }

        /// <summary>
        /// Creates staff lines
        /// <param name="xStartPos">Position where to draw the staff lines</param>
        /// <param name="parent">Parent transform for staff lines</param>
        /// </summary>
        private void DrawStaffLines(float xStartPos, Transform parent)
        {
            // Draw staff lines
            for (int i = 0; i < 5; i++)
            {
                var staffLine = GameObject.Instantiate(_line, new Vector3(xStartPos, -i * Constants.LineSpacing, 0), Quaternion.identity, parent.transform);
                staffLine.name = "StaffLine" + i;
                staffLine.GetComponent<LineRenderer>().SetPosition(1, new Vector3(MeasureWidth, 0, 0));
            }
        }
    
        /// <summary>
        /// Creates a vertical bar line at the specified position
        /// /// <param name="xStartPos">Position where to draw the bar line</param>
        /// <param name="parent">Parent transform for bar line</param>
        /// </summary>
        private void DrawBarLine(float xStartPos, Transform parent)
        {
            var barLine = GameObject.Instantiate(_line, new Vector3(xStartPos, 0), Quaternion.identity, parent.transform);
            barLine.name = "BarLine";
            var lineRenderer = barLine.GetComponent<LineRenderer>();
            lineRenderer.SetPosition(1, new Vector3(0, -Constants.LineSpacing * 4, 0));
            lineRenderer.widthMultiplier = Constants.BarLineWidth;
        }

        /// <summary>
        /// Draw a brace for grand-staff notation
        /// </summary>
        public void DrawBrace()
        {
            if (_bottomStaffObject == null)
            {
                return;
            }
            var symbolPrefab = MainManager.Instance.PrefabLoader.GetPrefab("Symbol");
            var width = _bottomStaffObject.transform.position.y - Constants.LineSpacing * 4;
            // The brace has anchor at the bottom
            _braceObject = GameObject.Instantiate(symbolPrefab, new Vector3(-1, width, 0), Quaternion.identity, _measureObject.transform);
            
            var braceText = _braceObject.GetComponent<TextMeshPro>();
            braceText.text = SymbolsMapping.Brace;
            braceText.fontSize = Math.Abs(width * 10); // use 10 times larger font than width to fit to the both staffs 
        }

        /// <summary>
        /// Maps a value from one range to another using linear interpolation
        /// </summary>
        private static float Remap(float fromMin, float fromMax, float toMin, float toMax, float value)
        {
            // First, normalize the input value to a 0-1 range
            var normalized = (value - fromMin) / Mathf.Max(fromMax - fromMin, 0.001f);
    
            // Then scale and shift to the target range
            return toMin + (normalized * (toMax - toMin));
        }

        /// <summary>
        /// Renders all symbols (notes, rests) for a specific staff (top or bottom)
        /// </summary>
        /// <param name="xPos">Starting horizontal position for drawing</param>
        /// <param name="staffPosition">Which staff to draw symbols for (TOP or BOTTOM)</param>
        /// <param name="parent">Parent transform that symbols will be attached to</param>
        /// <returns>New x-position after drawing all symbols</returns>
        private float DrawSymbols(float xPos, StaffPosition staffPosition, Transform parent)
        {
            var symbols = staffPosition == StaffPosition.TOP ? _topSymbols : _bottomSymbols;
            if (symbols == null || symbols.Count == 0)
                return xPos;
            
            foreach (var s in symbols)
            {
                // Skip notes that are part of a beam group and already processed
                if (s is Note note && _processedBeamNotes.Contains(note))
                    continue;
                if (s is Note { HasBeams: true } && HasBeamGroups && _currentBeamGroupIndex < _beamGroups.Count)
                {
                    // Find the corresponding beam group
                    var beamGroup = _beamGroups[_currentBeamGroupIndex];
                    if (beamGroup != null)
                    {
                        // Draw the entire beam group
                        beamGroup.Draw(s.GetStartPositionInMeasure(), parent.transform);
                        var beamGroupNotes = beamGroup.GetNotes();
                        if (beamGroupNotes != null)
                        {
                            // Mark all notes in this beam group as processed
                            foreach (var beamNote in beamGroup.GetNotes())
                            {
                                _processedBeamNotes.Add(beamNote);
                            }
                        }
                
                        // Adjust xPos based on the beam group's width
                        xPos += beamGroup.GetTotalWidth();
                        _currentBeamGroupIndex++;
                    }
                }
                else
                {
                    s.Draw(s.GetStartPositionInMeasure(), parent.transform);
                    var duration = s.GetWidthBasedOnDuration();
                    xPos += duration;
                }
            }

            return xPos;
        }

        /// <summary>
        /// Renders a staff and its contents (bar lines, staff head, symbols)
        /// </summary>
        /// <param name="xPos">Starting horizontal position for drawing</param>
        /// <param name="parent">Parent transform that staff content will be attached to</param>
        /// <param name="staffPosition">Which staff to draw (TOP or BOTTOM)</param>
        /// <param name="noSymbols">If true, only staff lines and bar lines are drawn without notes</param>
        /// <returns>New x-position after drawing staff content</returns>
        private float DrawStaffContent(float xPos, Transform parent, StaffPosition staffPosition, bool noSymbols)
        {
            DrawBarLine(xPos, parent.transform);
        
            // pad after last bar line
            xPos += Constants.BarLinePadding;
            StaffHead.Draw(xPos, parent.transform, _prevStaffHead, staffPosition);
            xPos += StaffHead.StaffHeadWidth;
            
            if (!noSymbols)
            {
                xPos = DrawSymbols(xPos, staffPosition, parent.transform);
            }

            return xPos;
        }

        /// <summary>
        /// Calculates the min and max width for symbols in a specific staff
        /// </summary>
        /// <param name="staffPosition">Which staff to calculate widths for (TOP or BOTTOM)</param>
        /// <returns>Tuple of (min width, max width) or null if staff has no symbols</returns>
        private (float minWidth, float maxWidth)? CalculateNoteWidthRangeInStaff(StaffPosition staffPosition)
        {
            var symbols = staffPosition == StaffPosition.TOP ? _topSymbols : _bottomSymbols;
            
            if (symbols == null || symbols.Count == 0)
                return null;
            
            var minWidth = symbols.Select(s => s.Duration).Min();
            var maxWidth = symbols.Select(s => s.Duration).Max();
            return (minWidth, maxWidth);
        }

        /// <summary>
        /// Calculates the optimal distance between staffs in grand staff based on present notes 
        /// </summary>
        /// <returns></returns>
        private float CalculateOptimalGrandStaffDistance()
        {
            var getLowestNote = new Func<List<Symbol>, float>((symbols) =>
                (symbols.OfType<Note>().Select(n => (n.IsChord ? n.GetLowestChordNote() : n).GetWorldPosition().y)
                    .DefaultIfEmpty(0.0f)
                    .Min()));
            var getHighestNote = new Func<List<Symbol>, float>((symbols) =>
                (symbols.OfType<Note>().Select(n => (n.IsChord ? n.GetHighestChordNote() : n).GetWorldPosition().y)
                    .DefaultIfEmpty(0.0f)
                    .Max()));
                
            // Set bottom line of top staff and top line of top staff (in local coordinates) as minimum thresholds 
            var topStaffBottomLine = -Constants.LineSpacing * 4;
            var bottomStaffTopLine = 0;
                
            var lowestTopStaffY = Math.Min(topStaffBottomLine, getLowestNote(_topSymbols));
            var highestBottomStaffY = Math.Max(bottomStaffTopLine, getHighestNote(_bottomSymbols));
                
            return Math.Abs(lowestTopStaffY - Constants.SpaceBetweenStaves - highestBottomStaffY);
        }
        
        /// <summary>
        /// Determines the range of note widths in the measure to ensure proper spacing
        /// </summary>
        /// <param name="renderGrandStaff">Whether both staves should be considered in calculations</param>
        private void CalculateNoteWidthRangeInMeasure(bool renderGrandStaff)
        {
            var min = Constants.MinAllowedSymbolDistance;
            var max = Constants.MaxAllowedSymbolDistance;
            
            var topRange = CalculateNoteWidthRangeInStaff(StaffPosition.TOP);
            var hasValidRange = false;
    
            if (topRange.HasValue)
            {
                min = topRange.Value.minWidth;
                max = topRange.Value.maxWidth;
                hasValidRange = true;
            }

            if (renderGrandStaff)
            {
                var bottomRange = CalculateNoteWidthRangeInStaff(StaffPosition.BOTTOM);
        
                if (bottomRange.HasValue)
                {
                    if (hasValidRange)
                    {
                        // We have values from both staves, take min and max
                        min = Mathf.Min(min, bottomRange.Value.minWidth);
                        max = Mathf.Max(max, bottomRange.Value.maxWidth);
                    }
                    else
                    {
                        // Only bottom staff has values
                        min = bottomRange.Value.minWidth;
                        max = bottomRange.Value.maxWidth;
                    }
                }
            }
            // Function to map min and max spaces between notes based on their durations 
            DurationRemapFunc =
                x => Remap(
                    min,
                    max,
                    Constants.MinAllowedSymbolDistance,
                    Constants.MaxAllowedSymbolDistance,
                    x
                );
        }
        
        private void CreatePositionMapper()
        {
            var symbols = _topSymbols.Union(_bottomSymbols).ToList();
            var sortedSymbols = symbols.DistinctBy(s => s.StartPosition).OrderBy(s => s.StartPosition).ToList();
            
            var positionMap = new Dictionary<float, float>();
    
            var currentPosition = 0.0f;
            foreach (var s in sortedSymbols)
            {
                positionMap[s.StartPosition] = currentPosition;
                currentPosition += s.GetWidthBasedOnDuration();
            }
            PositionRemapFunc = x => positionMap.GetValueOrDefault(x, x) + StaffHead.StaffHeadWidth;
        }
        /// <summary>
        /// Renders the complete measure with all its components
        /// </summary>
        /// <param name="xPos">Starting x-coordinate for the measure</param>
        /// <param name="parent">Parent transform that the measure will be attached to</param>
        /// <param name="renderGrandstaff">Whether to render both staves (grand staff)</param>
        /// <param name="noSymbols">If true, only staff lines and bar lines are drawn without symbols</param>
        /// <param name="emptyMeasureWidthForNotes">Width to allocate for empty measures</param>
        /// <returns>The optimal distance between the lowest symbol in top staff and the highest
        /// symbol in bottom staff</returns>
        public float Draw(
            float xPos,
            Transform parent,
            bool renderGrandstaff,
            bool noSymbols = false,
            float emptyMeasureWidthForNotes = 0.0f
        )
        {
            var measureStartPos = xPos;
            CalculateNoteWidthRangeInMeasure(renderGrandstaff);
            CreatePositionMapper(); 
            xPos = 0;
            _measureObject = new GameObject("Measure"){transform = { parent = parent }};
        
            _topStaffObject = new GameObject("TopStaff"){transform = { parent = _measureObject.transform}};
            if (renderGrandstaff && _bottomSymbols.Count > 0)
            {
                _bottomStaffObject = new GameObject("BottomStaff") {transform = { parent = _measureObject.transform}};
            }
        
            var xStartPos = xPos;
            DrawStaffContent(xPos, _topStaffObject.transform, StaffPosition.TOP, noSymbols);
            if (renderGrandstaff && _bottomSymbols.Count > 0)
                DrawStaffContent(xPos, _bottomStaffObject.transform, StaffPosition.BOTTOM, noSymbols);

            var lastTop = _topSymbols.LastOrDefault();
            var lastBottom = _bottomSymbols.LastOrDefault();
            var topWidth = lastTop != null ? lastTop.GetStartPositionInMeasure() + lastTop.GetWidthBasedOnDuration() : 0;
            var bottomWidth = lastBottom != null ? lastBottom.GetStartPositionInMeasure() + lastBottom.GetWidthBasedOnDuration() : 0;
            
            xPos = Math.Max(topWidth, bottomWidth);
            if (emptyMeasureWidthForNotes > 0)
                xPos += emptyMeasureWidthForNotes;
            MeasureWidth = xPos - xStartPos;

            DrawStaffLines(xStartPos, _topStaffObject.transform);
            DrawBarLine(xStartPos + MeasureWidth, _topStaffObject.transform);

            var optimalDistanceBetweenStaffs = Constants.SpaceBetweenStaves;
            if (renderGrandstaff && _bottomSymbols.Count > 0)
            {
                DrawStaffLines(xStartPos, _bottomStaffObject.transform);
                DrawBarLine(xStartPos + MeasureWidth, _bottomStaffObject.transform);

                optimalDistanceBetweenStaffs = CalculateOptimalGrandStaffDistance();
            }
            _measureObject.transform.position = new Vector3(measureStartPos, 0,0);
            return optimalDistanceBetweenStaffs;
        }

        /// <summary>
        /// Set the distance between top and bottom staff
        /// </summary>
        /// <param name="distance">Distance to set</param>
        public void SetStaffDistance(float distance)
        {
            if (_bottomStaffObject != null)
                _bottomStaffObject.transform.localPosition = new Vector3(0, -distance, 0);
        }

        /// <summary>
        /// Get the height range of the measure 
        /// </summary>
        /// <param name="renderGrandStaff">Whether the grand staff is rendered</param>
        /// <returns> The highest and the lowest Y coordinate of single-staff/grand-staff</returns>
        public (float, float) GetStaffHeightRange(bool renderGrandStaff)
        {
            if (renderGrandStaff && _bottomSymbols.Count > 0)
                return (0, _bottomStaffObject.transform.position.y - Constants.LineSpacing * 4);
            return (0, -Constants.LineSpacing * 4);
        }
        
        /// <summary>
        /// Cleans up the GameObject representing this measure
        /// </summary>
        public void DestroyMeasure()
        {
            GameObject.Destroy(_measureObject);
        }
    
        /// <summary>
        /// Gets the next symbol(s) to process in chronological order
        /// </summary>
        /// <returns>List of symbols that occur simultaneously, or null if no more symbols</returns>
        public List<Symbol> GetNextSymbols()
        {
            // No more symbols
            if (_currentTopSymbolIdx >= _topSymbols.Count && 
                _currentBottomSymbolIdx >= _bottomSymbols.Count)
                return null;

            var topX = (_currentTopSymbolIdx < _topSymbols.Count) 
                ? _topSymbols[_currentTopSymbolIdx].StartPosition 
                : float.PositiveInfinity;
    
            var bottomX = (_currentBottomSymbolIdx < _bottomSymbols.Count) 
                ? _bottomSymbols[_currentBottomSymbolIdx].StartPosition 
                : float.PositiveInfinity;

            var nextX = Math.Min(topX, bottomX);
            
            var nextSymbols = new List<Symbol>();
            const float tolerance = 0.01f;
            
            // Collect all top symbols at this time position
            while (_currentTopSymbolIdx < _topSymbols.Count &&
                   Math.Abs(_topSymbols[_currentTopSymbolIdx].StartPosition - nextX) < tolerance)
            {
                nextSymbols.Add(_topSymbols[_currentTopSymbolIdx++]);
            }

            // Collect all bottom symbols at this time position
            while (_currentBottomSymbolIdx < _bottomSymbols.Count &&
                   Math.Abs(_bottomSymbols[_currentBottomSymbolIdx].StartPosition - nextX) < tolerance)
            {
                nextSymbols.Add(_bottomSymbols[_currentBottomSymbolIdx++]);
            }

            return nextSymbols;
        }

        /// <summary>
        /// Gets all symbols in this measure organized by staff
        /// </summary>
        /// <returns>Tuple containing (top staff symbols, bottom staff symbols)</returns>
        public Tuple<List<Symbol>, List<Symbol>> GetAllSymbols()
        {
            return new Tuple<List<Symbol>, List<Symbol>>(_topSymbols, _bottomSymbols);
        }
    }
}
