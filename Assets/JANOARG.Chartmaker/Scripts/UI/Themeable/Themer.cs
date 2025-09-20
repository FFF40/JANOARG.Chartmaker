using System.Collections.Generic;
using UnityEngine;

namespace JANOARG.Chartmaker.UI.Themeable
{
    public class Themer : MonoBehaviour
    {
        public static Themer main;

        public Dictionary<string, Color> Keys;

        public List<Theme> Themes;

        public void InitTheme()
        {
            main = this;
       
            string name = Behaviors.Chartmaker.Chartmaker.Preferences.Theme;
            Theme theme = Themes.Find(x => x.name == name);
       
            if (!theme) 
                theme = Themes[0];
     
            Keys = Theme.ToDict(theme.Keys);
     
            SetAllColors();
        }

        private void SetAllColors()
        {
            foreach (Themeable themeable in FindObjectsOfType<Themeable>())
                themeable.SetColors();
        }
    }
}
