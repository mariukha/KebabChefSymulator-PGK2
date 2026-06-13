using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{

    private static readonly Color PanelBackground = new Color(0.012f, 0.014f, 0.018f, 0.95f);
    private static readonly Color CardBackground = new Color(0.035f, 0.045f, 0.06f, 0.76f);
    private static readonly Color FrameColor = new Color(0.16f, 0.17f, 0.19f, 0.95f);
    private static readonly Color HeaderColor = new Color(0.032f, 0.038f, 0.052f, 0.94f);
    private static readonly Color GoldText = new Color(0.86f, 0.68f, 0.28f);
    private static readonly Color WhiteText = new Color(0.9f, 0.91f, 0.93f, 0.92f);
    private static readonly Color SubText = new Color(0.58f, 0.62f, 0.69f);
    private static readonly Color HostButtonColor = new Color(0.05f, 0.34f, 0.16f);
    private static readonly Color HostButtonHover = new Color(0.08f, 0.46f, 0.22f);
    private static readonly Color JoinButtonColor = new Color(0.06f, 0.19f, 0.42f);
    private static readonly Color JoinButtonHover = new Color(0.09f, 0.28f, 0.58f);
    private static readonly Color DisconnectColor = new Color(0.48f, 0.08f, 0.07f);
    private static readonly Color InputFieldBg = new Color(0.035f, 0.045f, 0.06f, 0.95f);

    private Canvas lobbyCanvas;
    private GameObject lobbyPanel;
    private InputField ipInputField;
    private Text statusText;
    private Text playerCountText;
    private Button hostButton;
    private Button joinButton;
    private Button disconnectButton;
    private GameObject connectedPanel;
    private InputField nicknameInputField;

    public static string LocalPlayerNickname { get; private set; } = "Gracz";

    private bool isLobbyVisible;
    private float statusClearTime;
    private float lastActionTime;

    public bool IsLobbyOpen => isLobbyVisible && lobbyPanel != null && lobbyPanel.activeSelf;

    private void Awake()
    {
        CreateLobbyCanvas();
        if (lobbyCanvas != null)
        {
            lobbyCanvas.enabled = false;
        }
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (lobbyPanel == null)
        {
            return;
        }

        UpdateConnectionState();

        if (MainMenuUI.IsSoloMode)
        {
            if (isLobbyVisible)
            {
                HideLobby();
            }

            return;
        }

        bool settingsOpen = SettingsMenuUI.Instance != null && SettingsMenuUI.Instance.IsOpen;
        if (!settingsOpen && Input.GetKeyDown(KeyCode.F1))
        {
            if (isLobbyVisible)
            {
                HideLobby();
            }
            else
            {
                ShowLobby();
            }
        }

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
        if (MainMenuUI.IsSoloMode)
        {
            HideLobby();
            return;
        }

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
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(false);
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

        GameObject canvasObject = new GameObject("LobbyCanvas");
        canvasObject.transform.SetParent(transform, false);
        lobbyCanvas = canvasObject.AddComponent<Canvas>();
        lobbyCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        lobbyCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject backdrop = CreatePanel(canvasObject.transform, "Backdrop",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.52f));
        RectTransform backdropRect = backdrop.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        lobbyPanel = CreatePanel(canvasObject.transform, "LobbyPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(500f, 540f), PanelBackground);

        CreatePanelBorder(lobbyPanel.transform, new Vector2(500f, 540f), FrameColor);

        GameObject header = CreatePanel(lobbyPanel.transform, "Header",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -8f), new Vector2(-16f, 70f), HeaderColor);

        Text titleText = CreateText(header.transform, "Title", font, 25, TextAnchor.MiddleCenter,
            GoldText, FontStyle.Bold);
        titleText.text = "ONLINE LOBBY";
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        Text subTitle = CreateText(lobbyPanel.transform, "Subtitle", font, 15, TextAnchor.MiddleCenter,
            SubText, FontStyle.Normal);
        subTitle.text = "Hostuj pokoj lub dolacz kodem znajomego.";
        PositionRect(subTitle, new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(430f, 22f));

        Text nickLabel = CreateText(lobbyPanel.transform, "NickLabel", font, 14, TextAnchor.MiddleLeft,
            WhiteText, FontStyle.Normal);
        nickLabel.text = "NICK";
        PositionRect(nickLabel, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(420f, 22f));

        nicknameInputField = CreateInputField(lobbyPanel.transform, "NickInput", font,
            "Gracz", "Wpisz nick...",
            new Vector2(0.5f, 1f), new Vector2(0f, -164f), new Vector2(420f, 42f));

        hostButton = CreateStyledButton(lobbyPanel.transform, "HostButton", font,
            "UTWORZ POKOJ", HostButtonColor, HostButtonHover,
            new Vector2(0.5f, 1f), new Vector2(0f, -226f), new Vector2(420f, 50f));
        hostButton.onClick.AddListener(OnHostClicked);

        CreatePanel(lobbyPanel.transform, "Separator",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -276f), new Vector2(360f, 1f), new Color(1f, 1f, 1f, 0.08f));

        Text orText = CreateText(lobbyPanel.transform, "OrText", font, 14, TextAnchor.MiddleCenter,
            SubText, FontStyle.Normal);
        orText.text = "DOLACZ DO ISTNIEJACEJ SESJI";
        PositionRect(orText, new Vector2(0.5f, 1f), new Vector2(0f, -300f), new Vector2(430f, 20f));

        Text codeLabel = CreateText(lobbyPanel.transform, "CodeLabel", font, 14, TextAnchor.MiddleLeft,
            WhiteText, FontStyle.Normal);
        codeLabel.text = "KOD POKOJU";
        PositionRect(codeLabel, new Vector2(0.5f, 1f), new Vector2(0f, -332f), new Vector2(420f, 22f));

        ipInputField = CreateInputField(lobbyPanel.transform, "CodeInput", font,
            "", "Wpisz kod pokoju...",
            new Vector2(0.5f, 1f), new Vector2(0f, -366f), new Vector2(420f, 42f));

        joinButton = CreateStyledButton(lobbyPanel.transform, "JoinButton", font,
            "DOLACZ DO POKOJU", JoinButtonColor, JoinButtonHover,
            new Vector2(0.5f, 1f), new Vector2(0f, -424f), new Vector2(420f, 48f));
        joinButton.onClick.AddListener(OnJoinClicked);

        statusText = CreateText(lobbyPanel.transform, "StatusText", font, 15, TextAnchor.MiddleCenter,
            GoldText, FontStyle.Normal);
        statusText.text = string.Empty;
        PositionRect(statusText, new Vector2(0.5f, 1f), new Vector2(0f, -470f), new Vector2(430f, 24f));

        connectedPanel = CreatePanel(lobbyPanel.transform, "ConnectedPanel",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 16f), new Vector2(420f, 54f), CardBackground);
        connectedPanel.SetActive(false);

        playerCountText = CreateText(connectedPanel.transform, "PlayerCount", font, 16,
            TextAnchor.MiddleCenter, new Color(0.4f, 0.9f, 0.5f), FontStyle.Bold);
        playerCountText.text = "Polaczono: 1 gracz(y)";
        RectTransform pcRect = playerCountText.GetComponent<RectTransform>();
        pcRect.anchorMin = new Vector2(0f, 0.5f);
        pcRect.anchorMax = new Vector2(0.7f, 0.5f);
        pcRect.pivot = new Vector2(0.5f, 0.5f);
        pcRect.anchoredPosition = new Vector2(0f, 5f);
        pcRect.sizeDelta = new Vector2(0f, 24f);

        disconnectButton = CreateStyledButton(connectedPanel.transform, "DisconnectBtn", font,
            "ROZLACZ", DisconnectColor, new Color(0.58f, 0.12f, 0.10f),
            new Vector2(0.85f, 0.5f), new Vector2(0f, 0f), new Vector2(104f, 32f));
        disconnectButton.onClick.AddListener(OnDisconnectClicked);

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

    }

    private async void OnHostClicked()
    {
        if (Time.unscaledTime - lastActionTime < 3f) return;
        lastActionTime = Time.unscaledTime;

        if (RelayManager.Instance == null)
        {
            SetStatus("Blad: RelayManager nie istnieje.", 5f);
            return;
        }

        SaveNickname();
        SetStatus("Tworzenie pokoju...", 0f);

        if (hostButton != null) hostButton.interactable = false;
        if (joinButton != null) joinButton.interactable = false;

        string code = await RelayManager.Instance.CreateRelay(3);

        if (hostButton != null) hostButton.interactable = true;
        if (joinButton != null) joinButton.interactable = true;

        if (code != null)
        {
            SetStatus("POKOJ UTWORZONY!  Kod:  " + code, 0f);

            if (ipInputField != null)
            {
                ipInputField.text = code;
            }

            Invoke(nameof(HideLobby), 2f);
        }
        else
        {
            SetStatus("Blad: " + RelayManager.Instance.LastError, 5f);
        }
    }

    private async void OnJoinClicked()
    {
        if (Time.unscaledTime - lastActionTime < 3f) return;
        lastActionTime = Time.unscaledTime;

        if (RelayManager.Instance == null)
        {
            SetStatus("Blad: RelayManager nie istnieje.", 5f);
            return;
        }

        string code = ipInputField != null ? ipInputField.text.Trim().ToUpperInvariant() : "";
        if (string.IsNullOrWhiteSpace(code))
        {
            SetStatus("Wpisz kod pokoju!", 3f);
            return;
        }

        SaveNickname();
        SetStatus("Dolaczanie do pokoju " + code + "...", 0f);

        if (hostButton != null) hostButton.interactable = false;
        if (joinButton != null) joinButton.interactable = false;

        bool success = await RelayManager.Instance.JoinRelay(code);

        if (hostButton != null) hostButton.interactable = true;
        if (joinButton != null) joinButton.interactable = true;

        if (success)
        {
            SetStatus("Polaczono! Kod pokoju: " + code, 0f);

            Invoke(nameof(HideLobby), 2f);
        }
        else
        {
            SetStatus("Blad: " + RelayManager.Instance.LastError, 5f);
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

    private void SaveNickname()
    {
        string nick = nicknameInputField != null ? nicknameInputField.text.Trim() : "";
        if (string.IsNullOrWhiteSpace(nick))
        {
            nick = "Gracz";
        }

        if (nick.Length > 20)
        {
            nick = nick.Substring(0, 20);
        }

        LocalPlayerNickname = nick;
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
        Shadow shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        shadow.effectDistance = new Vector2(1f, -1f);
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

        Text btnText = CreateText(btnObj.transform, "Label", font, 17,
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

        CreatePanelBorder(inputObj.transform, size, new Color(0.2f, 0.25f, 0.35f));

        Text placeholderText = CreateText(inputObj.transform, "Placeholder", font, 15,
            TextAnchor.MiddleLeft, new Color(0.4f, 0.45f, 0.55f), FontStyle.Italic);
        placeholderText.text = placeholder;
        RectTransform phRect = placeholderText.GetComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = new Vector2(14f, 0f);
        phRect.offsetMax = new Vector2(-14f, 0f);

        Text inputText = CreateText(inputObj.transform, "Text", font, 16,
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
