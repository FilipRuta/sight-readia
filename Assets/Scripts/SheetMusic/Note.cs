using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

namespace SheetMusic
{
    public class Note : Symbol
    {
        private GameObject _accidentalObject;
        private GameObject _stemLineObject;
        private GameObject _noteNameObject;
        private List<Note> _chordNotes; // Ordered from the lowest pitch to highest

        public bool IsStemDown { get; }

        public List<BeamValues> Beams { get; }

        private GameObject _flagObject;
        public NoteStep Step { get; }

        public int Octave { get; }

        public AccidentalType? Accidental { get; }

        /// <summary>
        /// MIDI note number of this note with respect to the measure
        /// Altered by preceding notes, accidental or measure key  
        /// </summary>
        public int MidiCode { get; set; }
        
        public bool HasBeams => Beams is { Count: > 0 };

        public bool HasFlags => Constants.NoteToFlagsCount.ContainsKey(Type);

        public bool HasStem => Constants.NoteToFlagsCount.ContainsKey(Type) || Type is NoteType.HALF or NoteType.QUARTER;

        public bool IsEmptyNoteHead => !HasStem || Type == NoteType.HALF;
        
        /// <summary>
        /// Checks if this note holds chord notes
        /// </summary>
        public bool IsChord => _chordNotes is { Count: > 0 };
        
        /// <summary>
        /// Constructor for creating a Rest symbol
        /// </summary>
        /// <param name="duration">Duration of the rest in division units</param>
        /// <param name="startPosition">Starting position within the measure, measured in division units</param>
        /// <param name="type">Type of rest (whole, half, 16th, ...)</param>
        /// <param name="staff">Staff number (1 or 2)</param>
        /// <param name="step">Note step (C, D, E, F, G, A, B)</param>
        /// <param name="accidental">Accidental type (sharp, flat, natural)</param>
        /// <param name="octave">Octave number</param>
        /// <param name="stemDown">Is stem pointing down?</param>
        /// <param name="dotCount">Indicates the number of dots present</param>
        /// <param name="beams">List of beam values from the furthest beam to the closest beam to the note head</param>
        public Note(
            int duration, int startPosition, NoteType type, int staff, NoteStep step, AccidentalType? accidental,
            int octave, bool stemDown, int dotCount, List<BeamValues> beams
        ) : base(duration, startPosition, type, staff, dotCount)
        {
            Step = step;
            Octave = octave;
            Accidental = accidental;
            IsStemDown = stemDown;
            Beams = beams;
        }

        private static int StepDifferenceInSemitones((NoteStep step, int octave) a, (NoteStep step, int octave) b)
        {
            if (!Constants.NoteSequence.Contains(a.step) || !Constants.NoteSequence.Contains(b.step))
                Debug.LogError($"Invalid notes {a}, {b}");
            return (Constants.NoteSequence.IndexOf(a.step) - Constants.NoteSequence.IndexOf(b.step)) +
                   7 * (a.octave - b.octave);
        }

        /// <summary>
        /// Calculates the Y-position offset for this note on the staff based on clef and pitch
        /// </summary>
        /// <returns>Y-position offset in local coordinates</returns>
        private float GetOffsetPosition()
        {
            var clef = IsInTopStaff() ? Measure.StaffHead.TopClef : Measure.StaffHead.BottomClef;

            // Calculate offset between clef and note
            float offset = StepDifferenceInSemitones((Step, Octave), (clef.Step, clef.Octave));
            // offset is in semitones - LineSpacing equals 2 semitones
            offset *= Constants.LineSpacing / 2.0f;
            // top staff line has y=0, so offset by the clef line number inversed (musicxml has bottom line as 0)
            offset += clef.Line - 5;
            return offset;
        }

        /// <summary>
        /// Adds a note to this note to form a chord
        /// </summary>
        /// <param name="chordNote">The note to add to the chord</param>
        public void AddChordNote(Note chordNote)
        {
            if (_chordNotes == null)
                _chordNotes = new List<Note> { this }; // Add this note
            _chordNotes.Add(chordNote); // Add the chord note 
        }

