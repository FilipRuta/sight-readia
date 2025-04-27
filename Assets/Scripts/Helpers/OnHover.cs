using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Helpers
{
    public class OnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TextMeshProUGUI hintText;
    
        public void OnPointerEnter(PointerEventData eventData)
        {
            hintText.DOFade(1.0f, 0.5f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hintText.DOFade(0.0f, 0.5f);
        }
    }
}
