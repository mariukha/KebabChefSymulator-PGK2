using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Multiplayer lobby UI with Host/Join functionality.
/// Styled to match the game's dark-blue kitchen aesthetic.
/// Created programmatically using uGUI (no prefabs).
/// Shows on startup, hides when game session begins.
/// </summary>
public class LobbyUI : MonoBehaviour
{
    // UI Color palette (matches KitchenOrderBoard style)
    private static readonly Color PanelBackground = new Color(0.035f, 0.05f, 0.075f, 0.96f);
    private static readonly Color CardBackground = new Color(0.07f, 0.09f, 0.13f, 0.92f);
    private static readonly Color FrameColor = new Color(0.12f, 0.12f, 0.13f);
    private static readonly Color HeaderColor = new Color(0.07f, 0.11f, 0.18f);
    private static readonly Color GoldText = new Color(1f, 0.92f, 0.65f);
    private static readonly Color WhiteText = new Color(0.95f, 0.97f, 1f, 0.94f);
    private static readonly Color SubText = new Color(0.65f, 0.70f, 0.78f);
    private static readonly Color HostButtonColor = new Color(0.22f, 0.72f, 0.35f);
    private static readonly Color HostButtonHover = new Color(0.28f, 0.82f, 0.42f);
    private static readonly Color JoinButtonColor = new Color(0.25f, 0.55f, 0.88f);
    private static readonly Color JoinButtonHover = new Color(0.35f, 0.65f, 0.95f);
    private static readonly Color DisconnectColor = new Color(0.82f, 0.25f, 0.22f);
    private static readonly Color InputFieldBg = new Color(0.06f, 0.08f, 0.11f);

    private Canvas lobbyCanvas;
    private GameObject lobbyPanel;
    private InputField ipInputField;
    private Text statusText;
    private Text playerCountText;
    private Button hostButton;
    private Button joinButton;
    private Button disconnectButton;
    private GameObject connectedPanel;

    private bool isLobbyVisible = true;
    private float statusClearTime;

    public bool IsLobbyOpen => isLobbyVisible && lobbyPanel != null && lobbyPanel.activeSelf;

    private void Awake()
    {
        CreateLobbyCanvas();
    }

    private void Update()
    {
        if (lobbyPanel == null)
        {
            return;
        }

        UpdateConnectionState();

        if (statusClearTime > 0f && Time.unscaledTime >= statusClearTime)
        {
            statusClearTime = 0f;
            if (statusText != null)
            {
                statusText.text = string.Empty;
            }
        }
    }