        /// <summary>
        /// Orders chord notes by pitch from lowest to highest
        /// Must be called after the measure is initialized so the midiCodes are updated
        /// </summary>
        public void OrderChordNotes()
        {
            _chordNotes = _chordNotes.OrderBy(n => n.MidiCode).ToList();
        }

        /// <summary>
        /// Gets all notes in this chord including this note
        /// </summary>
        /// <returns>List of notes in the chord</returns>
        public List<Note> GetChordNotes()
        {
            if (!IsChord)
            {
                Debug.LogError("Not a chord note!");
                return null;
            }
            return new List<Note>(_chordNotes);
        }
        
        /// <summary>
        /// Gets the highest pitch note in the chord
        /// </summary>
        /// <returns>The highest pitch note</returns>
        public Note GetHighestChordNote()
        {
            if (!IsChord)
            {
                Debug.LogError("Not a chord note!");
                return null;
            }
            return _chordNotes.Last();
        }

        /// <summary>
        /// Gets the lowest pitch note in the chord
        /// </summary>
        /// <returns>The lowest pitch note</returns>
        public Note GetLowestChordNote()
        {
            if (!IsChord)
            {
                Debug.LogError("Not a chord note!");
                return null;
            }
            return _chordNotes.First();
        }
        
        /// <summary>
        /// Get X position of the note in the score
        /// For chord notes with adjacent notes returns position of right notehead for stem down and left for stem up 
        /// </summary>
        /// <returns>The lowest pitch note</returns>
        public float GetXPosition()
        {
            if (!IsChord)
            {
                return SymbolObject.transform.position.x;
            }
            // For chord notes there can be adjacent notes on the left or on the right of stem
            var chordNotesXCoords = _chordNotes.Select(n => n.GetWorldPosition().x);
            return IsStemDown ? chordNotesXCoords.Max() : chordNotesXCoords.Min();
        }

        /// <summary>
        /// Changes the color of a single note's visual elements
        /// </summary>
        /// <param name="color">Color to apply to the note elements</param>
        private void VisualizeSingleNote(Color color)
        {
            SymbolObject.GetComponent<TextMeshPro>().color = color;
        }

        /// <summary>
        /// Changes color of the note or all notes in the chord
        /// </summary>
        /// <param name="color">Color to apply</param>
        private void VisualizeNote(Color color)
        {
            List<Note> notes;
            if (MainManager.Instance.GameManager.TreatChordsAsIndividualNotes || !IsChord)
                notes = new List<Note> { this };
            else
                notes = _chordNotes;

            foreach (var note in notes)
            {
                note.VisualizeSingleNote(color);
            }
        }

        /// <summary>
        /// Resets the note's visual styling to default (black)
        /// </summary>
        public void ResetVisuals()
        {
            VisualizeNote(Color.black);
        }

        /// <summary>
        /// Highlights the note in yellow
        /// </summary>
        public void HighlightNote()
        {
            VisualizeNote(Color.yellow);
        }
        
        /// <summary>
        /// Get notes with respect to midi range of current input controller. 
        /// </summary>
        /// <param name="getNotesInside">If true get notes inside, if false get notes outside of range</param>
        /// <returns></returns>
        public List<Note> GetNotesWithRespectToDeviceRange(bool getNotesInside)
        {
            List<Note> notes;
            if (!IsChord)
                notes = new List<Note> { this };
            else
                notes = _chordNotes;

            bool IsInRange(Note n) => MainManager.Instance.PlayerManager.InputController.IsInDeviceRange(n.MidiCode);
            
            return notes.Where(n => getNotesInside == IsInRange(n)).ToList();
        }

        /// <summary>
        /// Gray out notes if not in device playable range
        /// </summary>
        private void GreyOutNotesOutOfDeviceRange()
        {
            foreach (var note in GetNotesWithRespectToDeviceRange(false))
            {
                note.VisualizeSingleNote(new Color(0f, 0f, 0f, 0.5f));
            }
        }


