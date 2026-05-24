using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JANOARG.Chartmaker.UI.NativeUI;
using JANOARG.Chartmaker.Utils;
using JANOARG.Chartmaker.Utils.NativeAPI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace JANOARG.Chartmaker.UI.Cursor
{
    public class CursorChanger : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IEndDragHandler, IPointerUpHandler, IPointerExitHandler
    {
        public CursorStyle Style;

        bool IsPointerInside;
        bool IsPointerHolding;
        bool IsShowingCursor;

        public void OnPointerEnter(PointerEventData eventData)
        {
            IsPointerInside = true;
            if (!IsShowingCursor) CursorManager.main.PushCursor(Style);
            IsShowingCursor = true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsPointerHolding = true;
            if (!IsPointerInside) OnPointerEnter(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsPointerHolding = false;
            if (!IsPointerInside && IsShowingCursor)
            {
                CursorManager.main.PopCursor();
                IsShowingCursor = false;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsPointerInside) OnPointerUp(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            IsPointerInside = false;
            if (!IsPointerHolding && IsShowingCursor)
            {
                CursorManager.main.PopCursor();
                IsShowingCursor = false;
            }
        }

        public void OnDisable()
        {
            if (IsShowingCursor) CursorManager.main.PopCursor();
            IsShowingCursor = false;
        }
    }
}