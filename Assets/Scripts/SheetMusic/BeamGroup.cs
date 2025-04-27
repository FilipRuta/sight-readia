using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SheetMusic
{
    public class BeamGroup
    {
        private readonly List<Note> _notes = new ();
        private bool _stemDown;
        private int _maxBeamCount;
        
        // Beam line direction
        private float _beamSlope;
        private float _beamIntercept;

        private GameObject _beamGroupObject;        
        private struct BeamEndpoints
        {
            public Vector3 Start;
            public Vector3 End;
        }

        /// <summary>
        /// Adds a note to the beam group.
        /// </summary>
        /// <param name="note">The note to add.</param>
        public void AddNote(Note note)
        {
            _notes.Add(note);
        }
        
        /// <summary>
        /// Retrieves the list of notes in the beam group.
        /// </summary>
        /// <returns>List of notes.</returns>
        public List<Note> GetNotes()
        {
            return _notes;
        }

        /// <summary>
        /// Calculates the total width of the beam group based on note durations.
        /// </summary>
        /// <returns>Total width of the beam group.</returns>
        public float GetTotalWidth()
        {
            return _notes.Sum(note => note.GetWidthBasedOnDuration());
        }
        
        /// <summary>
        /// Gets the maximum number of beams required for this group.
        /// </summary>
        /// <returns>Maximum beam count.</returns>
        private int GetMaxBeamCount()
        {
            return _notes.Max(note => note.Beams?.Count ?? 0);
        }

        /// <summary>
        /// Draws the beam group with notes and connecting beams.
        /// </summary>
        /// <param name="xStartPos">The starting X position for drawing.</param>
        /// <param name="parent">The parent transform to attach the beam group.</param>
        public void Draw(float xStartPos, Transform parent)
        {
            _beamGroupObject = new GameObject("BeamGroup"){transform = { parent = parent}};
            if (_notes.Count == 0) return;

            // Currently all stems should have same direction within beam group
            _stemDown = _notes.First().IsStemDown;
            _maxBeamCount = GetMaxBeamCount();
            
            var notePositions = new Dictionary<Note, float>();
            foreach (var note in _notes)
            {
                var startPosition = note.GetStartPositionInMeasure(); 
                note.Draw(startPosition, _beamGroupObject.transform);
                notePositions[note] = startPosition;
            }
            
            // Calculate beam slope and optimal stem lengths
            CalculateBeamSlope();
            CalculateStemLengths(notePositions);
            
            // Calculate and draw beam lines
            DrawBeamLines();
        }

        /// <summary>
        /// Calculates the slope of the beam to maintain a visually consistent direction.
        /// </summary>
        private void CalculateBeamSlope()
        {
            if (_notes.Count == 0)
            {
                Debug.LogWarning("BeamGroup has no notes");
                return;
            }
            var leftNote = _notes.First();
            var rightNote = _notes.Last();
            
            var leftNotePos = GetNoteOrChordNotePosition(leftNote);
            var rightNotePos = GetNoteOrChordNotePosition(rightNote);
            
            var width = rightNotePos.x - leftNotePos.x;
            var slope = width > 0 ? (rightNotePos.y - leftNotePos.y) / width : 0;
            
            // Limit extreme slopes
            const float maxSlope = Constants.BeamSlopeLimit;
            _beamSlope = Mathf.Clamp(slope, -maxSlope, maxSlope);
            _beamIntercept = leftNotePos.y - _beamSlope * leftNotePos.x;
        }
        
        /// <summary>
        /// Adjusts the stem lengths of notes based on the calculated beam slope.
        /// </summary>
        private void CalculateStemLengths(Dictionary<Note, float> notePositions)
        {
            const float baselineStemLength = Constants.StemLength * Constants.LineSpacing;

            foreach (var (note, xPos) in notePositions)
            {
                var y = _beamSlope * xPos + _beamIntercept;
                note.SetStemEndPosition(new Vector3(xPos, y + (_stemDown ? -1 : 1) * baselineStemLength));
            }
        }

        /// <summary>
        /// Gets the position of a note or the lowest/highest note in a chord.
        /// </summary>
        /// <param name="note">The note whose position is needed.</param>
        /// <returns>The position of the note or chord note.</returns>
        private Vector3 GetNoteOrChordNotePosition(Note note)
        {
            if (!note.IsChord)
                return note.GetWorldPosition();
            return _stemDown ? note.GetLowestChordNote()!.GetWorldPosition() : note.GetHighestChordNote()!.GetWorldPosition();
        }
        
        /// <summary>
        /// Draws the beam lines connecting notes.
        /// </summary>
        private void DrawBeamLines()
        {
            // For each beam level, determine which notes need connecting
            for (var beamLevel = 0; beamLevel < _maxBeamCount; beamLevel++)
            {
                var beamSegments = CalculateBeamSegments(beamLevel);
                
                foreach (var segment in beamSegments)
                {
                    DrawBeamSegment(segment, beamLevel);
                }
            }
        }

        /// <summary>
        /// Identifies beam segments to be drawn based on beam level.
        /// </summary>
        /// <param name="beamLevel">The current beam level being processed.</param>
        /// <returns>A list of beam segments with start and end positions.</returns>
        private List<BeamEndpoints> CalculateBeamSegments(int beamLevel)
        {
            var segments = new List<BeamEndpoints>();
            Note currentStart = null;
            
            foreach (var note in _notes)
            {
                var beams = note.Beams;
                
                // Skip if note doesn't have enough beams for this level
                if (beams == null || beamLevel >= beams.Count)
                    continue;
                
                var beamValue = beams[beamLevel];
                
                switch (beamValue)
                {
                    case BeamValues.BEGIN:
                        currentStart = note;
                        break;
                    case BeamValues.CONTINUE:
                        // Continue existing beam
                        break;
                    case BeamValues.END when currentStart != null:
                        // End of beam segment
                        segments.Add(new BeamEndpoints
                        {
                            Start = currentStart.GetStemEndPosition(),
                            End = note.GetStemEndPosition(),
                        });
                        currentStart = null;
                        break;
                    case BeamValues.FORWARD_HOOK or BeamValues.BACKWARD_HOOK:
                    {
                        var stemTop = note.GetStemEndPosition();
                        var hookLength = new Vector3(1, _beamSlope, 0).normalized;
                        hookLength *= note.GetWidthBasedOnDuration() * Constants.BeamHookLengthMultiplier;
                        
                        segments.Add(new BeamEndpoints
                        {
                            Start = stemTop,
                            End = stemTop + (beamValue is BeamValues.FORWARD_HOOK ? 1 : -1) * hookLength,
                        });
                        break;
                    }
                }
            }
            return segments;
        }

        /// <summary>
        /// Draws a beam segment between two points.
        /// </summary>
        /// <param name="segment">The segment defining the start and end points of the beam.</param>
        /// <param name="beamLevel">The level of the beam (affects vertical spacing).</param>
        private void DrawBeamSegment(BeamEndpoints segment, int beamLevel)
        {
            var beamLine = GameObject.Instantiate(MainManager.Instance.PrefabLoader.GetPrefab("Line"), _beamGroupObject.transform, true);
            beamLine.name = "BeamLine" + beamLevel;
            var lineRenderer = beamLine.GetComponent<LineRenderer>();
            
            // Set beam width
            lineRenderer.widthMultiplier = Constants.BeamWidth;
            lineRenderer.numCapVertices = 3;  // Add caps because sloped line cannot be vertically aligned with y axis
            
            // Adjust Y position based on beam level (each subsequent beam is slightly offset)
            const float beamSpacing = Constants.LineSpacing * 0.5f;
            var startPos = segment.Start + new Vector3(0, (_stemDown ? 1 : -1) * beamLevel * beamSpacing, 0);
            var endPos = segment.End + new Vector3(0, (_stemDown ? 1 : -1) * beamLevel * beamSpacing, 0);
            
            lineRenderer.SetPositions(new [] { startPos, endPos });
        }
    }
}