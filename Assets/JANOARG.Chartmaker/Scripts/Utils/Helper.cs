using System;
using System.Collections;
using System.IO;
using JANOARG.Shared.Data.ChartInfo;
using UnityEngine;

namespace JANOARG.Chartmaker.Utils
{
    public static class Helper 
    {
        public static bool IsHierarchyObject(object obj)
        {
            return obj 
                       is PlayableSong
                       or Cover or CoverLayer
                       or Chart 
                       or CameraController or Palette
                       or LaneStyle or HitStyle 
                       or LaneGroup or Lane
                   || (obj is IList list && IsHierarchyObject(list[0]))
                ;
        }

        public static string GetDataFolder() 
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

            if (string.IsNullOrEmpty(path)) 
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                if (string.IsNullOrEmpty(path)) path = Path.GetDirectoryName(Application.dataPath);
                else path += Path.Combine(path, "JANOARG Chartmaker");
            }
            else path = Path.Combine(path, "JANOARG Chartmaker");

            return path;
        }

        public static string GetSongFolder() 
        {
            return Path.Combine(GetDataFolder(), "Songs");
        }

        public static string GetRenderFolder() 
        {
            return Path.Combine(GetDataFolder(), "Renders");
        }
    }
}