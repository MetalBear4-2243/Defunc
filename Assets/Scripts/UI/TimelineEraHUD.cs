using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a compact HUD badge in the top-left corner indicating whether the player
/// is currently in the Past or Future timeline.
/// Automatically builds a minimal Screen Space Overlay Canvas if no UI references are assigned.
/// </summary>
public class TimelineEraHUD : MonoBehaviour
{
    [Header("References")]
    [Tooltip("TimeShiftController to observe. Auto-finds in scene if left empty.")]
    [SerializeField] private TimeShiftController timeShiftController;

    [Tooltip("Optional custom TextMeshProUGUI label. Auto-created in top-left corner if left empty.")]
    [SerializeField] private TMP_Text eraLabel;

    [Tooltip("Optional background Image for the badge.")]
    [SerializeField] private Image badgeBackground;

    [Tooltip("Optional small accent/dot Image inside the badge.")]
    [SerializeField] private Image indicatorDot;

    [Header("Display Labels")]
    [SerializeField] private string pastLabel = "PAST";
    [SerializeField] private string futureLabel = "FUTURE";

    [Header("Colors")]
    [SerializeField] private Color pastAccentColor = new Color(0.95f, 0.72f, 0.30f, 1f);
    [SerializeField] private Color futureAccentColor = new Color(0.32f, 0.85f, 1f, 1f);
    [SerializeField] private Color badgeBackgroundColor = new Color(0.08f, 0.10f, 0.14f, 0.78f);

    [Header("Auto-Generated Layout")]
    [SerializeField] private Vector2 topLeftMargin = new Vector2(20f, -20f);
    [SerializeField] private Vector2 badgeSize = new Vector2(140f, 40f);
    [SerializeField] private float fontSize = 20f;

    private bool isSubscribed;

    private void Awake()
    {
        if (timeShiftController == null)
        {
            timeShiftController = FindAnyObjectByType<TimeShiftController>();
        }

        if (eraLabel == null)
        {
            BuildDefaultUI();
        }
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        Subscribe();

        if (timeShiftController != null)
        {
            UpdateDisplay(timeShiftController.CurrentEra);
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (isSubscribed || timeShiftController == null)
        {
            return;
        }

        timeShiftController.OnTimelineShifted += HandleTimelineShifted;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed || timeShiftController == null)
        {
            return;
        }

        timeShiftController.OnTimelineShifted -= HandleTimelineShifted;
        isSubscribed = false;
    }

    private void HandleTimelineShifted(TimelineEra newEra, bool wasSafetyNudged)
    {
        UpdateDisplay(newEra);
    }

    /// <summary>
    /// Updates the HUD text and accent colors to match <paramref name="era"/>.
    /// </summary>
    public void UpdateDisplay(TimelineEra era)
    {
        bool isPast = era == TimelineEra.Past;
        Color accent = isPast ? pastAccentColor : futureAccentColor;

        if (eraLabel != null)
        {
            eraLabel.text = isPast ? pastLabel : futureLabel;
            eraLabel.color = accent;
        }

        if (indicatorDot != null)
        {
            indicatorDot.color = accent;
        }

        if (badgeBackground != null)
        {
            badgeBackground.color = badgeBackgroundColor;
        }
    }

    private void BuildDefaultUI()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("TimelineEraHUD_Canvas");
        canvasObj.transform.SetParent(transform, false);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Create top-left badge container
        GameObject badgeObj = new GameObject("EraBadge");
        badgeObj.transform.SetParent(canvasObj.transform, false);

        RectTransform badgeRect = badgeObj.AddComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0f, 1f);
        badgeRect.anchorMax = new Vector2(0f, 1f);
        badgeRect.pivot = new Vector2(0f, 1f);
        badgeRect.anchoredPosition = topLeftMargin;
        badgeRect.sizeDelta = badgeSize;

        badgeBackground = badgeObj.AddComponent<Image>();
        badgeBackground.color = badgeBackgroundColor;
        badgeBackground.raycastTarget = false;

        // Create small left accent bar/dot
        GameObject dotObj = new GameObject("AccentBar");
        dotObj.transform.SetParent(badgeObj.transform, false);

        RectTransform dotRect = dotObj.AddComponent<RectTransform>();
        dotRect.anchorMin = new Vector2(0f, 0.2f);
        dotRect.anchorMax = new Vector2(0f, 0.8f);
        dotRect.pivot = new Vector2(0f, 0.5f);
        dotRect.anchoredPosition = new Vector2(10f, 0f);
        dotRect.sizeDelta = new Vector2(6f, 0f);

        indicatorDot = dotObj.AddComponent<Image>();
        indicatorDot.raycastTarget = false;

        // Create TextMeshProUGUI label
        GameObject textObj = new GameObject("EraText");
        textObj.transform.SetParent(badgeObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(24f, 0f);
        textRect.offsetMax = new Vector2(-10f, 0f);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;

        eraLabel = tmp;
    }
}
