using JANOARG.Chartmaker.UI.Modal.ModalTypes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JANOARG.Chartmaker.UI
{
    public class FileModalItem : MonoBehaviour
    {
        public Button         Button;
        public TMP_Text       Text;
        public Image          Icon;
        public FileModal      Parent;
        public FileModalEntry Entry;

        public void Select()
        {
            Parent.SelectItem(Entry);
        }
    }
}
