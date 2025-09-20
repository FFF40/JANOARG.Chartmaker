using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JANOARG.Chartmaker.UI.ContextMenu
{
    public class ContextMenuButton : Button
    {
        public UnityEvent onHover;
        public float      hoverDelay = 0.5f;

        Coroutine currentRoutine;

        IEnumerator HoverRoutine()
        {
            yield return new WaitForSecondsRealtime(hoverDelay);
            onHover.Invoke();
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            currentRoutine = StartCoroutine(HoverRoutine());
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            StopCoroutine(currentRoutine);
        }

        bool isMenuActive;

        public void SetMenuActive(bool active) 
        {
            isMenuActive = active;
            DoStateTransition(currentSelectionState, instant: false);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            if (isMenuActive) state = SelectionState.Highlighted;
            base.DoStateTransition(state, instant);
        }
    }
}
