using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BouncyButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float scaleFactor = 1.2f; 
    [SerializeField] private float pressDuration = 0.1f; 
    [SerializeField] private float releaseDuration = 0.15f; 
    [SerializeField] private Ease easeType = Ease.OutBack; 

    private Vector3 originalScale; 
    private Button button; 

    void Start()
    {
        originalScale = transform.localScale;
        button = GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.DOScale(originalScale * scaleFactor, pressDuration).SetEase(Ease.InOutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.DOScale(originalScale, releaseDuration).SetEase(easeType);
    }

    void OnEnable()
    {
        if (button != null)
            button.interactable = true;
    }

    void OnDisable()
    {
        transform.localScale = originalScale;
    }
}
