using JANOARG.Chartmaker.Behaviors.Chartmaker;
using JANOARG.Chartmaker.UI.Pickers.ObjectPicker;
using JANOARG.Shared.Data.ChartInfo;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.UI;

namespace JANOARG.Chartmaker.UI.Form.FormTypes
{
    public class FormEntryObject : FormEntry<object>
    {
        [DoNotSerialize]
        public int CurrentIndex; 
        public Button   LinkButton;
        public TMP_Text ValueLabel;
        public Button   DropdownButton;
        public Image    IconImage;

        public ObjectPickerType Type;

        public new void Start() 
        {
            base.Start();
            Reset();
        }

        public void SetType(ObjectPickerType type) 
        {
            Type = type;
        }

        public void LinkClick() 
        {
            if (CurrentValue != null) 
                InspectorPanel.main.SetObject(CurrentValue);
        }

        public void Reset()
        {
            string name;

            switch (Type)
            {
                case ObjectPickerType.LaneStyle:
                    IconImage.sprite = ObjectPicker.main.LaneStyleIcon;
                    ValueLabel.text = CurrentValue == null
                        ? "<i>None" : string.IsNullOrEmpty(name = ((LaneStyle)CurrentValue)?.Name)
                            ? "Lane Style " + CurrentIndex : name;
                    break;
            
                case ObjectPickerType.HitStyle:
                    IconImage.sprite = ObjectPicker.main.HitStyleIcon;
                    ValueLabel.text = CurrentValue == null
                        ? "<i>None" : string.IsNullOrEmpty(name = ((HitStyle)CurrentValue)?.Name)
                            ? "Hit Style " + CurrentIndex : name;
                    break;
            }
        }

        public void OpenList()
        {
            ObjectPicker.main.CurrentObject = CurrentValue;
            ObjectPicker.main.Type = Type;
            ObjectPicker.main.Open();
            ObjectPicker.main.OnSet = () => 
            {
                CurrentIndex = Type switch
                {
                    ObjectPickerType.LaneStyle => Behaviors.Chartmaker.Chartmaker.main.CurrentChart.Palette.LaneStyles.IndexOf((LaneStyle)ObjectPicker.main.CurrentObject),
                    ObjectPickerType.HitStyle => Behaviors.Chartmaker.Chartmaker.main.CurrentChart.Palette.HitStyles.IndexOf((HitStyle)ObjectPicker.main.CurrentObject),
                    _ => CurrentIndex
                };

                SetValue(ObjectPicker.main.CurrentObject);
                Reset();
            };
        }
    }
}