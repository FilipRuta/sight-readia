using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SheetMusic
{
    public class MusicScore
    {
        public bool EndOfScore { get; private set; }
        private Measure CurrentMeasure { get; set; }
        public List<Note> UpcomingNotes { get; private set; } = new();
        public bool WaitForRelease { get; set; }
        public bool IsGrandStaff => _staffCount > 1;

        private readonly List<Measure> _measures;
        private List<Note> _trainingNoteSequence;
        private readonly int _staffCount;
        private readonly GameObject _scoreObject;
        private int _nextMeasureIdx;

        /// <summary>
        /// Initializes a new instance of the MusicScore class.
        /// </summary>
        /// <param name="measures">List of measures that make up the score.</param>
        /// <param name="staves">Number of staves in the score.</param>
        public MusicScore(List<Measure> measures, int staves)
        {
            _measures = measures;
            _staffCount = staves;
            _scoreObject = new GameObject("MusicScore");
        }

        public float GetXPositionOfUpcomingNote()
        {
            return UpcomingNotes.First().GetXPosition();
        }
        
        /// <summary>
        /// Renders the entire score, placing measures in sequence.
        /// </summary>
        /// <param name="renderGrandStaff">Whether to render a grand staff.</param>
        public void RenderWholeScore(bool renderGrandStaff)
        {
            var currentX = 0.0f;
            float maxStaffDist = 0;

            renderGrandStaff = renderGrandStaff && _staffCount > 1;
            
            // Draw score
            foreach (var measure in _measures)
            {
                var staffDist = measure.Draw(
                    currentX, _scoreObject.transform, renderGrandStaff
                );
                currentX += measure.MeasureWidth;
                maxStaffDist = Math.Max(maxStaffDist, staffDist);
            }
            if (renderGrandStaff)
            {
                _measures.ForEach(m => m.SetStaffDistance(maxStaffDist));
                _measures.First().DrawBrace();
            }

            // Find the Y range of the staff
            var (top, bottom) = _measures.First().GetStaffHeightRange(renderGrandStaff);
            var firstNoteXPosition = GetXPositionOfUpcomingNote();
            MainManager.Instance.PlayerManager.CameraController.FitCamera(
                0,
                Math.Min(_measures.First().MeasureWidth, firstNoteXPosition),
                top,
                bottom
            );
        }

        /// <summary>
        /// Renders the current measure and adjusts the camera.
        /// </summary>
        /// <param name="renderGrandStaff">Whether to render a grand staff.</param>
        /// <param name="noSymbols">If true, skips rendering symbols.</param>
        /// <param name="emptyMeasureWidthForNotes">Width of an empty measure for spacing notes.</param>
        private void RenderMeasure(bool renderGrandStaff, bool noSymbols = false, float emptyMeasureWidthForNotes = 0f)
        {
            renderGrandStaff = renderGrandStaff && _staffCount > 1;
            var staffDist = CurrentMeasure.Draw(
                0,
                _scoreObject.transform,
                renderGrandStaff,
                noSymbols,
                emptyMeasureWidthForNotes
            );
            CurrentMeasure.SetStaffDistance(staffDist);
            CurrentMeasure.DrawBrace();
            var (top, bottom) = CurrentMeasure.GetStaffHeightRange(renderGrandStaff);

            MainManager.Instance.PlayerManager.CameraController.FitCamera(
                0,
                CurrentMeasure.MeasureWidth,
                top, 
                bottom
            );
        }

        /// <summary>
        /// Prepares a training sequence by extracting notes from the current measure and shuffling them.
        /// </summary>
        /// <param name="repetitions">Number of times each note should be repeated in training.</param>
        private void PrepareMeasureTraining(int repetitions = 2)
        {
            if (EndOfScore)
                return;
        
            var measureSymbols = CurrentMeasure.GetAllSymbols();
            _trainingNoteSequence = measureSymbols.Item1.Concat((measureSymbols.Item2))
                .OfType<Note>()
                .SelectMany(x => x.IsChord ? x.GetChordNotes() : new List<Note> { x }) // Split onto separate chord notes
                .Where(x => MainManager.Instance.PlayerManager.InputController.IsInDeviceRange(x.MidiCode))
                .SelectMany(x => Enumerable.Repeat(x, repetitions)) // Repeat notes for training
                .OrderBy(x => Random.value).ToList(); // Randomly shuffle the measure notes
        }

        /// <summary>
        /// Updates the next training note. If all training notes were played, renders the measure.
        /// </summary>
        public void UpdateUpcomingTrainingNoteAndRender()
        {
            while (true)
            {
                if (EndOfScore)
                    return;

                if (_trainingNoteSequence == null || _trainingNoteSequence.Count == 0)
                {
                    CurrentMeasure?.DestroyMeasure();

                    AdvanceToNextMeasure();
                    PrepareMeasureTraining();
                    RenderMeasure(MainManager.Instance.GameManager.PlayGrandStaff, noSymbols: false);
                    continue;
                }

                UpcomingNotes = new List<Note> { _trainingNoteSequence[^1] };
                _trainingNoteSequence.RemoveAt(_trainingNoteSequence.Count - 1);
                return;
            }
        }

        /// <summary>
        /// Updates the next symbol to be played, skipping rests and notes that cannot be played with selected
        /// input controller.
        /// </summary>
        public void UpdateUpcomingSymbol()
        {
            while (!EndOfScore)
            {
                var shouldContinue = false;
    
                if (CurrentMeasure == null)
                    shouldContinue = true;

                else
                {
                    UpcomingNotes = CurrentMeasure.GetNextSymbols()?.OfType<Note>().ToList();
                
                    if (UpcomingNotes == null)
                        shouldContinue = true;
                    else
                    {
                        UpcomingNotes = UpcomingNotes.SelectMany(n => n.GetNotesWithRespectToDeviceRange(true)).ToList();
                    
                        if (UpcomingNotes.Count == 0)
                            shouldContinue = true;
                    }
                }
    
                if (!shouldContinue)
                {
                    break;
                }
    
                if (CurrentMeasure == null || UpcomingNotes == null)
                {
                    AdvanceToNextMeasure();
                }
            }
        }

        /// <summary>
        /// Deletes the score.
        /// </summary>
        public void DeleteScore()
        {
            GameObject.Destroy(_scoreObject.gameObject);
        }

        /// <summary>
        /// Advances to the next measure in the score.
        /// </summary>
        private void AdvanceToNextMeasure()
        {
            if (_nextMeasureIdx < _measures.Count)
            {
                CurrentMeasure = _measures[_nextMeasureIdx++];
                return;
            }

            UpcomingNotes = null;
            EndOfScore = true;
        }

        /// <summary>
        /// Resets the visual state of upcoming notes.
        /// </summary>
        public void ResetNextNoteVisuals()
        {
            foreach (var note in UpcomingNotes)
            {
                note.ResetVisuals();
            }
        }

        /// <summary>
        /// Visualizes played notes based on correctness.
        /// </summary>
        /// <param name="correctlyPlayedNotes">A set of MIDI note values that were played correctly.</param>
        public void VisualizePlayedNotes(HashSet<int> correctlyPlayedNotes)
        {
            foreach (var note in UpcomingNotes)
            {
                note.VisualizeNotePlayedCorrectly(correctlyPlayedNotes);
            }
        }

        /// <summary>
        /// Highlights the next notes to be played.
        /// </summary>
        public void HighlightNextNotes()
        {
            foreach (var note in UpcomingNotes)
            {
                note.HighlightNote();
            }
        }
    }
}