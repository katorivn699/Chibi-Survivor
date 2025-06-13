using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

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
            StartCoroutine(TransitionRoutine(sceneName));
        }
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        isTransitioning = true;

        // Tạm dừng gameplay
        Time.timeScale = 0.001f;

        // Hiệu ứng mở rộng vòng tròn
        yield return StartCoroutine(Expand());

        // Load scene mới
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Đợi 1 frame để UI scene mới khởi tạo
        yield return null;

        // Hiệu ứng thu nhỏ vòng tròn
        yield return StartCoroutine(Shrink());

        // Khôi phục gameplay
        Time.timeScale = 1f;

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
