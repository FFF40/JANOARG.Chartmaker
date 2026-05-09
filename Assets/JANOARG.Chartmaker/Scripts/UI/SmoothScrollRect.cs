using System.Collections;
using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Chartmaker.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using JANOARG.Shared.Utils.Animation;

namespace JANOARG.Chartmaker.UI
{
    public class SmoothScrollRect : ScrollRect 
    {
        private float m_AnimationSpeed = 0.25f;

        private Coroutine m_currentRoutine  = null;
        private Vector2?  m_targetScrollPos = null;
        private Vector2   m_animBaseVelocity;
        private Vector2   m_currentVelocity;

        private IEnumerator ScrollAnimation(Vector2 from, Vector2 to)
        {
            float t = 0;

            while (true)
            {
                t += Time.deltaTime;
                Vector2 current = content.anchoredPosition;
                Vector2 target = new (
                    SpringEase.Get(t, from.x, to.x, 0.1f, m_AnimationSpeed, m_animBaseVelocity.x),
                    SpringEase.Get(t, from.y, to.y, 0.1f, m_AnimationSpeed, m_animBaseVelocity.y)
                );
                
                SetContentAnchoredPosition(target);

                if (Vector2.SqrMagnitude(target - current) < 1e-3f && Vector2.SqrMagnitude(target - to) < 1e-3f)
                {
                    break;
                }

                yield return null;
            }

            yield return null;
            SetContentAnchoredPosition(to);

            m_targetScrollPos = null;
        }

        public void StartScrollAnimation(Vector2 from, Vector2 to) 
        {
            velocity = Vector2.zero;
        
            if (m_currentRoutine != null)
            {
                StopCoroutine(m_currentRoutine);
                m_animBaseVelocity = m_currentVelocity;
            }
            else
            {
                m_animBaseVelocity = velocity;
            }
        
            if (from == to) 
                return;
        
            m_currentRoutine = StartCoroutine(ScrollAnimation(from, to));
        }

        public void StartScrollAnimation(Vector2 to) 
        {
            StartScrollAnimation(content.anchoredPosition, to);
        }

        public override void OnScroll(PointerEventData data)
        {
            Vector2 from = content.anchoredPosition;
       
            if (m_targetScrollPos != null)
                content.anchoredPosition = (Vector2)m_targetScrollPos;
        
            base.OnScroll(data);
       
            Vector2 to = content.anchoredPosition;
            m_targetScrollPos = to;
       
            SetContentAnchoredPosition(from = Vector2.Lerp(from, to, .1f));

        
            StartScrollAnimation(from, to);
        }
    }
}