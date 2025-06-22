using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SliderAnimation : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float scaleFactor = 1.15f;
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private Ease scaleEase = Ease.OutBack;
    [SerializeField] private float shakeStrength = 5f;
    [SerializeField] private float shakeDuration = 0.1f;

    private Vector3 originalScale = Vector3.one;
    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        if (slider == null)
        {
            Debug.LogError("Slider component not found on " + gameObject.name, gameObject);
            enabled = false;
            return;
        }
        originalScale = transform.localScale != Vector3.zero ? transform.localScale : Vector3.one;
    }

    private void Start()
    {
        transform.localScale = originalScale;
        if (slider != null)
        {
            slider.interactable = true;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!slider.interactable) return;
        transform.DOKill();
        transform.DOScale(originalScale * scaleFactor, animationDuration).SetEase(scaleEase);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!slider.interactable) return;
        transform.DOKill();
        transform.DOScale(originalScale, animationDuration).SetEase(scaleEase);
        transform.DOShakePosition(shakeDuration, shakeStrength, 10, 90f, false);
    }

    private void OnEnable()
    {
        if (slider != null)
        {
            slider.interactable = true;
        }
        transform.DOKill();
        transform.localScale = originalScale;
    }

    private void OnDisable()
    {
        transform.DOKill();
        transform.localScale = originalScale;
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}