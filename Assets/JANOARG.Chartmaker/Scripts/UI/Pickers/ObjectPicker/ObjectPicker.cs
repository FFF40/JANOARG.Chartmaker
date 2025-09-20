using System.Collections.Generic;
using JANOARG.Shared.Data.ChartInfo;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

namespace JANOARG.Chartmaker.UI.Pickers.ObjectPicker
{
    public class ObjectPicker : Picker
    {
        public static ObjectPicker main;
    
        public ObjectPickerType Type;

        public RectTransform          ItemHolder;
        public ObjectPickerItemHolder ItemSample;
        List<ObjectPickerItemHolder>  Items = new();
        public TMP_InputField         SearchField;
        public GameObject             SearchIcon;
        public GameObject             SearchClear;

        [Space]
        public Sprite LaneStyleIcon;
        public Sprite HitStyleIcon;

        public object CurrentObject;

        public override void Awake()
        {
            main = this;

            base.Awake();
        }

        public override void Open()
        {
            base.Open();
        
            EventSystem.current.SetSelectedGameObject(SearchField.gameObject);
            UpdateItems();
        }

        public void UpdateItems() 
        {
            foreach (var item in Items)
                Destroy(item.gameObject);
        
            Items.Clear();

            void AddItem(object obj, string name, Sprite icon) 
            {
                var item = Instantiate(ItemSample, ItemHolder);
           
                item.Icon.sprite = icon;
                item.Icon.gameObject.SetActive(icon != null);
                item.SetItem(obj, name);
            
                Items.Add(item);
            }

            AddItem(null, "None", null);

            switch (Type)
            {
                case ObjectPickerType.LaneStyle:
                {
                    int index = 0;
          
                    foreach (LaneStyle style in Behaviors.Chartmaker.Chartmaker.main.CurrentChart.Palette.LaneStyles)
                    {
                        AddItem(style, string.IsNullOrEmpty(style.Name) ? "Lane Style " + index : style.Name, LaneStyleIcon);
               
                        index++;
                    }
                    break;
                }
            
                case ObjectPickerType.HitStyle:
                {
                    int index = 0;
               
                    foreach (HitStyle style in Behaviors.Chartmaker.Chartmaker.main.CurrentChart.Palette.HitStyles)
                    {
                        AddItem(style, string.IsNullOrEmpty(style.Name) ? "Hit Style " + index : style.Name, HitStyleIcon);
                   
                        index++;
                    }

                    break;
                }
            }

            UpdateVisibility();
        }

        public void UpdateVisibility() 
        {
            string query = SearchField.text;
            bool isSearch = !string.IsNullOrEmpty(query);
       
            SearchIcon.SetActive(!isSearch);
            SearchClear.SetActive(isSearch);

            foreach (var item in Items) 
            {
                item.gameObject.SetActive(!isSearch || (item.Target != null && item.name.ContainsInsensitive(query)));
                item.Checkmark.SetActive(item.Target == CurrentObject);
            }
        }


        public void Select(object obj) 
        {
            CurrentObject = obj;
       
            OnSet.Invoke();

            UpdateVisibility();
        }

        public void ClearSearch() 
        {
            SearchField.text = "";
        }
    }

    public enum ObjectPickerType 
    {
        LaneStyle,
        HitStyle,
    }
}