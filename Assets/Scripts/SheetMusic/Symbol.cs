using System;
using TMPro;
using UnityEngine;

namespace SheetMusic
{
    public abstract class Symbol
    {
        protected bool IsHidden;
        protected NoteType Type;
        protected int DotCount;
        
        protected Measure Measure;

        protected GameObject SymbolObject;
        protected GameObject DotObject;

        protected readonly GameObject SymbolPrefab;

        public int StartPosition { get; }
        public int Duration { get; }

        private readonly int _staff;
        
        /// <summary>
        /// Constructor for creating a musical symbol
        /// </summary>
        /// <param name="duration">Original duration of the symbol in division units</param>
        /// <param name="startPosition">Starting position within the measure, measured with division units</param>
        /// <param name="type">Type of note/rest symbol (whole, half, 16th, ...)</param>
        /// <param name="staff">Staff number (1 or 2)</param>
        /// <param name="dotCount">Indicates the number of dots present</param>
        protected Symbol(int duration, int startPosition, NoteType type, int staff, int dotCount)
        {
            StartPosition = startPosition;
            Duration = duration;
            Type = type;
            DotCount = dotCount;
            _staff = staff;
            SymbolPrefab = MainManager.Instance.PrefabLoader.GetPrefab("Symbol");
        }
    
        /// <summary>
        /// Sets the parent measure for this symbol
        /// </summary>
        /// <param name="measure">Measure containing this symbol</param>
        public void SetParentMeasure(Measure measure)
        {
            Measure = measure;
        }

        /// <summary>
        /// Checks if the symbol is located in the top staff
        /// </summary>
        /// <returns>True if symbol is in the top staff, false otherwise</returns>
        public bool IsInTopStaff()
        {
            return _staff == 1;
        }
    
        /// <summary>
        /// Retrieves the remapped start position of the symbol within its measure
        /// </summary>
        /// <returns>Start position as an integer</returns>
        public float GetStartPositionInMeasure()
        {
            if (Measure?.PositionRemapFunc == null)
            {
                throw new Exception("Position remapping not set in parent measure");
            }
            return Measure.PositionRemapFunc.Invoke(StartPosition);
        }
        
        /// <summary>
        /// Creates the visual representation of an augmentation dot
        /// </summary>
        /// <param name="xPos">X-position of the note in measure space</param>
        /// <param name="noteOffset">Y-offset of the note</param>
        protected void DrawDot(float xPos, float noteOffset)
        {
            xPos += Constants.NoteToDotPadding;
            for (var i = 0; i < DotCount; i++)
            {
                DotObject = GameObject.Instantiate(
                    SymbolPrefab, new Vector3(xPos, noteOffset, 0), Quaternion.identity, SymbolObject.transform
                );
                DotObject.name = "Dot";
                var text = DotObject.GetComponent<TextMeshPro>();
                text.text = SymbolsMapping.AugmentationDot;
                xPos += Constants.DotMutualPadding;
            }
        }
    
        /// <summary>
        /// Abstract method to hide the symbol
        /// </summary>
        public abstract void Hide();
        
        /// <summary>
        /// Abstract method to draw the symbol at a specific position
        /// Must be implemented by derived classes
        /// </summary>
        /// <param name="xPos">Horizontal position to draw the symbol (x in measure space - start of measure = 0)</param>
        /// <param name="parent">Parent transform for the symbol</param>
        public abstract void Draw(float xPos, Transform parent);

        /// <summary>
        /// Gets the world position of the symbol in the scene
        /// </summary>
        /// <returns>World position as a Vector3</returns>
        public Vector3 GetWorldPosition()
        {
            return SymbolObject.transform.position;
        }
        
        /// <summary>
        /// Calculates the visual width of the symbol based on its duration, duration is scaled based on duration remap
        /// function specified in Measure
        /// </summary>
        /// <returns>Width as a float</returns>
        public float GetWidthBasedOnDuration()
        {
            if (Measure?.DurationRemapFunc == null)
            {
                throw new Exception("Duration remapping not set in parent measure");
            }
            return Measure.DurationRemapFunc.Invoke(Duration);
        }
    }
}
