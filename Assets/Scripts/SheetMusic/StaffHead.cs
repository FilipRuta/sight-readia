using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace SheetMusic
{
    public class StaffHead
    {
        private readonly GameObject _symbol;
        private GameObject _staffHeadObject;

        public Clef TopClef { get; }

        public Clef BottomClef { get; }

        /// <summary>
        /// The key signature, represented as number of fifths (positive for sharps, negative for flats).
        /// </summary>
        public int Fifths { get; }

        public float StaffHeadWidth { get; private set; }

        public (int beats, int beatType) TimeSignature { get; }

        
        /// <summary>
        /// Maps accidental types to their vertical positions on the staff.
        /// Order and positions of accidentals from left to right where 0 is above the topmost staff line.
        /// Stated for a treble clef on second line (from bottom).
        /// </summary>
        private static readonly Dictionary<AccidentalType, int[]> FifthsPositions = new() 
        {
            { AccidentalType.FLAT, new[] { 5, 2, 6, 3, 7, 4, 8 } },
            { AccidentalType.SHARP, new[] { 1, 4, 0, 3, 6, 2, 5 } }
        };

        /// <summary>
        /// Maps clef types to their semitone offsets for positioning key signatures.
        /// </summary>
        private static readonly Dictionary<NoteStep, int> ClefToFifthsOffsetSemitones = new()
        {
            { NoteStep.G, 0 },
            { NoteStep.C, 1 },
            { NoteStep.F, 2 }
        };

        /// <summary>
        /// Creates a new StaffHead instance.
        /// </summary>
        /// <param name="topClef">The clef for the top staff.</param>
        /// <param name="bottomClef">The clef for the bottom staff.</param>
        /// <param name="fifths">The number of fifths for the key signature (positive for sharps, negative for flats).</param>
        /// <param name="timeSignature">The time signature as a tuple of (beats, beatType).</param>
        public StaffHead(Clef topClef,Clef bottomClef, int fifths, ValueTuple<int, int> timeSignature)
        {
            _symbol = MainManager.Instance.PrefabLoader.GetPrefab("Symbol");
            TopClef = topClef;
            BottomClef = bottomClef;
            Fifths = fifths;
            TimeSignature = timeSignature;
        }

        /// <summary>
        /// Draws single time signature symbol.
        /// </summary>
        /// <param name="xPos">The x-position to draw the time signature.</param>
        /// <param name="yPos">The y-position as an offset from the top line</param>
        /// <param name="name">Name of the symbol gameobject</param>
        /// <param name="symbolNum">Number of the time signature symbol</param>
        /// <param name="parent">The transform to parent the time signature symbol to.</param>
        private void DrawTimeSignatureSymbol(float xPos, float yPos, string name, int symbolNum, Transform parent)
        {
            const float timeSymbolYPos = -Constants.LineSpacing;
            var timeSymbol = GameObject.Instantiate(
                _symbol,
                new Vector3(xPos, timeSymbolYPos + yPos, 0),
                Quaternion.identity,
                parent.transform
            );
            timeSymbol.name = name;
            var upperText = timeSymbol.GetComponent<TextMeshPro>();

            // Convert int to char for case where the time signatures containing 2 digits
            upperText.text = string.Concat(
                symbolNum.ToString()
                    .Select(digit => (char)(SymbolsMapping.TimeSignatureSymbolOne[0] + (digit - '0')))
            );
        }
        
        /// <summary>
        /// Draws the time signature on the staff.
        /// </summary>
        /// <param name="xPos">The x-position to draw the time signature.</param>
        /// <param name="parent">The transform to parent the time signature to.</param>
        private void DrawTimeSignature(float xPos, Transform parent)
        {
            if (TimeSignature is { beats: 0, beatType: 0 })
                return;
            DrawTimeSignatureSymbol(xPos, 0, "Beat", TimeSignature.beats, parent);
            DrawTimeSignatureSymbol(xPos, -2 * Constants.LineSpacing, "BeatType", TimeSignature.beatType, parent);
        } 
    
        /// <summary>
        /// Draws the complete staff head with clef, key signature, and time signature.
        /// </summary>
        /// <param name="xPos">The starting x-position for drawing.</param>
        /// <param name="parent">The transform to parent the staff head to.</param>
        /// <param name="prevStaffHead">The previous staff head, used to determine if elements need to be redrawn.</param>
        /// <param name="staffPosition">Indicates whether this is for the top or bottom staff.</param>
        public void Draw(float xPos, Transform parent, StaffHead prevStaffHead, StaffPosition staffPosition)
        {
            var clef = staffPosition == StaffPosition.TOP ? TopClef : BottomClef;
            var prevClef = staffPosition == StaffPosition.TOP ? prevStaffHead?.TopClef : prevStaffHead?.BottomClef;
        
            _staffHeadObject = new GameObject("StaffHead") {transform = { parent = parent}};
            var alwaysShowStaffHead = MainManager.Instance.GameManager.AlwaysShowStaffHead;
            
            var shouldDrawClef = alwaysShowStaffHead || prevClef != clef;
            var shouldDrawKeySig = alwaysShowStaffHead || prevStaffHead == null || Fifths != prevStaffHead.Fifths;
            var shouldDrawTimeSig = alwaysShowStaffHead || prevStaffHead == null || TimeSignature != prevStaffHead.TimeSignature;
            
            var xStartPos = xPos;
            if (shouldDrawClef)
            {
                xPos = clef.Draw(xPos, _staffHeadObject.transform);
            }
            if (shouldDrawKeySig)
            {
                const float spaceAboveTopStaffLineYPosition = Constants.LineSpacing / 2;
                var accidentalType = (AccidentalType)(int)Mathf.Sign(Fifths); // Convert sign of fifths to enum type
                float clefOffset = 0;
                if (clef != null && ClefToFifthsOffsetSemitones.TryGetValue(clef.Step, out var offsetSemitone))
                    clefOffset = offsetSemitone;

                for (var idx = 0; idx < Mathf.Abs(Fifths); ++idx)
                {
                    var keyAccidental = GameObject.Instantiate(
                        _symbol,
                        new Vector3(
                            xPos,
                            spaceAboveTopStaffLineYPosition -
                            Constants.LineSpacing / 2 * (FifthsPositions[accidentalType][idx] + clefOffset),
                            0
                        ),
                        Quaternion.identity
                    );
                    keyAccidental.transform.SetParent(_staffHeadObject.transform, false);
                    keyAccidental.name = "Accidental";
                    xPos += Constants.KeySignaturePadding;
                    var keyText = keyAccidental.GetComponent<TextMeshPro>();
                    keyText.text = SymbolsMapping.Accidentals[accidentalType];
                }
            
                xPos += Constants.AfterKeySigntaurePadding;
            }
            if (shouldDrawTimeSig)
            {
                DrawTimeSignature(xPos, _staffHeadObject.transform);
                xPos += Constants.AfterTimeSignaturePadding;
            }
            StaffHeadWidth = xPos - xStartPos;
            StaffHeadWidth += Constants.AfterStaffHeadPadding;
        }
    }
}
