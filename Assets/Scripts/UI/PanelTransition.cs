using DG.Tweening;
using UnityEngine;

public class PanelTransition : MonoBehaviour
{
    [SerializeField] private float slideDistance = 200f;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private Ease slideEase = Ease.OutQuad;
    [SerializeField] private Ease fadeEase = Ease.InOutQuad;

    private CanvasGroup canvasGroup;
    private Vector3 originalPosition;
    private bool isShown = false;

    void Awake() // Use Awake instead of Start for earlier initialization
    {
        // Ensure CanvasGroup exists
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            Debug.LogWarning($"CanvasGroup was missing on {gameObject.name} and has been added.");
        }

        originalPosition = transform.localPosition;

        // Initialize panel state
        canvasGroup.alpha = 0f;
        transform.localPosition = originalPosition - new Vector3(slideDistance, 0, 0);
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    public void TogglePanel()
    {
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"Cannot toggle panel on {gameObject.name}: GameObject is inactive.");
            return;
        }

        if (isShown)
            HidePanel();
        else
            ShowPanel();
    }

    public void ShowPanel()
    {
        if (canvasGroup == null || transform == null)
        {
            Debug.LogError($"Cannot show panel on {gameObject.name}: CanvasGroup or Transform is null.");
            return;
        }

        transform.DOLocalMove(originalPosition, fadeDuration).SetEase(slideEase);
        canvasGroup.DOFade(1f, fadeDuration).SetEase(fadeEase);
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        isShown = true;
    }

    public void HidePanel()
    {
        if (canvasGroup == null || transform == null)
        {
            Debug.LogError($"Cannot hide panel on {gameObject.name}: CanvasGroup or Transform is null.");
            return;
        }

        transform.DOLocalMove(originalPosition - new Vector3(slideDistance, 0, 0), fadeDuration).SetEase(slideEase);
        canvasGroup.DOFade(0f, fadeDuration).SetEase(fadeEase);
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        isShown = false;
    }

    void OnDisable()
    {
        // Only reset if components are available
        if (canvasGroup != null && transform != null)
        {
            DOTween.Kill(transform); // Kill any ongoing tweens to prevent conflicts
            DOTween.Kill(canvasGroup);
            transform.localPosition = originalPosition;
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            isShown = false;
        }
        else
        {
            Debug.LogWarning($"OnDisable called on {gameObject.name} but CanvasGroup or Transform is null.");
        }
    }
}