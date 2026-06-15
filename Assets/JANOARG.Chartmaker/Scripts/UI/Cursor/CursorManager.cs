using System;
using System.Collections.Generic;
using JANOARG.Chartmaker.Utils.NativeAPI;
using UnityEngine;

namespace JANOARG.Chartmaker.UI.Cursor
{
    public class CursorManager : MonoBehaviour
    {
        public static CursorManager main;

        public PreferredCursorMode PreferredCursorMode;
        public List<CursorDefinition> CursorDefinitions;
        public Dictionary<CursorStyle, CursorDefinition> CursorDefinitionsDictionary = new();

        Stack<CursorStyle> cursorStack = new();
        CursorDefinition activeCustomCursor;
        float currentCursorFrameTime;
        int currentCursorFrame;


        public void Awake()
        {
            main = this;
        }

        public void Start()
        {
            PreferredCursorMode = Behaviors.Chartmaker.Chartmaker.Preferences.PreferredCursorMode;
            foreach (CursorDefinition cursor in CursorDefinitions)
            {
                CursorDefinitionsDictionary[cursor.CursorStyle] = cursor;
            }
            UpdateCursor();
        }

        public void Update()
        {
            if (activeCustomCursor && activeCustomCursor.Frames.Count > 1) 
            {
                currentCursorFrameTime += Time.unscaledDeltaTime;
                int lastFrame = currentCursorFrame;
                int a = 1000;
            
                while (currentCursorFrameTime >= activeCustomCursor.Frames[currentCursorFrame].Duration && a > 0)
                {
                    currentCursorFrameTime -= activeCustomCursor.Frames[currentCursorFrame].Duration;
                    currentCursorFrame = (currentCursorFrame + 1) % activeCustomCursor.Frames.Count;
                    a--;
                }
            
                if (lastFrame != currentCursorFrame)
                {
                    UnityEngine.Cursor.SetCursor(activeCustomCursor.Frames[currentCursorFrame].Texture, activeCustomCursor.Pivot, CursorMode.Auto);
                }
            }
        }
        public void PushCursor(CursorStyle style)
        {
            cursorStack.Push(style);
            UpdateCursor();
        }

        public void PopCursor()
        {
            cursorStack.TryPop(out _);
            UpdateCursor();
        }

        public void UpdateCursor()
        {
            CursorStyle currentCursor = cursorStack.Count > 0 ? cursorStack.Peek() : CursorStyle.Arrow;

            if (PreferredCursorMode != PreferredCursorMode.PreferCustom && NativeWindow.IsApiAvailable)
            {
                if (NativeWindow.MainWindow.SetCurrentCursor(currentCursor, PreferredCursorMode == PreferredCursorMode.PreferNativeBestEffort))
                {
                    activeCustomCursor = null;
                    return;
                }
            }
            
            activeCustomCursor = CursorDefinitionsDictionary.GetValueOrDefault(currentCursor, CursorDefinitionsDictionary[CursorStyle.Arrow]);
            UnityEngine.Cursor.SetCursor(activeCustomCursor.Frames[0].Texture, activeCustomCursor.Pivot, CursorMode.Auto);
        }

        public void SetPreferredCursorMode(PreferredCursorMode mode)
        {
            PreferredCursorMode = mode;
            UpdateCursor();
        }
    }
}
