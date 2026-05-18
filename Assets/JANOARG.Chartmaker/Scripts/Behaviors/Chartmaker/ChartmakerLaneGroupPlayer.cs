using JANOARG.Shared.Data.ChartInfo;
using UnityEngine;

namespace JANOARG.Chartmaker.Behaviors.Chartmaker
{
    public class ChartmakerLaneGroupPlayer : MonoBehaviour
    {
        public LaneGroupManager CurrentGroup;

        public void UpdateObjects(LaneGroupManager group)
        {
            CurrentGroup = group;

            // Apply only this group's own local transform — nesting is handled
            // by the GameObject hierarchy itself, so we must NOT use FinalPosition/FinalRotation
            // (those have the parent chain baked in already).
            transform.SetLocalPositionAndRotation(
                group.CurrentGroup.Position,
                Quaternion.Euler(group.CurrentGroup.Rotation)
            );
        }
    }
}
