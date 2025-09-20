using System.Collections.Generic;
using System.IO;
using JANOARG.Chartmaker.UI.Modal;
using JANOARG.Chartmaker.UI.Modal.ModalTypes;
using TMPro;
using UnityEngine.UI;

namespace JANOARG.Chartmaker.UI.Form.FormTypes
{
    public class FormEntryFile : FormEntry<string>
    {
        public TMP_Text                ValueLabel;
        public Button                  DrowpdownButton;
        public string                  HeaderText    = "Select a file...";
        public string                  SelectText    = "Select";
        public List<FileModalFileType> AcceptedTypes = new ();

        public new void Start() 
        {
            base.Start();
            Reset();
        }

        public void Reset()
        {
            ValueLabel.text = string.IsNullOrWhiteSpace(CurrentValue) ? "Select..." : Path.GetFileName(CurrentValue);
        }

        public void OpenList()
        {
            FileModal modal = ModalHolder.main.Spawn<FileModal>();
            modal.AcceptedTypes = AcceptedTypes;
            modal.HeaderLabel.text = HeaderText;
            modal.SelectLabel.text = SelectText;
            modal.OnSelect.AddListener(() =>
            {
                SetValue(modal.SelectedEntry.Path); Reset();
            });
        }
    }
}