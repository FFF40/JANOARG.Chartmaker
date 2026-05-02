using System;
using System.Collections.Generic;
using JANOARG.Chartmaker.UI;
using UnityEngine;
using JANOARG.Shared.Utils;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JANOARG.Chartmaker.Behaviors.Chartmaker
{

    public class HierarchyFiltersPanel : MonoBehaviour
    {
        public static HierarchyFiltersPanel main;

        public HierarchyFilterItem                                ItemSample;
        public RectTransform                                      ItemHolder;
        public Dictionary<HierarchyItemType, HierarchyFilterItem> Items = new();

        public List<HierarchyFilterSetting> Settings;

        public void Awake()
        {
            main = this;
            foreach (var setting in Settings)
            {
                HierarchyFilterItem item = Instantiate(ItemSample, ItemHolder);
                item.SetItem(setting);
                Items.Add(setting.Target, item);
            }
        }

        public void Reset() 
        {
            foreach (var item in Items.Values) 
                item.Reset();
        
            HierarchyPanel.main.UpdateHolders();
        }

        public void ResetPosition()
        {
            RectTransform rt = (RectTransform)transform;

            HierarchyPanel hierarchyPanel = HierarchyPanel.main;
            RectTransform hierarchyPanelRt = (RectTransform)hierarchyPanel.transform;

            UnityEngine.Debug.Log(hierarchyPanelRt.anchoredPosition.x + " " + hierarchyPanelRt.sizeDelta.x);

            rt.anchoredPosition *= new Vector2Frag(x: hierarchyPanelRt.anchoredPosition.x + hierarchyPanelRt.sizeDelta.x + 2);
        }

        public bool GetVisibility(HierarchyItemType type, HierarchyContext context) 
        {
            return context switch
            {
                HierarchyContext.Hierarchy    => Items[type].InHierarchyToggle.isOn,
                HierarchyContext.SearchResult => Items[type].InSearchResultToggle.isOn,
                _                             => false,
            };
        }
    }

    [Serializable]
    public class HierarchyFilterSetting 
    {
        public HierarchyItemType Target;
    
        public string Name;
        public Sprite Icon;
        public int    Indent;
    
        public bool InHierarchyToggleable    = true;
        public bool InHierarchyDefault       = true;
        public bool InSearchResultToggleable = true;
        public bool InSearchResultDefault    = true;
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(HierarchyFilterSetting))]
    public class HierarchyFilterSettingDrawer : PropertyDrawer
    {
        void DrawToggleSetField(string label, Rect position, SerializedProperty property, string key) {

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float padding = EditorGUIUtility.standardVerticalSpacing;
            float lineHeightPadding = lineHeight + padding;

            bool toggleable = property.FindPropertyRelative(key + "Toggleable").boolValue;

            GUI.Label(
                new Rect(
                    position.x + 10, 
                    position.y, 
                    position.width, position.height), 
                label
            );
        
            GUI.Label(
                new Rect(
                    position.x + position.width - lineHeight - 12, 
                    position.y, 
                    width: lineHeight + 10, 
                    height:lineHeight), 
                text:"", 
                GUI.skin.button);
        
            property.FindPropertyRelative(key + "Toggleable").boolValue = 
                GUI.Toggle(
                    new Rect(
                        position.x + position.width - lineHeight - lineHeightPadding * 0.9f - 1,
                        position.y, 
                        width: lineHeight, 
                        height:lineHeight), 
                    toggleable, 
                    toggleable ? "V" : "X", 
                    GUI.skin.GetStyle("ButtonLeft")
                );
        
            EditorGUI.PropertyField(
                new Rect(
                    position.x + position.width - lineHeight, position.y, 
                    width:lineHeight, 
                    height:lineHeight), 
                property.FindPropertyRelative(key + "Default"), GUIContent.none);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
        
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float padding = EditorGUIUtility.standardVerticalSpacing;
            float lineHeightPadding = lineHeight + padding;
            int indent = EditorGUI.indentLevel;
            float labelIndent = .8f * property.FindPropertyRelative("Indent").intValue * lineHeight;
        
            EditorGUI.indentLevel = 0;

            EditorGUI.PropertyField(
                new Rect(
                    position.x, 
                    position.y, 
                    width: position.width / 3 * 2, 
                    height:lineHeight), 
                property.FindPropertyRelative("Target"), 
                GUIContent.none
            );

            GUI.Label(
                new Rect(
                    position.x, position.y + lineHeightPadding - 1, 
                    width: 10,
                    height:lineHeight), 
                text:">"
            );
        
            EditorGUI.PropertyField(
                new Rect(
                    position.x + 12, 
                    position.y + lineHeightPadding, 
                    width: 50, 
                    height:lineHeight), 
                property.FindPropertyRelative("Indent"), 
                GUIContent.none);
        
            GUI.Label(
                    new Rect(
                        position.x + 62, 
                        position.y + lineHeightPadding - 0.5f, 
                        width: 10, 
                        height:lineHeight), 
                    text:"|")
                ;
            EditorGUI.PropertyField(
                new Rect(
                    position.x + 70 + labelIndent, 
                    position.y + lineHeightPadding,
                    width: 60,
                    height:lineHeight), 
                property.FindPropertyRelative("Icon"), 
                GUIContent.none);
        
            EditorGUI.PropertyField(
                new Rect(
                    position.x + 132 + labelIndent, 
                    position.y + lineHeightPadding, 
                    width: position.width - 132 - labelIndent,
                    height:lineHeight), 
                property.FindPropertyRelative("Name"), GUIContent.none);

            DrawToggleSetField(
                label:"In Hierarchy",
                new Rect(
                    position.x + position.width / 2 * 0, 
                    position.y + lineHeightPadding * 2, 
                    width: position.width / 2,
                    height:lineHeight),
                property, 
                key:"InHierarchy");
        
            DrawToggleSetField(
                label:"In Search",
                new Rect(
                    position.x + position.width / 2 * 1, 
                    position.y + lineHeightPadding * 2, 
                    width: position.width / 2,
                    height:lineHeight),
                property, 
                key:"InSearchResult");


            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
    
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            int totalLines = 3;

            return EditorGUIUtility.singleLineHeight * totalLines + EditorGUIUtility.standardVerticalSpacing * (totalLines - 1);
        }
    }
#endif
}