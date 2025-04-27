using TMPro;
using UnityEngine;

namespace SheetMusic
{
    public class Rest : Symbol
    {
        /// <summary>
        /// Constructor for creating a Rest symbol
        /// </summary>
        /// <param name="duration">Duration of the rest in division units</param>
        /// <param name="startPosition">Starting position within the measure, measured in division units</param>
        /// <param name="type">Type of rest (whole, half, 16th, ...)</param>
        /// <param name="staff">Staff number (1 or 2)</param>
        /// <param name="wholeMeasure">Indicates if the rest covers the entire measure</param>
        /// <param name="dotCount">Indicates the number of dots present</param>
        public Rest(int duration, int startPosition, NoteType type, int staff, bool wholeMeasure, int dotCount)
            : base(duration, startPosition, type, staff, dotCount)
        {
            if (wholeMeasure)
                Type = NoteType.WHOLE;
        }
   
        /// <inheritdoc/>
        public override void Draw(float xPos, Transform parent)
        {
            // Only whole rest has y=-1
            var yShift = Type == NoteType.WHOLE ? -1 : -2;
            SymbolObject = GameObject.Instantiate(SymbolPrefab, new Vector3(xPos, yShift, 0), Quaternion.identity, parent.transform);
            SymbolObject.name = "Rest " + Type;
            var text = SymbolObject.GetComponent<TextMeshPro>();
            text.text = SymbolsMapping.Rests[Type];
        }

        /// <inheritdoc/>
        public override void Hide()
        {
            SymbolObject.SetActive(false);
            IsHidden = true;
        }
    }
}
