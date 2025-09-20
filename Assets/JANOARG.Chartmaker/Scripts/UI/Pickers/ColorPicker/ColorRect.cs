using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JANOARG.Chartmaker.UI.Pickers.ColorPicker
{
    [ExecuteInEditMode][RequireComponent(typeof(CanvasRenderer))]
    public class ColorRect : MaskableGraphic, IPointerDownHandler, IDragHandler
    {
        public Vector2Int Resolution = new Vector2Int(5, 5);
    
        public ColorPicker Picker;

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            Vector2 origin = new Vector2(rectTransform.rect.xMin, rectTransform.rect.yMin);
            Vector2 size = new Vector2(rectTransform.rect.width, rectTransform.rect.height);

            vertexHelper.Clear();

            UIVertex vert = UIVertex.simpleVert;
            vert.color = color;

            Color.RGBToHSV(color, out float hue, out _, out _);

            for (int x = 0; x <= Resolution.x; x++) 
            {
                for (int y = 0; y <= Resolution.y; y++) 
                {
                    vert.color = Color.HSVToRGB(hue, (float)x / Resolution.x, (float)y / Resolution.y);
                    vert.position = origin + size * new Vector2(x, y) / Resolution;
               
                    vertexHelper.AddVert(vert);
                
                    if (x > 0 && y > 0)
                    {
                        int resY = Resolution.y + 1;
                        int pos = x * resY + y;
                    
                        vertexHelper.AddTriangle(pos - resY - 1, pos - resY, pos);
                        vertexHelper.AddTriangle(pos - resY - 1, pos, pos - 1);
                    }
                }
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            OnPointerDown(eventData);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Vector3[] corners = new Vector3[4];
        
            rectTransform.GetWorldCorners(corners);
        
            Rect worldRect = new Rect(corners[0], corners[2] - corners[0]);
        
            Picker.CurrentHSV.y = Mathf.Clamp01((eventData.position.x - worldRect.xMin) / worldRect.width);
            Picker.CurrentHSV.z = Mathf.Clamp01((eventData.position.y - worldRect.yMin) / worldRect.height);
        
            Picker.UpdateRGB();
            Picker.UpdateHex();
            Picker.UpdateUI();
        }
    }
}
