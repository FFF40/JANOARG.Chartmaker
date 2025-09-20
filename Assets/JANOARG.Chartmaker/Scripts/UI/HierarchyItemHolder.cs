using JANOARG.Chartmaker.Behaviors.Chartmaker;
using JANOARG.Chartmaker.Data.Chartmaker.Actions;
using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Chartmaker.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JANOARG.Chartmaker.UI
{
    public class HierarchyItemHolder : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Button         Button;
        public Image          Icon;
        public GameObject     SelectedBackground;
        public TMP_InputField NameField;
        public TMP_Text       Text;
        public LayoutElement  IndentBox;
        public Button         ExpandButton;
        public RectTransform  ExpandIcon;

        public HierarchyItem Target;

        bool isNameFieldDirty;

        public void Select()
        {
            HierarchyPanel.main.Select(Target);
        }

        public void RightClickSelect()
        {
            HierarchyPanel.main.RightClickSelect(Target, this);
        }

        public void SetItem(HierarchyItem item, int indent)
        {
            Target = item;
            gameObject.name = item.Name;
            Text.text = item.Name + " <alpha=#77>" + item.Subname;
            IndentBox.minWidth = 12 * indent + 24;
            UpdateExpand();
        }

        public void Rename()
        {
            switch (Target.Target)
            {
                case LaneStyle laneStyle: 
                    NameField.text = laneStyle.Name;
                    break;
            
                case HitStyle hitStyle: 
                    NameField.text = hitStyle.Name;
                    break;
            
                case LaneGroup group: 
                    NameField.text = group.Name;
                    break;
            
                case Lane lane: 
                    NameField.text = lane.Name;
                    break;
            
                default: 
                    return;
            }
            NameField.gameObject.SetActive(true);
            NameField.Select();
        
            isNameFieldDirty = false;
        }

        public void SetNameFieldDirty()
        {
            isNameFieldDirty = true;
        }

        public void DoneRename()
        {
            NameField.gameObject.SetActive(false);
        
            if (!isNameFieldDirty)
                return;
        
            switch (Target.Target)
            {
                case LaneGroup group:
                    if (string.IsNullOrEmpty(NameField.text))
                        return;
                
                    var action = new ChartmakerGroupRenameAction() 
                    {
                        From = group.Name,
                        To = InspectorPanel.main.GetNewGroupName(NameField.text.Trim(), group),
                    };
                
                    action.Redo();
                
                    Behaviors.Chartmaker.Chartmaker.main.History.AddAction(action);
                    Behaviors.Chartmaker.Chartmaker.main.OnHistoryUpdate();
                    break;
            
                case HitStyle: 
                case LaneStyle: 
                case Lane: 
                    Behaviors.Chartmaker.Chartmaker.main.SetItem(Target.Target, "Name", NameField.text.Trim());
                    break;
            
                default: 
                    return;
            }
            HierarchyPanel.main.UpdateHierarchy();
        }

        public void ToggleExpand()
        {
            HierarchyPanel.main.ToggleExpand(Target);
        }

        public void UpdateExpand()
        {
            ExpandIcon.localEulerAngles = Vector3.forward * (Target.Expanded ? 180 : -90);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            HierarchyPanel.main.OnItemBeginDrag(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            HierarchyPanel.main.OnItemDrag(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            HierarchyPanel.main.OnItemEndDrag(this, eventData);
        }
    }
}
