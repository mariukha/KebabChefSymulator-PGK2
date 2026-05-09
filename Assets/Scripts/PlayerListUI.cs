using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerListUI : MonoBehaviour
{
    
    private static readonly Color PanelBackground = new Color(0.025f, 0.04f, 0.065f, 0.92f);
    private static readonly Color HeaderColor = new Color(0.06f, 0.10f, 0.16f, 0.95f);
    private static readonly Color RowEven = new Color(0.05f, 0.07f, 0.10f, 0.80f);
    private static readonly Color RowOdd = new Color(0.07f, 0.09f, 0.13f, 0.80f);
    private static readonly Color GoldText = new Color(1f, 0.92f, 0.65f);
    private static readonly Color WhiteText = new Color(0.95f, 0.97f, 1f, 0.94f);
    private static readonly Color SubText = new Color(0.65f, 0.70f, 0.78f);
    private static readonly Color FrameColor = new Color(0.14f, 0.16f, 0.20f);

    private static readonly Color[] PlayerColors = new Color[]
    {
        new Color(0.2f, 0.6f, 0.9f),
        new Color(0.9f, 0.4f, 0.3f),
        new Color(0.3f, 0.8f, 0.4f),
        new Color(0.9f, 0.75f, 0.2f)
    };

    private Canvas listCanvas;
    private GameObject listPanel;
    private Text titleText;
    private Text hintText;
    private GameObject rowContainer;

    private GameObject[] playerRows = new GameObject[4];
    private Image[] playerColorDots = new Image[4];
    private Text[] playerNameTexts = new Text[4];
    private Text[] playerRoleTexts = new Text[4];
    private Text[] playerPingTexts = new Text[4];

    private Font cachedFont;
    private bool isVisible;

    private void Awake()
    {
        CreateUI();
    }

    private void Update()
    {
        
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ShowList();
        }

        if (Input.GetKeyUp(KeyCode.Tab))
        {
            HideList();
        }

        if (isVisible)
        {
            RefreshPlayerData();
        }
    }

    private void ShowList()
    {
        isVisible = true;
        if (listCanvas != null)
        {
            listCanvas.enabled = true;
        }
    }

    private void HideList()
    {
        isVisible = false;
        if (listCanvas != null)
        {
            listCanvas.enabled = false;
        }
    }

    private void CreateUI()
    {
        cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (cachedFont == null)
        {
            cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        GameObject canvasObject = new GameObject("PlayerListCanvas");
        canvasObject.transform.SetParent(transform, false);
        listCanvas = canvasObject.AddComponent<Canvas>();
        listCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        listCanvas.sortingOrder = 150; 
        listCanvas.enabled = false;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject backdrop = CreatePanel(canvasObject.transform, "ListBackdrop",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.35f));
        RectTransform bdRect = backdrop.GetComponent<RectTransform>();
        bdRect.anchorMin = Vector2.zero;
        bdRect.anchorMax = Vector2.one;
        bdRect.offsetMin = Vector2.zero;
        bdRect.offsetMax = Vector2.zero;

        listPanel = CreatePanel(canvasObject.transform, "PlayerListPanel",
            new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(500f, 320f), PanelBackground);

        CreatePanelBorder(listPanel.transform, FrameColor);

        CreatePanel(listPanel.transform, "HeaderBg",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -4f), new Vector2(-8f, 50f), HeaderColor);

        titleText = CreateText(listPanel.transform, "Title", 20, TextAnchor.MiddleCenter,
            GoldText, FontStyle.Bold);
        titleText.text = "\u2726  GRACZE ONLINE  \u2726";
        PositionRect(titleText, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(460f, 40f));

        Text colName = CreateText(listPanel.transform, "ColName", 12, TextAnchor.MiddleLeft,
            SubText, FontStyle.Normal);
        colName.text = "NICK";
        PositionRect(colName, new Vector2(0f, 1f), new Vector2(70f, -68f), new Vector2(200f, 20f));

        Text colRole = CreateText(listPanel.transform, "ColRole", 12, TextAnchor.MiddleCenter,
            SubText, FontStyle.Normal);
        colRole.text = "ROLA";
        PositionRect(colRole, new Vector2(0.5f, 1f), new Vector2(60f, -68f), new Vector2(100f, 20f));

        Text colPing = CreateText(listPanel.transform, "ColPing", 12, TextAnchor.MiddleRight,
            SubText, FontStyle.Normal);
        colPing.text = "PING";
        PositionRect(colPing, new Vector2(1f, 1f), new Vector2(-30f, -68f), new Vector2(80f, 20f));

        CreatePanel(listPanel.transform, "HeaderSep",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -82f), new Vector2(-16f, 1f), new Color(1f, 1f, 1f, 0.12f));

        for (int i = 0; i < 4; i++)
        {
            CreatePlayerRow(i);
        }

        hintText = CreateText(listPanel.transform, "Hint", 11, TextAnchor.MiddleCenter,
            new Color(0.5f, 0.55f, 0.65f), FontStyle.Italic);
        hintText.text = "Przytrzymaj TAB aby widziec liste  |  B — sklep";
        PositionRect(hintText, new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(460f, 20f));
    }

    private void CreatePlayerRow(int index)
    {
        float yOffset = -92f - index * 48f;
        Color rowBg = index % 2 == 0 ? RowEven : RowOdd;

        playerRows[index] = CreatePanel(listPanel.transform, "PlayerRow" + index,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, yOffset), new Vector2(-12f, 44f), rowBg);
        playerRows[index].SetActive(false);

        GameObject dotObj = new GameObject("Dot" + index);
        dotObj.transform.SetParent(playerRows[index].transform, false);
        playerColorDots[index] = dotObj.AddComponent<Image>();
        playerColorDots[index].color = PlayerColors[index % PlayerColors.Length];
        RectTransform dotRect = dotObj.GetComponent<RectTransform>();
        dotRect.anchorMin = new Vector2(0f, 0.5f);
        dotRect.anchorMax = new Vector2(0f, 0.5f);
        dotRect.pivot = new Vector2(0.5f, 0.5f);
        dotRect.anchoredPosition = new Vector2(22f, 0f);
        dotRect.sizeDelta = new Vector2(14f, 14f);

        playerNameTexts[index] = CreateText(playerRows[index].transform, "Name" + index,
            16, TextAnchor.MiddleLeft, WhiteText, FontStyle.Bold);
        RectTransform nameRect = playerNameTexts[index].GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0f);
        nameRect.anchorMax = new Vector2(0.55f, 1f);
        nameRect.offsetMin = new Vector2(42f, 0f);
        nameRect.offsetMax = new Vector2(0f, 0f);

        playerRoleTexts[index] = CreateText(playerRows[index].transform, "Role" + index,
            13, TextAnchor.MiddleCenter, SubText, FontStyle.Normal);
        RectTransform roleRect = playerRoleTexts[index].GetComponent<RectTransform>();
        roleRect.anchorMin = new Vector2(0.55f, 0f);
        roleRect.anchorMax = new Vector2(0.80f, 1f);
        roleRect.offsetMin = Vector2.zero;
        roleRect.offsetMax = Vector2.zero;

        playerPingTexts[index] = CreateText(playerRows[index].transform, "Ping" + index,
            13, TextAnchor.MiddleRight, SubText, FontStyle.Normal);
        RectTransform pingRect = playerPingTexts[index].GetComponent<RectTransform>();
        pingRect.anchorMin = new Vector2(0.80f, 0f);
        pingRect.anchorMax = new Vector2(1f, 1f);
        pingRect.offsetMin = Vector2.zero;
        pingRect.offsetMax = new Vector2(-12f, 0f);
    }

    private void RefreshPlayerData()
    {
        NetworkPlayer[] players = FindObjectsByType<NetworkPlayer>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < 4; i++)
        {
            if (i < players.Length && players[i] != null && players[i].IsSpawned)
            {
                NetworkPlayer np = players[i];
                playerRows[i].SetActive(true);

                int colorIdx = np.PlayerIndex % PlayerColors.Length;
                playerColorDots[i].color = PlayerColors[colorIdx];

                string displayName = np.PlayerName;
                if (np.IsOwner)
                {
                    displayName += "  (TY)";
                }
                playerNameTexts[i].text = displayName;

                bool isHost = np.OwnerClientId == 0;
                playerRoleTexts[i].text = isHost ? "HOST" : "KLIENT";
                playerRoleTexts[i].color = isHost
                    ? new Color(0.3f, 0.85f, 0.45f)
                    : SubText;

                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                {
                    playerPingTexts[i].text = isHost ? "—" : "~20ms";
                }
                else
                {
                    playerPingTexts[i].text = isHost ? "—" : "~";
                }
            }
            else
            {
                playerRows[i].SetActive(false);
            }
        }

        int count = 0;
        foreach (NetworkPlayer p in players)
        {
            if (p != null && p.IsSpawned) count++;
        }
        titleText.text = "\u2726  GRACZE ONLINE — " + count + "/4  \u2726";
    }

    private GameObject CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image img = panel.AddComponent<Image>();
        img.color = color;

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return panel;
    }

    private void CreatePanelBorder(Transform parent, Color borderColor)
    {
        float t = 2f;
        CreatePanel(parent, "BT", new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, t), borderColor);
        CreatePanel(parent, "BB", new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, t), borderColor);
        CreatePanel(parent, "BL", new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(0f, 0.5f), Vector2.zero, new Vector2(t, 0f), borderColor);
        CreatePanel(parent, "BR", new Vector2(1f, 0f), new Vector2(1f, 1f),
            new Vector2(1f, 0.5f), Vector2.zero, new Vector2(t, 0f), borderColor);
    }

    private Text CreateText(Transform parent, string name, int size,
        TextAnchor alignment, Color color, FontStyle style)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        Text text = textObj.AddComponent<Text>();
        text.font = cachedFont;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.fontStyle = style;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private void PositionRect(Component comp, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        RectTransform rect = comp.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
    }
}
