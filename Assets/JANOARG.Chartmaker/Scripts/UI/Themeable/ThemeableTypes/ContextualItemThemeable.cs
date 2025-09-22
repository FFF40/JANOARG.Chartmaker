using UnityEngine;
using UnityEngine.UI;

namespace JANOARG.Chartmaker.UI.Themeable.ThemeableTypes
{
    [RequireComponent(typeof(Image))]
    public class ContextualItemThemeable : Themeable<Image>
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