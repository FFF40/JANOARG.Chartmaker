using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;

#endif

namespace JANOARG.Chartmaker.UI
{

    public class RightClickButton : Button
    {
        public UnityEvent onRightClick;

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            if (eventData.button == PointerEventData.InputButton.Right) 
                onRightClick.Invoke();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(RightClickButton), true)]
    [CanEditMultipleObjects]
    public class RightClickButtonInspector : ButtonEditor {
        public override void OnInspectorGUI(){
            base.OnInspectorGUI();
            this.serializedObject.Update();
     
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onRightClick"));
        
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}