
using System;
using UnityEngine;

namespace JANOARG.Shared.Data.Chartmaker
{
    [Serializable]
    public struct RecentSong
    {
        public string Path;
        public string IconPath;
        public string SongName;
        public string SongArtist;
        public Color  BackgroundColor;
        public Color  InterfaceColor;
    }
}