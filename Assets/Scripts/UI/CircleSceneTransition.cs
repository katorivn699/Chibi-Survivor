using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class CircleSceneTransition : MonoBehaviour
{
    public static CircleSceneTransition Instance;

    [Header("Assign in Prefab")]
    public Image circleImage;
    public float expandDuration = 1f;
    public float shrinkDuration = 1f;

    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 1000; // Đảm bảo luôn ở trên cùng
            }

            // Tối ưu CanvasGroup nếu có
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void TransitionToScene(string sceneName)
    {
        if (!isTransitioning)
        {
            StartCoroutine(TransitionRoutine(sceneName, null));
        }
    }

    public void TransitionToScene(string sceneName, Action onTransitionComplete)
    {
        if (!isTransitioning)
        {
            StartCoroutine(TransitionRoutine(sceneName, onTransitionComplete));
        }
    }

    private IEnumerator TransitionRoutine(string sceneName, Action onTransitionComplete)
    {
        isTransitioning = true;

        // Pause gameplay
        Time.timeScale = 0.001f;

        // Expand circle animation
        yield return StartCoroutine(Expand());

        // Load new scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Wait one frame for new scene UI to initialize
        yield return null;

        // Shrink circle animation
        yield return StartCoroutine(Shrink());

        // Restore gameplay
        Time.timeScale = 1f;

        // Invoke the completion callback
        onTransitionComplete?.Invoke();

        isTransitioning = false;
    }

    private IEnumerator Expand()
    {
        circleImage.rectTransform.localScale = Vector3.zero;

        float timer = 0f;
        while (timer < expandDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / expandDuration;
            float scale = Mathf.Lerp(0f, 25f, t);
            circleImage.rectTransform.localScale = new Vector3(scale, scale, 1);
            yield return null;
        }

        circleImage.rectTransform.localScale = new Vector3(25f, 25f, 1);
    }

    private IEnumerator Shrink()
    {
        circleImage.rectTransform.localScale = new Vector3(25f, 25f, 1);

        float timer = 0f;
        while (timer < shrinkDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / shrinkDuration;
            float scale = Mathf.Lerp(25f, 0f, t);
            circleImage.rectTransform.localScale = new Vector3(scale, scale, 1);
            yield return null;
        }

        circleImage.rectTransform.localScale = Vector3.zero;
    }


    public static void EnsureInstanceExists()
    {
        if (Instance == null)
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/CircleTransitionUI");
            GameObject obj = Instantiate(prefab);
            Instance = obj.GetComponent<CircleSceneTransition>();
        }
    }
}
