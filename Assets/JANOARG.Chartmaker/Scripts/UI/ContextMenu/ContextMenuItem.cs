using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JANOARG.Chartmaker.UI.ContextMenu
{
    public class ContextMenuItem : MonoBehaviour
    {
        public CanvasGroup       Group;
        public TMP_Text          ContentLabel;
        public TMP_Text          ShortcutLabel;
        public GameObject        CheckedIndicator;
        public GameObject        SubmenuIndicator;
        public ContextMenuButton Button;
        public Image             Icon;
    }
}
