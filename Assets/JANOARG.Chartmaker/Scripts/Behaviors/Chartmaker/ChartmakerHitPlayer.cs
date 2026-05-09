using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Chartmaker.Utils;
using UnityEngine;
using UnityEngine.Serialization;

namespace JANOARG.Chartmaker.Behaviors.Chartmaker
{
    public class ChartmakerHitPlayer : MonoBehaviour
    {
        public HitObjectManager CurrentHit;
        public MeshRenderer     Renderer;
        [FormerlySerializedAs("IndicatohRenderers")] 
        public MeshRenderer[] IndicatorRenderers;

        public MeshRenderer HoldTail;
        public MeshRenderer FlickEmblem;
        
        public Transform HitObjectPosition;

        public void OnDestroy()
        {
            if (HoldTail)
                Destroy(HoldTail.gameObject);
            if (FlickEmblem)
                Destroy(FlickEmblem.gameObject);
        }

        public void UpdateObjects(HitObjectManager hit) 
        {
            CurrentHit = hit;
            transform.localPosition = hit.Position;
            transform.localRotation = hit.Rotation;

            var styles = PlayerView.main.Manager.PalleteManager.HitStyles;
            int index = hit.Current.StyleIndex;
            bool visible = index >= 0 && index < styles.Count;

            Material material = null, mainMaterial;
            mainMaterial = visible 
                ? styles[index].NormalMaterial : null;

            switch (hit.Current.Type)
            {
                case HitObject.HitType.Normal:
                    Renderer.transform.localScale = new (hit.Length - .5f, .5f, .5f);
            
                    IndicatorRenderers[0].transform.localScale    = IndicatorRenderers[1].transform.localScale = new (.25f, .5f, .5f);
                    IndicatorRenderers[0].transform.localPosition = new (hit.Length / 2 + .125f, 0, 0);
                    IndicatorRenderers[1].transform.localPosition = -IndicatorRenderers[0].transform.localPosition;
            
                    material = visible 
                        ? styles[index].NormalMaterial : null;
                
                    break;
            
                case HitObject.HitType.Catch:
                    Renderer.transform.localScale = new (hit.Length, .25f, .25f);
                
                    IndicatorRenderers[0].transform.localScale    = IndicatorRenderers[1].transform.localScale = new (.25f, .5f, .5f);
                    IndicatorRenderers[0].transform.localPosition = new (hit.Length / 2 + .125f, 0, 0);
                
                    IndicatorRenderers[1].transform.localPosition = -IndicatorRenderers[0].transform.localPosition;
                
                    material = visible 
                        ? styles[index].CatchMaterial : null;
                
                    break;
            }

            Vector2 camStart = PlayerView.main.MainCamera.WorldToScreenPoint(IndicatorRenderers[0].transform.position);
            Vector2 camEnd = PlayerView.main.MainCamera.WorldToScreenPoint(IndicatorRenderers[1].transform.position);

            if (Renderer.sharedMaterial != material) 
            {
                Renderer.enabled = material;
                Renderer.sharedMaterial = material;
            
                foreach (MeshRenderer ind in IndicatorRenderers) 
                {
                    ind.enabled = Renderer.enabled;
                    ind.sharedMaterial = mainMaterial;
                }
            }

            if (hit.HoldMesh && visible) 
            { 
                if (!HoldTail)
                    HoldTail = Instantiate(PlayerView.main.HoldMeshSample, transform.parent);
            
                HoldTail.sharedMaterial = styles[index].HoldTailMaterial;
                HoldTail.GetComponent<MeshFilter>().sharedMesh = hit.HoldMesh;
            }
            else 
            {
                if (HoldTail) {
                    Destroy(HoldTail.gameObject);
                } 
            }

            if (hit.Current.Flickable) 
            { 
                if (!FlickEmblem) {
                    FlickEmblem = Instantiate(PlayerView.main.HoldMeshSample, transform);
                } 
            
                
                FlickEmblem.gameObject.SetActive(true);
                FlickEmblem.sharedMaterial = mainMaterial;
                FlickEmblem.transform.eulerAngles = PlayerView.main.MainCamera.transform.eulerAngles;
            
                bool directional = float.IsFinite(hit.Current.FlickDirection);
            
                FlickEmblem.GetComponent<MeshFilter>().sharedMesh = directional 
                    ? PlayerView.main.ArrowFlickIndicator : PlayerView.main.FreeFlickIndicator;
            
                if (directional) 
                    FlickEmblem.transform.Rotate(Vector3.back * hit.Current.FlickDirection);
                else
                    FlickEmblem.transform.Rotate(Vector3.forward * Vector2.SignedAngle(Vector2.right, camEnd - camStart));
            }
            else 
            {
                if (FlickEmblem)
                    FlickEmblem.gameObject.SetActive(false); // hide, don't destroy yet
            }
        }
    }
}