using System.Collections.Generic;
using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Chartmaker.Utils;
using UnityEngine;

namespace JANOARG.Chartmaker.Behaviors.Chartmaker
{
    public class ChartmakerLanePlayer : MonoBehaviour
    {
        public LaneManager    CurrentLane;
        public Transform      Holder;
        public MeshRenderer   Renderer;
        public MeshFilter     Filter;
        public MeshCollider   Collider;
        public MeshRenderer   JudgeLine;
        public MeshRenderer[] JudgeEnds;

        public List<ChartmakerHitPlayer> HitPlayers { get; private set; } = new();

        public void UpdateObjects(LaneManager lane) 
        {
            CurrentLane = lane;
        
            transform.SetLocalPositionAndRotation(lane.FinalPosition, lane.FinalRotation);
        
            Holder.localPosition = Vector3.back * lane.CurrentDistance;
        
            List<LaneStyleManager> styles = PlayerView.main.Manager.PalleteManager.LaneStyles;
        
            int index = lane.Current.StyleIndex;
        
            Collider.enabled = lane.Steps.Count >= 2 && PlayerView.main.CurrentTime < lane.Steps[^1].Offset;
            Renderer.enabled = Collider.enabled && index >= 0 && index < styles.Count;
        
            Renderer.sharedMaterial = Renderer.enabled ? styles[index].LaneMaterial : null;

            if (PlayerView.main.MainCamera.activeTexture || !Collider.enabled)
                Collider.sharedMesh = null;
            
            Filter.sharedMesh = Renderer.enabled 
                ? lane.CurrentMesh : null;

            if (PlayerView.main.CurrentTime >= lane.Steps[0].Offset && PlayerView.main.CurrentTime < lane.Steps[^1].Offset)
            {
                   JudgeLine.gameObject.SetActive(Renderer.enabled);
                JudgeEnds[0].gameObject.SetActive(Renderer.enabled);
                JudgeEnds[1].gameObject.SetActive(Renderer.enabled);
            
                JudgeLine.sharedMaterial = JudgeEnds[0].sharedMaterial = 
                    JudgeEnds[1].sharedMaterial = Renderer.enabled 
                        ? styles[index].JudgeMaterial : null;
            
                JudgeEnds[0].transform.localPosition = lane.StartPosLocal;
                JudgeEnds[1].transform.localPosition = lane.EndPosLocal;
            
                JudgeLine.transform.localPosition    = (lane.StartPosLocal + lane.EndPosLocal) / 2;
                JudgeLine.transform.localScale       = new (Vector3.Distance(lane.StartPosLocal, lane.EndPosLocal), .05f, .05f);
                JudgeLine.transform.localEulerAngles = Vector3.back * Vector2.SignedAngle(lane.EndPosLocal - lane.StartPosLocal, Vector2.left);
                
            }
            else 
            {
                   JudgeLine.gameObject.SetActive(false);
                JudgeEnds[0].gameObject.SetActive(false);
                JudgeEnds[1].gameObject.SetActive(false);
            }
        
            int count = 0;
        
            foreach (HitObjectManager hitobject in lane.Objects)
            {
                if (hitobject.TimeEnd < PlayerView.main.CurrentTime) 
                    continue;
            
                if (hitobject.Position.z > lane.CurrentDistance + 250) 
                    break;
            
                if (HitPlayers.Count <= count)
                    HitPlayers.Add(Instantiate(PlayerView.main.HitPlayerSample, Holder));
            
                HitPlayers[count].UpdateObjects(hitobject);
                count++;
            }
        
            while (HitPlayers.Count > count)
            {
                Destroy(HitPlayers[count].gameObject);
                HitPlayers.RemoveAt(count);
            }

        }
    }
}