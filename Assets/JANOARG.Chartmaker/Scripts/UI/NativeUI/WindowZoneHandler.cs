using UnityEngine;
using UnityEngine.EventSystems;

namespace JANOARG.Chartmaker.UI.NativeUI
{
    public class WindowZoneHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public WindowZone ZoneType;

        public void OnPointerEnter(PointerEventData eventData)
        {
            // BorderlessWindow.CurrentWindowZone = ZoneType;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // BorderlessWindow.CurrentWindowZone = WindowZone.Client;
        }
    }
}
