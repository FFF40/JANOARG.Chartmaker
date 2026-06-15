using UnityEngine;
using UnityEngine.EventSystems;
using JANOARG.Chartmaker.Utils.NativeAPI;

namespace JANOARG.Chartmaker.UI.NativeUI
{
    public class WindowZoneHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public WindowZone ZoneType;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (NativeWindow.IsApiAvailable) NativeWindow.MainWindow.SetHitTestZone((int)ZoneType);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (NativeWindow.IsApiAvailable) NativeWindow.MainWindow.SetHitTestZone((int)WindowZone.Client);
        }
    }
}
