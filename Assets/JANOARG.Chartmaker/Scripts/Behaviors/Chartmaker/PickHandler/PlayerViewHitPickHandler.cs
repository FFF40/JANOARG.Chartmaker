using JANOARG.Shared.Data.ChartInfo;
using UnityEngine;
using UnityEngine.EventSystems;

namespace JANOARG.Chartmaker.Behaviors.Chartmaker.PickHandler
{
    public class PlayerViewHitPickHandler : PlayerViewPickHandler
    {
        public ChartmakerHitPlayer Instance;

        public override bool Pick(PointerEventData eventData)
        {
            if (TimelinePanel.main.CurrentMode == TimelineMode.HitObjects)
            {
                ChartmakerLanePlayer lane = Instance.transform.parent.parent.GetComponent<ChartmakerLanePlayer>();
                if (lane && InspectorPanel.main.CurrentHierarchyObject == lane.CurrentLane.Original)
                {
                    InspectorPanel.main.SetObject(lane.CurrentLane.Original);
                }

                InspectorPanel.main.SetObject(Instance.CurrentHit.Original);

                return true;
            }

            return false;
        }
    }
}