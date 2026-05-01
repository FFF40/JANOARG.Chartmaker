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

        public static string GetHomeFolder()
        {
            string path;
            
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass environment = new AndroidJavaClass("android.os.Environment"))
                using (AndroidJavaObject externalStorage = environment.CallStatic<AndroidJavaObject>("getExternalStorageDirectory"))
                {
                    if (externalStorage != null)
                    {
                        path = externalStorage.Call<string>("getAbsolutePath");
                    }
                    else
                    {
                        // Fallback if external storage unavailable
                        path = Application.persistentDataPath;
                    }
                }
            }
            else
            {
                // Fallback for editor and other platforms
                path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

                if (string.IsNullOrEmpty(path))
                    path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }

            return path;
        }
        
        public static string GetDataFolder() 
        {
            string path = Path.Combine(GetHomeFolder(), "JANOARG Chartmaker");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            return path;
        }

        public static string GetSongFolder() 
        {
            string path = Path.Combine(GetDataFolder(), "Songs");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            return path;
        }

        public static string GetRenderFolder() 
        {
            string path = Path.Combine(GetDataFolder(), "Renders");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            return path;
        }
    }
}