        /// <summary>
        /// Visualizes notes based on whether they were correctly played
        /// Green for correct notes, red for incorrect
        /// </summary>
        /// <param name="correctlyPlayed">Set of MIDI codes that were correctly played</param>
        public void VisualizeNotePlayedCorrectly(HashSet<int> correctlyPlayed)
        {
            List<Note> notes;
            if (MainManager.Instance.GameManager.TreatChordsAsIndividualNotes || !IsChord)
                notes = new List<Note> { this };
            else
                notes = _chordNotes;

            foreach (var note in notes)
            {
                if (correctlyPlayed.Contains(note.MidiCode))
                    note.VisualizeSingleNote(Color.green);
                else
                    note.VisualizeSingleNote(Color.red);
            }
        }

        /// <summary>
        /// Maps a staff line index to its corresponding note name and octave
        /// </summary>
        /// <param name="i">Staff line index (0=top line, 4=bottom line)</param>
        /// <returns>Tuple containing note name and octave</returns>
        private (NoteStep step, int octave) GetStaffLineNoteRepresentation(int i)
        {
            if (i is < 0 or > 4)
                throw new IndexOutOfRangeException("Invalid staff line index");
            // Get note name of staff line, 0 means topmost line, 4 means bottommost
            var clef = IsInTopStaff() ? Measure.StaffHead.TopClef : Measure.StaffHead.BottomClef;
            var clefStepIndex = Constants.NoteSequence.IndexOf(clef.Step);
            
            var semitoneOffset = (5 - i - clef.Line) * 2; // clef.Line has value 1 for lowest line, double for semitones
            var octaveOffset = Mathf.FloorToInt((clefStepIndex + semitoneOffset) / 7f) - Mathf.FloorToInt(clefStepIndex / 7f);
            var pitchOffset = (clefStepIndex + semitoneOffset + 7) % 7;

            return (Constants.NoteSequence[pitchOffset], clef.Octave + octaveOffset);
        }

        /// <summary>
        /// Counts how many ledger lines should be drawn for this note
        /// </summary>
        /// <returns>
        /// Number of ledger lines (positive = above staff, negative = below staff, 0 = no ledger lines)
        /// </returns>
        private int CountLedgerLines()
        {
            var topLineOffset = StepDifferenceInSemitones((Step, Octave), GetStaffLineNoteRepresentation(0));
            if (topLineOffset > 0)
                return topLineOffset / 2;

            var bottomLineOffset = StepDifferenceInSemitones((Step, Octave), GetStaffLineNoteRepresentation(4));
            if (bottomLineOffset < 0)
                return bottomLineOffset / 2;

            return 0;
        }

        /// <summary>
        /// Creates the visual representation of ledger lines for notes above or below the staff
        /// </summary>
        /// <param name="xPos">X-position where the ledger lines should be drawn</param>
        private void DrawLedgerLines(float xPos)
        {
            // negative -> below staff, positive above staff
            var ledgerLineCnt = CountLedgerLines();
            if (ledgerLineCnt == 0)
                return;
            
            var ledgerLinePositions = new Vector3[]
            {
                new(-1, 0, 0),
                new(1, 0, 0)
            };
            var linePrefab = MainManager.Instance.PrefabLoader.GetPrefab("Line");
            for (var i = 0; i < Mathf.Abs(ledgerLineCnt); i++)
            {
                var ledgerY = Mathf.Sign(ledgerLineCnt) > 0 ? (i + 1) : -(i + 5);
                var ledgerLine = GameObject.Instantiate(
                    linePrefab, new Vector3(xPos, ledgerY * Constants.LineSpacing, 0),
                    Quaternion.identity, SymbolObject.transform
                );
                var lineRenderer = ledgerLine.GetComponent<LineRenderer>();
                lineRenderer.SetPositions(ledgerLinePositions);
                lineRenderer.widthMultiplier = 3.0f;
                ledgerLine.name = "LedgerLine" + i;
            }
        }

