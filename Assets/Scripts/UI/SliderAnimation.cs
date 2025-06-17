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

    private Vector3 originalScale = Vector3.one; // Default to Vector3.one for safety
    private Slider slider;
    private bool isDragging;

    private void Awake()
    {
        // Cache the slider component in Awake to ensure it's available early
        slider = GetComponent<Slider>();
        if (slider == null)
        {
            Debug.LogError("Slider component not found on " + gameObject.name, gameObject);
            enabled = false; // Disable script to prevent further issues
            return;
        }

        // Cache the original scale in Awake to ensure it's set before any animations
        originalScale = transform.localScale != Vector3.zero ? transform.localScale : Vector3.one;
    }

    private void Start()
    {
        // Ensure the scale is reset to original on start
        transform.localScale = originalScale;

        // Ensure the slider is interactable
        if (slider != null)
        {
            slider.interactable = true;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!slider.interactable) return;

        isDragging = true;
        // Kill any existing animations to prevent conflicts
        transform.DOKill();
        transform.DOScale(originalScale * scaleFactor, animationDuration).SetEase(scaleEase);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!slider.interactable) return;

        isDragging = false;
        // Kill any existing animations to prevent conflicts
        transform.DOKill();
        transform.DOScale(originalScale, animationDuration).SetEase(scaleEase);
        transform.DOShakePosition(shakeDuration, shakeStrength, 10, 90f, false);
    }

    private void OnEnable()
    {
        // Reset scale and state when enabled
        if (slider != null)
        {
            slider.interactable = true;
        }
        transform.DOKill(); // Kill any existing animations
        transform.localScale = originalScale;
        isDragging = false;
    }

    private void OnDisable()
    {
        // Clean up animations and reset scale when disabled
        transform.DOKill();
        transform.localScale = originalScale;
        isDragging = false;
    }

    private void OnDestroy()
    {
        // Ensure all DOTween animations are killed when the object is destroyed
        transform.DOKill();
    }
}