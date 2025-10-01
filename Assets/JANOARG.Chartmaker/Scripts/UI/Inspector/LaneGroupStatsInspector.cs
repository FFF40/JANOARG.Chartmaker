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
        public TMP_Text TotalHitObjects;
        public TMP_Text Taps;
        public TMP_Text Catches;
        public TMP_Text Flickables;
        public TMP_Text Holds;
        
        private LaneGroup _lastLaneGroup;
        
        private void Update()
        {
            if (HightlightedLaneGroup == null)
            {
                LaneCount.text = "-";
                LaneGroupCount.text = "-";
                LaneCountRecursive.text = "-";
                MaxNestingCount.text = "-";
                TotalHitObjects.text = "-";
                Taps.text = "-";
                Catches.text = "-";
                Flickables.text = "-";
                Holds.text = "-";
                return;
            }
            
            // Only recalculate if the lane group changed
            if (_lastLaneGroup == HightlightedLaneGroup)
                return;
            
            // Incremented in CalculateRecursiveLaneCount
            int totalHitObjects = 0, 
                taps = 0, 
                catches = 0, 
                directionalFlickables = 0, 
                omniFlickables = 0,
                holds = 0;
            
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
            
            LaneCountRecursive.text = CalculateRecursiveLaneCount(groupName, chart, 
                ref totalHitObjects,ref taps, ref catches, ref directionalFlickables, ref omniFlickables, ref holds).ToString();
            
            TotalHitObjects.text = totalHitObjects.ToString();
            
            Taps.text = taps.ToString();
            Catches.text = catches.ToString();
            Flickables.text = $"{omniFlickables}/{directionalFlickables}";
            Holds.text = holds.ToString();
        }
        
        private int CalculateMaxNestingDepth(string groupName, Chart chart)
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
        
        private int CalculateRecursiveLaneCount(string groupName, Chart chart, 
            ref int totalHitObjects, ref int taps, ref int catches, ref int directionalFlickables, ref int omniFlickables, ref int holds)
        {
            int totalLanes = 0;
    
            // Count direct child lanes
            foreach (var lane in chart.Lanes)
            {
                if (lane.Group != groupName) 
                    continue;

                totalLanes++;
                totalHitObjects += lane.Objects.Count;
                foreach (var obj in lane.Objects)
                {
                    switch (obj.Type)
                    {
                        case HitObject.HitType.Normal:
                            taps++;

                            break;
                        case HitObject.HitType.Catch:
                            catches++;

                            break;
                    }
    
                    if (obj.Flickable)
                    {
                        if (float.IsFinite(obj.FlickDirection))
                            directionalFlickables++;
                        else
                            omniFlickables++;
                    }
    
                    if (obj.Length > 0)
                        holds++;
                }
            }
    
            // Recursively count lanes in child groups
            foreach (var group in chart.Groups)
                if (group.Group == groupName)
                    totalLanes += CalculateRecursiveLaneCount(group.Name, chart, ref totalHitObjects,ref taps, ref catches, ref directionalFlickables, ref omniFlickables, ref holds);
    
            return totalLanes;
        }

    }
    
}