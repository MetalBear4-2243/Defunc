using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Displays a Victory Screen overlay when the player reaches a <see cref="VictoryZone"/>.
/// Automatically builds a clean Screen Space Overlay Canvas if no custom UI panel is assigned.
/// </summary>
public class VictoryScreen : MonoBehaviour
{
    [Header("Custom UI References (Optional)")]
    [Tooltip("Root panel to activate on victory. If unassigned, a default victory UI is generated at runtime.")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button nextLevelButton;

    [Header("Text Content")]
    [SerializeField] private string victoryTitle = "LEVEL COMPLETE";
    [SerializeField] private string victorySubtitle = "Timeline stabilized. Press R or click Restart to play again.";

    [Header("Behavior")]
    [Tooltip("If enabled, sets Time.timeScale to 0 when the victory screen appears.")]
    [SerializeField] private bool pauseTimeOnVictory = true;

    [Tooltip("Optional scene name for the Next Level button. If empty, checks Build Settings for currentBuildIndex + 1.")]
    [SerializeField] private string nextSceneName = "";

    [Tooltip("Duration (seconds) of the unscaled fade-in animation.")]
    [SerializeField] private float fadeInDuration = 0.25f;

    [Header("Default UI Styling")]
    [SerializeField] private Color backdropColor = new Color(0.03f, 0.05f, 0.08f, 0.78f);
    [SerializeField] private Color cardColor = new Color(0.08f, 0.11f, 0.15f, 0.96f);
    [SerializeField] private Color accentColor = new Color(0.2f, 0.95f, 0.5f, 1f);
    [SerializeField] private Color buttonColor = new Color(0.14f, 0.55f, 0.34f, 1f);
    [SerializeField] private Color secondaryButtonColor = new Color(0.20f, 0.25f, 0.32f, 1f);

    private CanvasGroup canvasGroup;
    private bool isShowing;
    private float fadeTimer;

    /// <summary>
    /// True while the victory screen is currently displayed.
    /// </summary>
    public bool IsShowing => isShowing;

    private void Awake()
    {
        if (victoryPanel == null)
        {
            BuildDefaultUI();
        }
        else
        {
            canvasGroup = victoryPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = victoryPanel.AddComponent<CanvasGroup>();
            }
        }

        WireButtons();
        HideImmediate();
    }

    private void Update()
    {
        if (!isShowing)
        {
            return;
        }

        if (canvasGroup != null && canvasGroup.alpha < 1f)
        {
            fadeTimer += Time.unscaledDeltaTime;
            float t = fadeInDuration > 0.001f ? Mathf.Clamp01(fadeTimer / fadeInDuration) : 1f;
            canvasGroup.alpha = t;
        }

        if (WasRestartKeyPressed())
        {
            RestartLevel();
        }
    }

    private void OnDestroy()
    {
        if (pauseTimeOnVictory && isShowing)
        {
            Time.timeScale = 1f;
        }
    }

    /// <summary>
    /// Displays the victory screen and optionally pauses gameplay.
    /// </summary>
    public void ShowVictory()
    {
        if (isShowing)
        {
            return;
        }

        isShowing = true;
        fadeTimer = 0f;

        EnsureEventSystemExists();

        if (titleText != null)
        {
            titleText.text = victoryTitle;
        }

        if (subtitleText != null)
        {
            subtitleText.text = victorySubtitle;
        }

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = fadeInDuration > 0.001f ? 0f : 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (pauseTimeOnVictory)
        {
            Time.timeScale = 0f;
        }
    }

    /// <summary>
    /// Reloads the currently active scene and restores normal time scale.
    /// </summary>
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    /// <summary>
    /// Loads the configured next scene or the next build index if available.
    /// </summary>
    public void LoadNextLevel()
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        int nextBuildIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextBuildIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextBuildIndex);
        }
    }

    private void HideImmediate()
    {
        isShowing = false;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }
    }

    private void WireButtons()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartLevel);
            restartButton.onClick.AddListener(RestartLevel);
        }

        if (nextLevelButton != null)
        {
            bool hasNext = !string.IsNullOrEmpty(nextSceneName) ||
                           (SceneManager.GetActiveScene().buildIndex + 1) < SceneManager.sceneCountInBuildSettings;
            nextLevelButton.gameObject.SetActive(hasNext);
            nextLevelButton.onClick.RemoveListener(LoadNextLevel);
            nextLevelButton.onClick.AddListener(LoadNextLevel);
        }
    }

    private static bool WasRestartKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.rKey.wasPressedThisFrame;
        }