        /// <summary>
        /// Updates the stem end position for beam connections
        /// </summary>
        /// <param name="newWorldPos">New world position for the stem end</param>
        public void SetStemEndPosition(Vector3 newWorldPos)
        {
            if (!_stemLineObject)
            {
                Debug.Log("No stem object found");
                return;
            }

            var lineRenderer = _stemLineObject.GetComponent<LineRenderer>();
            var oldPos = lineRenderer.GetPosition(0);
            lineRenderer.SetPosition(1,
                new Vector3(oldPos.x, _stemLineObject.transform.InverseTransformPoint(newWorldPos).y, oldPos.z));
        }

        /// <summary>
        /// Gets the current world position of the stem end
        /// </summary>
        /// <returns>World position of the stem end</returns>
        public Vector3 GetStemEndPosition()
        {
            if (!_stemLineObject)
            {
                Debug.Log("No stem object found");
                return Vector3.zero;
            }
            var lineRenderer = _stemLineObject.GetComponent<LineRenderer>();
            var pos = lineRenderer.GetPosition(1);
            return _stemLineObject.transform.TransformPoint(pos);
        }

        /// <summary>
        /// Get chord note orientations relative to stem, needed for adjacent notes
        /// </summary>
        /// <returns>List of ChordNoteOrientation contating values LEFT and RIGHT</returns>
        private List<ChordNoteOrientation> GetChordNoteOrientations()
        {
            if (!IsChord)
            {
                Debug.LogError("Not a chord note!");
                return null;
            }

            var notesOrientations = new List<ChordNoteOrientation>();
            var adjacentNoteGroups = new List<List<Note>>();
            Note lastNote = null;
            foreach (var note in _chordNotes)
            {
                if (lastNote == null ||
                    Math.Abs(StepDifferenceInSemitones((lastNote.Step, lastNote.Octave), (note.Step, note.Octave))) > 1)
                {
                    adjacentNoteGroups.Add(new List<Note> { note });
                }
                else
                {
                    adjacentNoteGroups.Last().Add(note);
                }
                lastNote = note;
            }

            var correctSide = IsStemDown ? ChordNoteOrientation.RIGHT : ChordNoteOrientation.LEFT;
            var oppositeSide = IsStemDown ? ChordNoteOrientation.LEFT : ChordNoteOrientation.RIGHT;
            foreach (var adjacentNoteGroup in adjacentNoteGroups)
            {
                if (adjacentNoteGroup.Count == 1)
                {
                    notesOrientations.Add(correctSide);
                }

                else if (adjacentNoteGroup.Count == 2)
                {
                    // For 2 notes place lower to the left side and upper to the right side
                    notesOrientations.Add(ChordNoteOrientation.LEFT);
                    notesOrientations.Add(ChordNoteOrientation.RIGHT);
                }
                else
                {
                    IEnumerable<ChordNoteOrientation> desiredOrientation;
                    if (adjacentNoteGroup.Count % 2 == 0)
                    {
                        // For even notehead groups always put lowest to the left and then alternate
                        desiredOrientation = Enumerable
                            .Repeat(
                                new List<ChordNoteOrientation>
                                    { ChordNoteOrientation.LEFT, ChordNoteOrientation.RIGHT },
                                adjacentNoteGroup.Count / 2).SelectMany(x => x);
                    }
                    else
                    {
                        // For odd notehead group put the lowest to the correct side and then alternate
                        desiredOrientation = Enumerable.Range(0, adjacentNoteGroup.Count)
                            .Select(i => i % 2 == 0 ? correctSide : oppositeSide);
                    }
                    notesOrientations.AddRange(desiredOrientation);
                }
            }

            return notesOrientations;
        }
        
