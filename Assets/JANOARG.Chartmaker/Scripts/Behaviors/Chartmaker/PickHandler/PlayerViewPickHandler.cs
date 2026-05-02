using UnityEngine;
using UnityEngine.EventSystems;

namespace JANOARG.Chartmaker.Behaviors.Chartmaker.PickHandler
{
    public abstract class PlayerViewPickHandler : MonoBehaviour
    {
        public abstract bool Pick(PointerEventData eventData);
    }
}