using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Achievement system with toast popup notifications.
/// Tracks milestones and displays animated popups in the corner.
/// </summary>
public class AchievementPopup : MonoBehaviour
{
    public static AchievementPopup Instance { get; private set; }

    private Canvas achievementCanvas;
    private Font cachedFont;

    private readonly HashSet<string> unlocked = new HashSet<string>();

    private readonly Queue<string[]> pendingPopups = new Queue<string[]>();
    private GameObject activePopup;
    private float popupTimer;

    private const float PopupDuration = 3.5f;
    private const float SlideSpeed = 6f;

    private int lastCompleted;
    private int lastFailed;
    private int consecutiveSuccess;
    private int lastUpgradeCount;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (cachedFont == null) cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private void Start()
    {
        CreateCanvas();
    }

    private void Update()
    {
        CheckMilestones();
        UpdatePopup();
    }

    private void CheckMilestones()
    {
        OrderManager om = OrderManager.Instance;
        if (om == null) return;

        int completed = om.CompletedOrders;
        int failed = om.FailedOrders;

        if (completed > lastCompleted)
        {
            consecutiveSuccess += (completed - lastCompleted);

            if (completed >= 1) TryUnlock("first_kebab", "Pierwszy Kebab!", "Zrealizowales pierwsze zamowienie");
            if (completed >= 10) TryUnlock("experienced", "Doswiadczony Kucharz", "10 zrealizowanych zamowien");
            if (completed >= 25) TryUnlock("master", "Mistrz Kebaba", "25 zrealizowanych zamowien");
            if (completed >= 50) TryUnlock("legend", "Legenda Kuchni", "50 zrealizowanych zamowien");
            if (consecutiveSuccess >= 5) TryUnlock("perfectionist", "Perfekcjonista", "5 zamowien z rzedu bez bledu");
            if (consecutiveSuccess >= 10) TryUnlock("flawless", "Bezbladny", "10 zamowien z rzedu bez bledu");
        }

        if (failed > lastFailed)
        {
            consecutiveSuccess = 0;
        }

        lastCompleted = completed;
        lastFailed = failed;

        ShopManager sm = ShopManager.Instance;
        if (sm != null)
        {
            int totalUpgrades = sm.TotalUpgradesPurchased;

            if (totalUpgrades > lastUpgradeCount && totalUpgrades >= 1)
            {
                TryUnlock("investor", "Inwestor", "Kupiono pierwsze ulepszenie");
            }

            var allDefs = sm.GetAllDefinitions();
            if (allDefs != null && allDefs.Count > 0)
            {
                int maxedCount = 0;
                foreach (var def in allDefs)
                {
                    if (def != null && sm.GetUpgradeLevel(def.type) >= def.maxLevel)
                    {
                        maxedCount++;
                    }
                }

                if (maxedCount >= allDefs.Count)
                {
                    TryUnlock("empire", "Imperium Kebabowe", "Wszystkie ulepszenia na maksimum!");
                }
            }

            lastUpgradeCount = totalUpgrades;
        }
    }

    private void TryUnlock(string id, string title, string description)
    {
        if (unlocked.Contains(id)) return;
        unlocked.Add(id);
        pendingPopups.Enqueue(new[] { title, description });
    }

    private void UpdatePopup()
    {

        if (activePopup != null)
        {
            popupTimer -= Time.deltaTime;

            RectTransform rt = activePopup.GetComponent<RectTransform>();
            if (rt != null)
            {
                float targetX = -20f;
                float currentX = rt.anchoredPosition.x;
                rt.anchoredPosition = new Vector2(
                    Mathf.Lerp(currentX, targetX, Time.deltaTime * SlideSpeed),
                    rt.anchoredPosition.y);
            }

            if (popupTimer < 0.5f)
            {
                CanvasGroup cg = activePopup.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = Mathf.Clamp01(popupTimer / 0.5f);
                }
            }

            if (popupTimer <= 0f)
            {
                Destroy(activePopup);
                activePopup = null;
            }

            return;
        }

        if (pendingPopups.Count > 0)
        {
            string[] data = pendingPopups.Dequeue();
            ShowPopup(data[0], data[1]);
        }
    }

    private void ShowPopup(string title, string description)
    {
        if (achievementCanvas == null) return;

        GameObject popup = new GameObject("AchievementPopup");
        popup.transform.SetParent(achievementCanvas.transform, false);

        Image bg = popup.AddComponent<Image>();
        bg.color = new Color(0.025f, 0.03f, 0.05f, 0.95f);

        RectTransform rt = popup.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(320f, 80f);
        rt.sizeDelta = new Vector2(300f, 70f);

        CanvasGroup cg = popup.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        GameObject accent = new GameObject("Accent");
        accent.transform.SetParent(popup.transform, false);
        Image accentImg = accent.AddComponent<Image>();
        accentImg.color = new Color(0.875f, 0.725f, 0.32f);
        RectTransform ar = accent.GetComponent<RectTransform>();
        ar.anchorMin = new Vector2(0f, 0f);
        ar.anchorMax = new Vector2(0f, 1f);
        ar.pivot = new Vector2(0f, 0.5f);
        ar.anchoredPosition = Vector2.zero;
        ar.sizeDelta = new Vector2(4f, 0f);

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(popup.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.font = cachedFont;
        titleText.fontSize = 13;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = new Color(0.875f, 0.78f, 0.52f);
        titleText.alignment = TextAnchor.MiddleLeft;
        titleText.text = title;
        titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        RectTransform trt = titleObj.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 0.5f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.offsetMin = new Vector2(14f, 0f);
        trt.offsetMax = new Vector2(-8f, -5f);

        GameObject descObj = new GameObject("Desc");
        descObj.transform.SetParent(popup.transform, false);
        Text descText = descObj.AddComponent<Text>();
        descText.font = cachedFont;
        descText.fontSize = 10;
        descText.color = new Color(0.55f, 0.58f, 0.64f);
        descText.alignment = TextAnchor.MiddleLeft;
        descText.text = description;
        descText.horizontalOverflow = HorizontalWrapMode.Overflow;
        RectTransform drt = descObj.GetComponent<RectTransform>();
        drt.anchorMin = new Vector2(0f, 0f);
        drt.anchorMax = new Vector2(1f, 0.5f);
        drt.offsetMin = new Vector2(14f, 5f);
        drt.offsetMax = new Vector2(-8f, 0f);

        activePopup = popup;
        popupTimer = PopupDuration;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayNewOrderSound();
        }
    }

    private void CreateCanvas()
    {
        GameObject obj = new GameObject("AchievementCanvas");
        obj.transform.SetParent(transform, false);
        achievementCanvas = obj.AddComponent<Canvas>();
        achievementCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        achievementCanvas.sortingOrder = 160;
        CanvasScaler scaler = obj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
