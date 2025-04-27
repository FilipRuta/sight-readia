using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SheetMusic
{
    public class Clef
    {
        public NoteStep Step { get; }

        public int Line { get; }

        public int Octave => StepToOctave[Step];

        /// <summary>
        /// Maps each clef type (by note step) to its standard octave.
        /// </summary>
        private static readonly Dictionary<NoteStep, int> StepToOctave = new()
        {
            { NoteStep.C, 4 },
            { NoteStep.G, 4 },
            { NoteStep.F, 3 }
        };

        /// <summary>
        /// Creates a new Clef instance.
        /// </summary>
        /// <param name="step">The note step (C, G, or F) that defines the clef type.</param>
        /// <param name="line">The line on the staff where the clef is positioned.</param>
        public Clef(NoteStep step, int line)
        {
            Step = step;
            Line = line;
        }

        /// <summary>
        /// Draws the clef symbol on the staff.
        /// </summary>
        /// <param name="xPos">The starting x-position for drawing the clef.</param>
        /// <param name="parent">The transform to parent the clef GameObject to.</param>
        /// <returns>The updated x-position after drawing the clef.</returns>
        public float Draw(float xPos, Transform parent)
        {
            if (!SymbolsMapping.Clefs.ContainsKey(Step))
            {
                Debug.LogWarning("Unsupported clef");
                return xPos;
            }

            var invertedLinePos = Line - 5; // Inverted line numbering
            var clef = GameObject.Instantiate(
                MainManager.Instance.PrefabLoader.GetPrefab("Symbol"), 
                new Vector3(xPos, invertedLinePos, 0), 

                Quaternion.identity,
                parent.transform
            );
            clef.name = "Clef";
            var clefText = clef.GetComponent<TextMeshPro>();
            clefText.text = SymbolsMapping.Clefs[Step];
            xPos += Constants.AfterClefPadding;
            return xPos;
        }
    }
}