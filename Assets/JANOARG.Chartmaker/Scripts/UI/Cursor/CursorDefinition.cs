using System;
using System.Collections.Generic;
using JANOARG.Chartmaker.Utils.NativeAPI;
using UnityEngine;

namespace JANOARG.Chartmaker.UI.Cursor
{
    [CreateAssetMenu(fileName = "New Cursor", menuName = "Cursor Definition", order = 100)]
    public class CursorDefinition : ScriptableObject
    {
        public CursorStyle       CursorStyle;
        public Vector2           Pivot;
        public List<CursorFrame> Frames;
    }

    [Serializable]
    public class CursorFrame 
    {
        public Texture2D Texture;
        public float     Duration = 0.33f;
    }
}