    public void ShowLobby()
    {
        isLobbyVisible = true;
        if (lobbyCanvas != null)
        {
            lobbyCanvas.enabled = true;
        }
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void HideLobby()
    {
        isLobbyVisible = false;
        if (lobbyCanvas != null)
        {
            lobbyCanvas.enabled = false;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void CreateLobbyCanvas()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        // Canvas
        GameObject canvasObject = new GameObject("LobbyCanvas");
        canvasObject.transform.SetParent(transform, false);
        lobbyCanvas = canvasObject.AddComponent<Canvas>();
        lobbyCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        lobbyCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        // Full-screen darkened backdrop
        GameObject backdrop = CreatePanel(canvasObject.transform, "Backdrop",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.75f));
        RectTransform backdropRect = backdrop.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        // Main panel (centered card)
        lobbyPanel = CreatePanel(canvasObject.transform, "LobbyPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(520f, 480f), PanelBackground);

        // Frame border
        CreatePanelBorder(lobbyPanel.transform, new Vector2(520f, 480f), FrameColor);

        // Header
        GameObject header = CreatePanel(lobbyPanel.transform, "Header",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -8f), new Vector2(-16f, 60f), HeaderColor);

        // Title
        Text titleText = CreateText(header.transform, "Title", font, 22, TextAnchor.MiddleCenter,
            GoldText, FontStyle.Bold);
        titleText.text = "\u2726  KEBAB CHEF  —  MULTIPLAYER  \u2726";
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        // Subtitle
        Text subTitle = CreateText(lobbyPanel.transform, "Subtitle", font, 13, TextAnchor.MiddleCenter,
            SubText, FontStyle.Normal);
        subTitle.text = "Wspolna kuchnia — do 4 graczy w sieci LAN";
        PositionRect(subTitle, new Vector2(0.5f, 1f), new Vector2(0f, -85f), new Vector2(460f, 22f));

        // === Host Section ===
        hostButton = CreateStyledButton(lobbyPanel.transform, "HostButton", font,
            "\u25B6  STWORZ GRE (HOST)", HostButtonColor, HostButtonHover,
            new Vector2(0.5f, 1f), new Vector2(0f, -135f), new Vector2(440f, 52f));
        hostButton.onClick.AddListener(OnHostClicked);

        // === Separator ===
        CreatePanel(lobbyPanel.transform, "Separator",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -180f), new Vector2(380f, 1f), new Color(1f, 1f, 1f, 0.08f));

        Text orText = CreateText(lobbyPanel.transform, "OrText", font, 12, TextAnchor.MiddleCenter,
            SubText, FontStyle.Normal);
        orText.text = "lub dolacz do istniejacego serwera";
        PositionRect(orText, new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(460f, 20f));

        // === IP Input ===
        Text ipLabel = CreateText(lobbyPanel.transform, "IpLabel", font, 12, TextAnchor.MiddleLeft,
            WhiteText, FontStyle.Normal);
        ipLabel.text = "Adres IP serwera:";
        PositionRect(ipLabel, new Vector2(0.5f, 1f), new Vector2(-110f, -235f), new Vector2(200f, 22f));

        ipInputField = CreateInputField(lobbyPanel.transform, "IpInput", font,
            "127.0.0.1", "Wpisz adres IP...",
            new Vector2(0.5f, 1f), new Vector2(0f, -268f), new Vector2(440f, 42f));

        // === Join button ===
        joinButton = CreateStyledButton(lobbyPanel.transform, "JoinButton", font,
            "\u279C  DOLACZ DO GRY (JOIN)", JoinButtonColor, JoinButtonHover,
            new Vector2(0.5f, 1f), new Vector2(0f, -325f), new Vector2(440f, 48f));
        joinButton.onClick.AddListener(OnJoinClicked);

        // === Status ===
        statusText = CreateText(lobbyPanel.transform, "StatusText", font, 13, TextAnchor.MiddleCenter,
            GoldText, FontStyle.Normal);
        statusText.text = string.Empty;
        PositionRect(statusText, new Vector2(0.5f, 1f), new Vector2(0f, -380f), new Vector2(460f, 24f));

        // === Connected Panel (shown after connection) ===
        connectedPanel = CreatePanel(lobbyPanel.transform, "ConnectedPanel",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 15f), new Vector2(440f, 60f), new Color(0.08f, 0.14f, 0.1f, 0.9f));
        connectedPanel.SetActive(false);

        playerCountText = CreateText(connectedPanel.transform, "PlayerCount", font, 14,
            TextAnchor.MiddleCenter, new Color(0.4f, 0.9f, 0.5f), FontStyle.Bold);
        playerCountText.text = "Polaczono: 1 gracz(y)";
        RectTransform pcRect = playerCountText.GetComponent<RectTransform>();
        pcRect.anchorMin = new Vector2(0f, 0.5f);
        pcRect.anchorMax = new Vector2(0.7f, 0.5f);
        pcRect.pivot = new Vector2(0.5f, 0.5f);
        pcRect.anchoredPosition = new Vector2(0f, 5f);
        pcRect.sizeDelta = new Vector2(0f, 24f);

        disconnectButton = CreateStyledButton(connectedPanel.transform, "DisconnectBtn", font,
            "ROZLACZ", DisconnectColor, new Color(0.9f, 0.3f, 0.25f),
            new Vector2(0.85f, 0.5f), new Vector2(0f, 0f), new Vector2(110f, 34f));
        disconnectButton.onClick.AddListener(OnDisconnectClicked);

        // Ensure EventSystem
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    private void UpdateConnectionState()
    {
        bool connected = NetworkSetup.Instance != null && NetworkSetup.Instance.IsNetworkActive;

        if (hostButton != null)
        {
            hostButton.interactable = !connected;
        }

        if (joinButton != null)
        {
            joinButton.interactable = !connected;
        }

        if (ipInputField != null)
        {
            ipInputField.interactable = !connected;
        }

        if (connectedPanel != null)
        {
            connectedPanel.SetActive(connected);
        }

        if (connected && playerCountText != null)
        {
            int count = NetworkSetup.Instance.ConnectedPlayerCount;
            string role = NetworkSetup.Instance.IsHost ? "HOST" : "KLIENT";
            playerCountText.text = role + " — Graczy: " + count;
        }

        // Auto-hide lobby once game is active for a few seconds
        if (isLobbyVisible && connected && NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening &&
            NetworkManager.Singleton.LocalTime.Time > 3.0)
        {
            HideLobby();
        }
    }

