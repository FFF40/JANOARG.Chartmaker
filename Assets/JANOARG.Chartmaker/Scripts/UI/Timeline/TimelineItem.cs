using System.Collections;
using System.Collections.Generic;
using JANOARG.Chartmaker.Behaviors.Chartmaker;
using JANOARG.Shared.Data.ChartInfo;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JANOARG.Chartmaker.UI.Timeline
{
    public class TimelineItem : Selectable, IPointerDownHandler, IPointerClickHandler
    {
        public object Item;
        public Lane   Lane;
        public Image  Border;
        public Image  Icon;
        public Image  SelectedBorder;

        public virtual new void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                IList list = (InspectorPanel.main.CurrentTimestamp?.Count ?? 0) > 0 
                    ? InspectorPanel.main.CurrentTimestamp : InspectorPanel.main.CurrentObject is IList currentObjectList && currentObjectList.Contains(Item) 
                        ? currentObjectList : null;
           
                if (list != null)
                    TimelinePanel.main.BeginDragItem(list, eventData);
                else
                    TimelinePanel.main.BeginDragItem(new List<object>() { Item }, eventData);
            }
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (TimelinePanel.main.isDragged)
                return;
        
            switch (eventData.button)
            {
                case PointerEventData.InputButton.Left:
                    SelectItem();

                    break;
                case PointerEventData.InputButton.Right:
                    RightClickItem();

                    break;
            }
        }

        public void SetItem(object item, Lane lane = null)
        {
            Item = item;
            Lane = lane;
            SelectedBorder.gameObject.SetActive(InspectorPanel.main?.IsSelected(item) == true);
        }

        public void SelectItem()
        {
            if (PickerPanel.main.CurrentTimelinePickerMode == TimelinePickerMode.Delete)
                Behaviors.Chartmaker.Chartmaker.main.DeleteItem(Item);
            else
            {
                if (Lane != null)
                    InspectorPanel.main.SetObject(Lane);
            
                InspectorPanel.main.SetObject(Item);
            }
        }

        public void RightClickItem()
        {
            TimelinePanel.main.RightClickItem(this);
        }
    }
}