        /// <summary>
        /// Creates the visual representation of the note stem and flags
        /// <param name="isLowestNoteheadOnOppositeSide">Indicates if the lowest note is on the opposite side
        /// of the stem</param>
        /// </summary>
        private void DrawStemAndFlags(bool isLowestNoteheadOnOppositeSide)
        {
            if (!HasStem)
                return;
            float startPos;
            float endPos;
            
            // Set parent note as a parent
            _stemLineObject = GameObject.Instantiate(
                MainManager.Instance.PrefabLoader.GetPrefab("Line"),
                SymbolObject.transform,
                false
            );
            _stemLineObject.name = "Stem";
            if (!IsChord)
            {
                startPos = 0;
                endPos = Constants.StemLength * Constants.LineSpacing * (IsStemDown ? -1 : 1);
            }
            else
            {
                var pitchDiffDistance =
                    _chordNotes.Last().GetOffsetPosition() - _chordNotes.First().GetOffsetPosition();
                // in local coordinates, 0 for `this` bottom note or pitchDiffDistance for topmost node
                startPos = IsStemDown ? pitchDiffDistance : 0;
                endPos = (IsStemDown ? 0 : pitchDiffDistance) +
                         (IsStemDown ? -1 : 1) * (Constants.LineSpacing * Constants.StemLength);
            }

            const float xStemAdjustment = 0.525f;
            const float yStemAdjustment = 0.12f;

            var lineRenderer = _stemLineObject.GetComponent<LineRenderer>();
            
            var xStemOffset = (IsStemDown && !isLowestNoteheadOnOppositeSide ? -1 : 1) * xStemAdjustment;
            var yStemOffset = (IsStemDown ? -1 : 1) * yStemAdjustment;
            _stemLineObject.transform.localPosition = new Vector3(xStemOffset, yStemOffset, 1);

            lineRenderer.SetPositions(
                new Vector3[]
                {
                    new(0, startPos, 0),
                    new(0, endPos, 0),
                }
            );

            if (Beams == null && HasFlags)
            {
                var xFlagOffset = (IsStemDown ? 0.503f : 0.455f);
                var yFlagOffset = (IsStemDown ? -0.12f : 0.68f);
                // has flags
                var flagCnt = Constants.NoteToFlagsCount[Type];
                _flagObject = GameObject.Instantiate(SymbolPrefab, _stemLineObject.transform, false);
                _flagObject.transform.localPosition = new Vector3(xFlagOffset, endPos + yFlagOffset, 0);
                _flagObject.name = "Flag";
                var text = _flagObject.GetComponent<TextMeshPro>();
                // flag codes +2 = add one more flag +1 inverts orientation of flag
                text.text = char.ConvertFromUtf32(SymbolsMapping.FlagFirst[0] + (flagCnt - 1) * 2 + Convert.ToInt32(IsStemDown));
            }
        }

        /// <summary>
        /// Returns the string representation of this note (e.g. "C#4")
        /// </summary>
        public override string ToString()
        {
            // Don't use the midi code because midi was altered by the current key/accidental to represent its true pitch
            var noteName = $"{Step}";
            if (Accidental == AccidentalType.SHARP)
                noteName += "#";
            else if (Accidental == AccidentalType.FLAT)
                noteName += "b";
            noteName += $"{Octave}";
            return noteName;
        }

        
        /// <summary>
        /// Creates the visual representation of an accidental
        /// </summary>
        /// <param name="xPos">X-position of the note in measure space</param>
        /// <param name="noteOffset">Y-offset of the note</param>
        private void DrawAccidental(float xPos, float noteOffset)
        {
            if (!Accidental.HasValue)
                return;
            if (!SymbolsMapping.Accidentals.ContainsKey(Accidental.Value))
            {
                Debug.LogWarning("Accidental not supported");
                return;
            }
            // render note accidental
            _accidentalObject = GameObject.Instantiate(SymbolPrefab, new Vector3(xPos - 1.0f, noteOffset, 0),
                Quaternion.identity, SymbolObject.transform);
            _accidentalObject.name = "Accidental" + Accidental;
            var accidentalText = _accidentalObject.GetComponent<TextMeshPro>();
            accidentalText.text = SymbolsMapping.Accidentals[Accidental.Value];
        }