    private void OnHostClicked()
    {
        if (NetworkSetup.Instance == null)
        {
            SetStatus("Blad: NetworkSetup nie istnieje.", 5f);
            return;
        }

        SetStatus("Tworzenie serwera...", 0f);

        if (NetworkSetup.Instance.StartHost())
        {
            SetStatus("Serwer uruchomiony! Oczekiwanie na graczy...", 0f);
        }
        else
        {
            SetStatus("Blad: nie udalo sie uruchomic serwera.", 5f);
        }
    }

    private void OnJoinClicked()
    {
        if (NetworkSetup.Instance == null)
        {
            SetStatus("Blad: NetworkSetup nie istnieje.", 5f);
            return;
        }

        string ip = ipInputField != null ? ipInputField.text.Trim() : "127.0.0.1";
        if (string.IsNullOrWhiteSpace(ip))
        {
            ip = "127.0.0.1";
        }

        SetStatus("Laczenie z " + ip + "...", 0f);

        if (NetworkSetup.Instance.StartClient(ip))
        {
            SetStatus("Laczenie...", 0f);
        }
        else
        {
            SetStatus("Blad: nie udalo sie polaczyc.", 5f);
        }
    }

    private void OnDisconnectClicked()
    {
        if (NetworkSetup.Instance != null)
        {
            NetworkSetup.Instance.Disconnect();
        }

        SetStatus("Rozlaczono.", 3f);
    }

    private void SetStatus(string message, float clearAfter)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }

        statusClearTime = clearAfter > 0f ? Time.unscaledTime + clearAfter : 0f;
    }

    // === UI Builder Helpers ===

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

    private void CreatePanelBorder(Transform parent, Vector2 panelSize, Color borderColor)
    {
        float thickness = 2f;
        CreatePanel(parent, "BorderTop",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(0f, thickness), borderColor);
        CreatePanel(parent, "BorderBottom",
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            Vector2.zero, new Vector2(0f, thickness), borderColor);
        CreatePanel(parent, "BorderLeft",
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            Vector2.zero, new Vector2(thickness, 0f), borderColor);
        CreatePanel(parent, "BorderRight",
            new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
            Vector2.zero, new Vector2(thickness, 0f), borderColor);
    }

    private Text CreateText(Transform parent, string name, Font font, int size,
        TextAnchor alignment, Color color, FontStyle style)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        Text text = textObj.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.fontStyle = style;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private void PositionRect(Component comp, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        RectTransform rect = comp.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
    }

    private Button CreateStyledButton(Transform parent, string name, Font font,
        string label, Color normalColor, Color hoverColor,
        Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        GameObject btnObj = CreatePanel(parent, name, anchor, anchor,
            new Vector2(0.5f, 0.5f), anchoredPos, size, normalColor);

        Button btn = btnObj.AddComponent<Button>();
        Image btnImage = btnObj.GetComponent<Image>();

        ColorBlock colors = btn.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = hoverColor;
        colors.pressedColor = hoverColor * 0.85f;
        colors.selectedColor = normalColor;
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        btn.colors = colors;
        btn.targetGraphic = btnImage;

        Text btnText = CreateText(btnObj.transform, "Label", font, 15,
            TextAnchor.MiddleCenter, WhiteText, FontStyle.Bold);
        btnText.text = label;
        RectTransform textRect = btnText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return btn;
    }

    private InputField CreateInputField(Transform parent, string name, Font font,
        string defaultText, string placeholder,
        Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        GameObject inputObj = CreatePanel(parent, name, anchor, anchor,
            new Vector2(0.5f, 0.5f), anchoredPos, size, InputFieldBg);

        // Border
        CreatePanelBorder(inputObj.transform, size, new Color(0.2f, 0.25f, 0.35f));

        // Placeholder
        Text placeholderText = CreateText(inputObj.transform, "Placeholder", font, 14,
            TextAnchor.MiddleLeft, new Color(0.4f, 0.45f, 0.55f), FontStyle.Italic);
        placeholderText.text = placeholder;
        RectTransform phRect = placeholderText.GetComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = new Vector2(14f, 0f);
        phRect.offsetMax = new Vector2(-14f, 0f);

        // Input text
        Text inputText = CreateText(inputObj.transform, "Text", font, 15,
            TextAnchor.MiddleLeft, WhiteText, FontStyle.Normal);
        RectTransform itRect = inputText.GetComponent<RectTransform>();
        itRect.anchorMin = Vector2.zero;
        itRect.anchorMax = Vector2.one;
        itRect.offsetMin = new Vector2(14f, 0f);
        itRect.offsetMax = new Vector2(-14f, 0f);

        InputField inputField = inputObj.AddComponent<InputField>();
        inputField.textComponent = inputText;
        inputField.placeholder = placeholderText;
        inputField.text = defaultText;
        inputField.characterLimit = 45;

        return inputField;
    }
}
