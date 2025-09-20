using System.Collections.Generic;
using UnityEngine;

namespace JANOARG.Chartmaker.UI.Modal
{
    public class ModalHolder : MonoBehaviour
    {
        public static ModalHolder main;

        public List<Modal> Modals;

        public RectTransform NormalModalHolder;
        public RectTransform PriorityModalHolder;

        void Awake() 
        {
            main = this;
        }

        public T Spawn<T>() where T : Modal
        {
            foreach (Modal modal in Modals)
                if (modal is T)
                    return Instantiate(modal, NormalModalHolder) as T;
        
            throw new System.Exception();
        }
    }
}