#endif
        return Input.GetKeyDown(KeyCode.R);
    }

    private static void EnsureEventSystemExists()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        eventSystemObj.AddComponent<InputSystemUIInputModule>();
#else
        eventSystemObj.AddComponent<StandaloneInputModule>();
#endif
    }

    private void BuildDefaultUI()
    {
        GameObject canvasObj = new GameObject("VictoryScreen_Canvas");
        canvasObj.transform.SetParent(transform, false);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Fullscreen dim backdrop
        victoryPanel = new GameObject("VictoryPanel");
        victoryPanel.transform.SetParent(canvasObj.transform, false);

        RectTransform panelRect = victoryPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image backdrop = victoryPanel.AddComponent<Image>();
        backdrop.color = backdropColor;

        canvasGroup = victoryPanel.AddComponent<CanvasGroup>();

        // Centered card
        GameObject cardObj = new GameObject("VictoryCard");
        cardObj.transform.SetParent(victoryPanel.transform, false);

        RectTransform cardRect = cardObj.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(620f, 340f);

        Image cardBg = cardObj.AddComponent<Image>();
        cardBg.color = cardColor;

        // Top accent bar
        GameObject accentObj = new GameObject("AccentBar");
        accentObj.transform.SetParent(cardObj.transform, false);

        RectTransform accentRect = accentObj.AddComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, 8f);

        Image accentImage = accentObj.AddComponent<Image>();
        accentImage.color = accentColor;
        accentImage.raycastTarget = false;

        // Title
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(cardObj.transform, false);

        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -44f);
        titleRect.sizeDelta = new Vector2(-60f, 64f);

        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = victoryTitle;
        titleTmp.fontSize = 46f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = accentColor;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.raycastTarget = false;
        titleText = titleTmp;

        // Subtitle
        GameObject subObj = new GameObject("SubtitleText");
        subObj.transform.SetParent(cardObj.transform, false);

        RectTransform subRect = subObj.AddComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0f, 1f);
        subRect.anchorMax = new Vector2(1f, 1f);
        subRect.pivot = new Vector2(0.5f, 1f);
        subRect.anchoredPosition = new Vector2(0f, -124f);
        subRect.sizeDelta = new Vector2(-80f, 80f);

        TextMeshProUGUI subTmp = subObj.AddComponent<TextMeshProUGUI>();
        subTmp.text = victorySubtitle;
        subTmp.fontSize = 22f;
        subTmp.color = new Color(0.85f, 0.90f, 0.95f, 1f);
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.textWrappingMode = TextWrappingModes.Normal;
        subTmp.raycastTarget = false;
        subtitleText = subTmp;

        // Buttons
        bool hasNext = !string.IsNullOrEmpty(nextSceneName) ||
                       (SceneManager.GetActiveScene().buildIndex + 1) < SceneManager.sceneCountInBuildSettings;

        if (hasNext)
        {
            nextLevelButton = CreateButton(cardObj.transform, "NextLevelButton", "NEXT LEVEL", new Vector2(-115f, 54f), buttonColor);
            restartButton = CreateButton(cardObj.transform, "RestartButton", "RESTART", new Vector2(115f, 54f), secondaryButtonColor);
        }
        else
        {
            restartButton = CreateButton(cardObj.transform, "RestartButton", "RESTART LEVEL", new Vector2(0f, 54f), buttonColor);
        }
    }

    private static Button CreateButton(Transform parent, string objectName, string label, Vector2 anchoredBottomPos, Color bgColor)
    {
        GameObject btnObj = new GameObject(objectName);
        btnObj.transform.SetParent(parent, false);

        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0f);
        btnRect.anchorMax = new Vector2(0.5f, 0f);
        btnRect.pivot = new Vector2(0.5f, 0f);
        btnRect.anchoredPosition = anchoredBottomPos;
        btnRect.sizeDelta = new Vector2(210f, 54f);

        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = bgColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = colors;

        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 20f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return btn;
    }
}
