using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Chartmaker.Utils;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace JANOARG.Chartmaker.Editor
{
	public class GizmoIconUtility 
	{
		[DidReloadScripts]
		static GizmoIconUtility()
		{
			EditorApplication.projectWindowItemOnGUI = ItemOnGUI;
		}

		static void ItemOnGUI(string guid, Rect rect)
		{
			string assetPath = AssetDatabase.GUIDToAssetPath(guid);

			Object obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

			if (rect.height > rect.width)
				rect.height = rect.width;
			else
				rect.width = rect.height;
        
			if (Mathf.Approximately(rect.height, 16)) 
				rect.x += 3;

			switch (obj)
			{
				case ExternalPlayableSong:
					EditorGUI.DrawRect(rect, Color.black); break;
			
				case ExternalChart item:
				{
					EditorGUI.DrawRect(rect, Color.white);

					GUIStyle diffStyle = new GUIStyle("label");
					diffStyle.alignment = TextAnchor.MiddleCenter;
					diffStyle.normal.textColor = Color.black;
					diffStyle.fontSize = Mathf.RoundToInt(rect.height / 2);

					GUI.Label(rect, item.Data.DifficultyLevel, diffStyle);
					break;
				}
			}
		}
	}
}