        /// <summary>
        /// Creates the visual representation of the note name inside the note head
        /// </summary>
        private void DrawNoteName()
        {
            _noteNameObject = GameObject.Instantiate(SymbolPrefab, SymbolObject.transform, false);
            _noteNameObject.transform.position += new Vector3(0, 0, -1); // Put the text in front of the notehead 
            _noteNameObject.name = "NoteLetter_" + Step;
            
            var noteText = _noteNameObject.GetComponent<TextMeshPro>();
            noteText.text = Step.ToString();
            noteText.color = IsEmptyNoteHead ? Color.black : Color.white;
            noteText.fontStyle = FontStyles.Bold;
            noteText.fontSize = 6;
        }

        /// <summary>
        /// Creates the visual representation of a single note
        /// </summary>
        /// <param name="xPos">X-position of the note in measure space</param>
        /// <param name="orientation">Orientation of the notehead LEFT or RIGHT according to stem position
        /// (even for stemless notes) where adjacent notes can occur</param>
        /// <param name="parent">Parent transform for the note's GameObjects</param>
        private void DrawSingleNote(float xPos, ChordNoteOrientation orientation, Transform parent)
        {
            var noteOffset = GetOffsetPosition();

            // Put notes in front of the staff lines
            SymbolObject = GameObject.Instantiate(SymbolPrefab, new Vector3(xPos, noteOffset, -1), Quaternion.identity, parent);
            SymbolObject.name = "Note" + ToString();
            
            if (IsStemDown && orientation == ChordNoteOrientation.LEFT)
            {
                SymbolObject.transform.localPosition -= new Vector3(Constants.AdjacentNoteheadOffset, 0, 0);
            }
            else if (!IsStemDown && orientation == ChordNoteOrientation.RIGHT)
            {
                SymbolObject.transform.localPosition += new Vector3(Constants.AdjacentNoteheadOffset, 0, 0);
            }
            
            var text = SymbolObject.GetComponent<TextMeshPro>();
            text.text = SymbolsMapping.NoteHeads.GetValueOrDefault(Type, SymbolsMapping.FullNoteHead);

            if (MainManager.Instance.GameManager.ShowNoteNamesInNoteHeads)
                DrawNoteName();

            if (Accidental.HasValue)
                DrawAccidental(xPos, noteOffset);
            if (DotCount > 0)
                DrawDot(xPos, noteOffset);
        }

        /// <inheritdoc/>
        public override void Draw(float xPos, Transform parent)
        {
            var isLowestNoteheadOnOppositeSide = false;
            if (IsChord)
            {
                var chord = new GameObject("Chord") { transform = { parent = parent } };
                var noteOrientations = GetChordNoteOrientations();
                isLowestNoteheadOnOppositeSide = (IsStemDown && noteOrientations[0] == ChordNoteOrientation.LEFT) ||
                                                 (!IsStemDown && noteOrientations[0] == ChordNoteOrientation.RIGHT);
                for (var i = 0; i < _chordNotes.Count; i++)
                {
                    _chordNotes[i].DrawSingleNote(xPos, noteOrientations[i], chord.transform);
                    _chordNotes[i].DrawLedgerLines(xPos);
                }
            }
            else
            {
                var correctSide = IsStemDown ? ChordNoteOrientation.RIGHT : ChordNoteOrientation.LEFT;
                DrawSingleNote(xPos, correctSide, parent);
                DrawLedgerLines(xPos);
            }

            DrawStemAndFlags(isLowestNoteheadOnOppositeSide);
            GreyOutNotesOutOfDeviceRange();
        }

        /// <inheritdoc/>
        public override void Hide()
        {
            if (IsChord)
                SymbolObject.transform.parent.gameObject.SetActive(false);
            else
                SymbolObject.SetActive(false);

            IsHidden = true;
        }
    }
}