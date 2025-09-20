using System;
using TMPro;
using UnityEngine;

namespace JANOARG.Chartmaker.UI.Form
{
    public class FormEntry : MonoBehaviour
    {
        public string   Title;
        public TMP_Text TitleLabel;

        public void Start() 
        {
            gameObject.name = TitleLabel.text = Title;
        }
    }

    public class FormEntry<T> : FormEntry
    {
        public T CurrentValue;

        public Func<T>   OnGet;
        public Action<T> OnSet;

        public new void Start() 
        {
            base.Start();
            CurrentValue = OnGet();
        }

        public void SetValue(T value)
        {
            CurrentValue = value;
            OnSet(value);
        }
    }
}