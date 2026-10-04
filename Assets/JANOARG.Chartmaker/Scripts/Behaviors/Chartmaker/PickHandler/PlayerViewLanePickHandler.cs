using UnityEngine;
using UnityEngine.EventSystems;

namespace JANOARG.Chartmaker.Behaviors.Chartmaker.PickHandler
{
    public class PlayerViewLanePickHandler : PlayerViewPickHandler
    {
        public ChartmakerLanePlayer Instance;

        public override bool Pick(PointerEventData eventData)
        {
            if (
                (TimelinePanel.main.CurrentMode == TimelineMode.Lanes || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                && eventData.button == PointerEventData.InputButton.Left
            )
            {
                InspectorPanel.main.SetObject(Instance.CurrentLane.Original);

                return true;
            }

            return false;
        }
    }
}