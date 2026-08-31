using JANOARG.Shared.Data.ChartInfo;
using Unity.Collections;
using UnityEngine;

namespace JANOARG.Chartmaker.Behaviors.Chartmaker
{
    public class ChartmakerLaneGroupPlayer : MonoBehaviour
    {
        public LaneGroupManager CurrentGroup;

        [SerializeField] [ReadOnly]
        private ulong Uuid;

        public void UpdateObjects(LaneGroupManager group)
        {
            if (CurrentGroup != null && Uuid != CurrentGroup.Uuid)
                Uuid = CurrentGroup.Uuid;

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
