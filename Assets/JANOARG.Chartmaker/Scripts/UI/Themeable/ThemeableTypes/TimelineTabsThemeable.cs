using UnityEngine;

namespace JANOARG.Chartmaker.UI.Themeable.ThemeableTypes
{
    public class TimelineTabsThemeable : Themeable<GraphicParallelogram>
    {
        public JANOARG.Chartmaker.Behaviors.Chartmaker.TimelineMode Mode;

        public override void SetColors()
        {
            string key = "TimelineMode" + Mode;
            
            if (Themer.main.Keys.TryGetValue(key, out Color color)) 
                Target.color = color;
        }
    }
}