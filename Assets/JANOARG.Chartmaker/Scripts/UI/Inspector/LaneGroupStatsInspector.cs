using System.Collections;
using System.Collections.Generic;
using JANOARG.Shared.Data.ChartInfo;
using TMPro;
using UnityEngine;

namespace JANOARG.Chartmaker.UI.Inspector
{
    public class LaneGroupStatsInspector : MonoBehaviour
    {
        public LaneGroup HightlightedLaneGroup;
        [Header( "LaneGroup Stats" )]
        public TMP_Text LaneCount;
        public TMP_Text LaneGroupCount;
        public TMP_Text LaneCountRecursive;
        public TMP_Text MaxNestingCount;
        [Header( "Hit Objects" )]
        public HitObjectCountDisplay HitObjectCountDisplay;

        private LaneGroup _lastLaneGroup;

        void Start()
        {
            UpdateStats();
        }
        
        void UpdateStats()
        {
            if (HightlightedLaneGroup == null)
            {
                LaneCount.text = "-";
                LaneGroupCount.text = "-";
                LaneCountRecursive.text = "-";
                MaxNestingCount.text = "-";
                HitObjectCountDisplay.UpdateStatsIndeterminate();
                return;
            }
            
            // Only recalculate if the lane group changed
            if (_lastLaneGroup == HightlightedLaneGroup)
                return;
            
            _lastLaneGroup = HightlightedLaneGroup;
        
            var chart = Behaviors.Chartmaker.Chartmaker.main.CurrentChart;
            string groupName = HightlightedLaneGroup.Name;
        
            int laneCount = 0;
            int laneGroupCount = 0;
        
            // Single pass for lanes
            foreach (var lane in chart.Lanes)
            {
                if (lane.Group == groupName)
                {
                    laneCount++;
                }
            }

            // Single pass for groups
            foreach (var group in chart.Groups)
            {
                if (group.Group == groupName)
                    laneGroupCount++;
            }
        
            LaneCount.text = laneCount.ToString();
            LaneGroupCount.text = laneGroupCount.ToString();
            MaxNestingCount.text = CalculateMaxNestingDepth(groupName, chart).ToString();
            LaneCountRecursive.text = CalculateRecursiveLaneCount(groupName, chart).ToString();

            HitObjectCountDisplay.UpdateStats(TraverseRecursiveLaneHitObjects(groupName, chart));
        }
        
        private static int CalculateMaxNestingDepth(string groupName, Chart chart)
        {
            // Find all direct children of this group
            bool hasChildren = false;
            int maxChildDepth = 0;
            
            // Check child groups
            foreach (var laneGroup in chart.Groups)
            {
                if (laneGroup.Group == groupName)
                {
                    hasChildren = true;
                    int childDepth = CalculateMaxNestingDepth(laneGroup.Name, chart);
                    if (childDepth > maxChildDepth)
                        maxChildDepth = childDepth;
                }
            }
    
            // Check child lanes (leaf nodes)
            foreach (var lane in chart.Lanes)
            {
                if (lane.Group == groupName)
                {
                    hasChildren = true;
                    // Lanes are leaf nodes, depth is 0
                }
            }
    
            // If this group has children, depth is 1 + max child depth
            // Otherwise it's a leaf with depth 0
            return hasChildren ? 1 + maxChildDepth : 0;
        }
        
        private static int CalculateRecursiveLaneCount(string groupName, Chart chart)
        {
            int totalLanes = 0;
    
            // Count direct child lanes
            foreach (var lane in chart.Lanes)
            {
                if (lane.Group != groupName) 
                    continue;

                totalLanes++;
            }
    
            // Recursively count lanes in child groups
            foreach (var group in chart.Groups)
                if (group.Group == groupName)
                    totalLanes += CalculateRecursiveLaneCount(group.Name, chart);
    
            return totalLanes;
        }
        
        private static IEnumerable<HitObject> TraverseRecursiveLaneHitObjects(string groupName, Chart chart)
        {
            foreach (var lane in chart.Lanes)
            {
                if (lane.Group != groupName) 
                    continue;
                
                foreach (var obj in lane.Objects)
                    yield return obj;
            }
    
            foreach (var group in chart.Groups)
                if (group.Group == groupName)
                    foreach (var obj in TraverseRecursiveLaneHitObjects(group.Name, chart))
                        yield return obj;

        }

    }
    
}