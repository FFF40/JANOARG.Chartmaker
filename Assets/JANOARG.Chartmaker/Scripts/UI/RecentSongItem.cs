using System;
using JANOARG.Chartmaker.UI.Tooltip;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JANOARG.Chartmaker.UI
{
    public class RecentSongItem : MonoBehaviour
    {
        public RightClickButton Button;
        public RawImage         Icon;
        public TooltipTarget    Tooltip;
        public TMP_Text         SongArtistLabel;
        public TMP_Text         SongNameLabel;
        public Graphic          Background;
    }
}