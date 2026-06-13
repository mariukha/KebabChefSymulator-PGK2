/// \file ShopUI.cs
/// \brief Plik zawierający klasę interfejsu użytkownika sklepu z ulepszeniami.
/// \details Definiuje klasę ShopUI odpowiedzialną za tworzenie, wyświetlanie i obsługę
/// interfejsu graficznego sklepu z ulepszeniami. Panel sklepu jest budowany programowo
/// z użyciem komponentów Unity UI (Canvas, Image, Text, Button). Obsługuje animacje
/// otwierania/zamykania, komunikaty zwrotne po zakupie oraz integrację z systemem sieciowym.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Klasa odpowiedzialna za interfejs użytkownika sklepu z ulepszeniami.
/// </summary>
/// <remarks>
/// Tworzy kompletny panel sklepu programowo (bez prefabów) z wykorzystaniem Unity UI.
/// Panel zawiera nagłówek z tytułem i saldem, listę dostępnych ulepszeń z przyciskami zakupu
/// oraz stopkę z komunikatami zwrotnymi i podpowiedziami.
/// 
/// Obsługuje:
/// - Otwieranie/zamykanie sklepu klawiszem B i Escape
/// - Animację pojawiania/znikania panelu (fade + scale)
/// - Odświeżanie zawartości w czasie rzeczywistym (saldo, poziomy, koszty)
/// - Komunikaty zwrotne o wyniku zakupu (sukces/porażka)
/// - Efekty wizualne i dźwiękowe przy zakupie
/// - Obsługę trybu sieciowego (przekazywanie żądań zakupu do serwera)
///
/// Współpracuje z <see cref="ShopManager"/>, <see cref="EconomyManager"/>,
/// <see cref="AudioManager"/>, <see cref="VFXManager"/>, <see cref="PostProcessSetup"/>
/// oraz <see cref="CameraEffects"/>.
/// </remarks>
public class ShopUI : MonoBehaviour
{
    /// <summary>
    /// Komponent Canvas używany do renderowania interfejsu sklepu.
    /// </summary>
    private Canvas shopCanvas;

    /// <summary>
    /// Główny obiekt panelu sklepu, który jest aktywowany/dezaktywowany przy otwieraniu/zamykaniu.
    /// </summary>
    private GameObject shopPanel;

    /// <summary>
    /// Element tekstowy wyświetlający aktualne saldo gracza w nagłówku sklepu.
    /// </summary>
    private Text balanceText;

    /// <summary>
    /// Element tekstowy wyświetlający tytuł panelu sklepu ("ULEPSZENIA").
    /// </summary>
    private Text titleText;

    /// <summary>
    /// Element tekstowy wyświetlający podpowiedź zamknięcia sklepu w stopce.
    /// </summary>
    private Text hintText;

    /// <summary>
    /// Element tekstowy wyświetlający komunikat zwrotny po próbie zakupu ulepszenia.
    /// </summary>
    private Text purchaseFeedbackText;

    /// <summary>
    /// Lista wierszy ulepszeń wyświetlanych w panelu sklepu.
    /// </summary>
    /// <remarks>
    /// Każdy wiersz odpowiada jednej definicji ulepszenia i zawiera referencje
    /// do wszystkich elementów UI potrzebnych do wyświetlenia i interakcji.
    /// </remarks>
    private readonly List<ShopUpgradeRow> upgradeRows = new List<ShopUpgradeRow>();

    /// <summary>
    /// Flaga określająca, czy sklep jest aktualnie otwarty.
    /// </summary>
    private bool isOpen;

    /// <summary>
    /// Licznik czasu pozostałego do ukrycia komunikatu zwrotnego.
    /// </summary>
    /// <remarks>
    /// Odlicza w dół od <see cref="FeedbackDuration"/>. Gdy osiągnie zero,
    /// tekst komunikatu jest czyszczony.
    /// </remarks>
    private float feedbackTimer;

    /// <summary>
    /// Bieżący postęp animacji otwierania/zamykania panelu (0 = zamknięty, 1 = otwarty).
    /// </summary>
    private float panelAnimationProgress;

    /// <summary>
    /// Komponent CanvasGroup panelu, używany do kontroli przezroczystości podczas animacji.
    /// </summary>
    private CanvasGroup panelCanvasGroup;

    /// <summary>
    /// Czas trwania wyświetlania komunikatu zwrotnego po zakupie (w sekundach).
    /// </summary>
    private const float FeedbackDuration = 2.5f;

    /// <summary>
    /// Szybkość animacji otwierania/zamykania panelu sklepu.
    /// </summary>
    /// <remarks>
    /// Wyższa wartość oznacza szybszą animację. Używana z <see cref="Time.unscaledDeltaTime"/>
    /// aby animacja działała niezależnie od skali czasu gry.
    /// </remarks>
    private const float AnimationSpeed = 8f;

    /// <summary>
    /// Kolor półprzezroczystego tła nakładki zasłaniającej ekran za panelem sklepu.
    /// </summary>
    private static readonly Color BackgroundOverlay = new Color(0.005f, 0.008f, 0.012f, 0.82f);

    /// <summary>
    /// Kolor tła głównego panelu sklepu.
    /// </summary>
    private static readonly Color PanelColor = new Color(0.018f, 0.022f, 0.028f, 0.98f);

    /// <summary>
    /// Kolor obramowania panelu sklepu.
    /// </summary>
    private static readonly Color PanelBorderColor = new Color(0.15f, 0.16f, 0.17f, 0.95f);

    /// <summary>
    /// Kolor tła paska nagłówka panelu sklepu.
    /// </summary>
    private static readonly Color HeaderColor = new Color(0.028f, 0.034f, 0.044f, 0.96f);

    /// <summary>
    /// Kolor tekstu tytułu "ULEPSZENIA" w nagłówku.
    /// </summary>
    private static readonly Color TitleColor = new Color(0.875f, 0.725f, 0.32f, 1f);

    /// <summary>
    /// Kolor tekstu wyświetlającego wartość salda gracza.
    /// </summary>
    private static readonly Color BalanceValueColor = new Color(0.22f, 0.82f, 0.42f, 1f);

    /// <summary>
    /// Kolor tła wiersza ulepszenia na liście.
    /// </summary>
    private static readonly Color RowBackgroundColor = new Color(0.035f, 0.043f, 0.056f, 0.94f);

    /// <summary>
    /// Kolor nazwy ulepszenia w wierszu.
    /// </summary>
    private static readonly Color NameColor = new Color(0.93f, 0.97f, 1f, 1f);

    /// <summary>
    /// Kolor opisu ulepszenia w wierszu.
    /// </summary>
    private static readonly Color DescriptionColor = new Color(0.65f, 0.72f, 0.82f, 1f);

    /// <summary>
    /// Kolor aktywnego wskaźnika poziomu ulepszenia.
    /// </summary>
    private static readonly Color LevelActiveColor = new Color(0.875f, 0.78f, 0.52f, 1f);

    /// <summary>
    /// Kolor tekstu kosztu ulepszenia, gdy gracz posiada wystarczające środki.
    /// </summary>
    private static readonly Color CostColor = new Color(0.96f, 0.97f, 0.99f, 1f);

    /// <summary>
    /// Kolor przycisku zakupu w stanie normalnym (gracz może kupić).
    /// </summary>
    private static readonly Color ButtonNormalColor = new Color(0.08f, 0.38f, 0.18f, 1f);

    /// <summary>
    /// Kolor przycisku zakupu w stanie wyłączonym (brak środków).
    /// </summary>
    private static readonly Color ButtonDisabledColor = new Color(0.12f, 0.13f, 0.15f, 0.82f);

    /// <summary>
    /// Kolor przycisku zakupu dla ulepszenia na maksymalnym poziomie.
    /// </summary>
    private static readonly Color ButtonMaxedColor = new Color(0.14f, 0.28f, 0.42f, 0.9f);

    /// <summary>
    /// Kolor tekstu na przyciskach zakupu ("KUP" / "MAX").
    /// </summary>
    private static readonly Color ButtonTextColor = new Color(0.98f, 0.99f, 1f, 1f);

    /// <summary>
    /// Kolor tekstu podpowiedzi w stopce panelu.
    /// </summary>
    private static readonly Color HintColor = new Color(0.50f, 0.58f, 0.68f, 0.85f);

    /// <summary>
    /// Kolor komunikatu zwrotnego o pomyślnym zakupie.
    /// </summary>
    private static readonly Color FeedbackSuccessColor = new Color(0.22f, 0.82f, 0.42f, 1f);

    /// <summary>
    /// Kolor komunikatu zwrotnego o nieudanym zakupie.
    /// </summary>
    private static readonly Color FeedbackFailColor = new Color(0.92f, 0.24f, 0.2f, 1f);

    /// <summary>
    /// Kolor linii separatora między sekcjami panelu.
    /// </summary>
    private static readonly Color DividerColor = new Color(0.16f, 0.17f, 0.18f, 0.62f);

    /// <summary>
    /// Buforowana referencja do czcionki używanej w interfejsie sklepu.
    /// </summary>
    /// <remarks>
    /// Ładowana raz przy tworzeniu panelu. Preferowana jest czcionka LegacyRuntime,
    /// z fallbackiem na Arial.
    /// </remarks>
    private Font cachedFont;

    /// <summary>
    /// Buforowana referencja do komponentu interfejsu lobby.
    /// </summary>
    /// <remarks>
    /// Wykorzystywana do sprawdzania, czy lobby jest otwarte, w celu blokowania
    /// otwierania sklepu, gdy lobby jest aktywne.
    /// </remarks>
    private LobbyUI cachedLobbyUI;

    /// <summary>
    /// Pobiera informację, czy panel sklepu jest aktualnie otwarty.
    /// </summary>
    /// <value><c>true</c> jeśli sklep jest otwarty; <c>false</c> w przeciwnym razie.</value>
    public bool IsShopOpen => isOpen;

    /// <summary>
    /// Inicjalizuje interfejs sklepu przy starcie obiektu.
    /// </summary>
    /// <remarks>
    /// Zapewnia istnienie komponentu EventSystem w scenie (wymagany do obsługi interakcji UI)
    /// i tworzy kompletny panel sklepu programowo.
    /// </remarks>
    private void Awake()
    {
        EnsureEventSystem();
        CreateShopCanvas();
    }

    /// <summary>
    /// Obsługuje logikę aktualizacji sklepu w każdej klatce.
    /// </summary>
    /// <remarks>
    /// Wykonuje następujące operacje:
    /// - Buforuje referencję do <see cref="LobbyUI"/>
    /// - Obsługuje klawisz B (otwieranie/zamykanie sklepu, o ile lobby i ustawienia są zamknięte)
    /// - Obsługuje klawisz Escape (zamykanie sklepu)
    /// - Aktualizuje animację panelu
    /// - Aktualizuje timer komunikatu zwrotnego
    /// - Odświeża zawartość sklepu gdy jest otwarty
    /// </remarks>
    private void Update()
    {
        if (cachedLobbyUI == null)
        {
            cachedLobbyUI = FindFirstObjectByType<LobbyUI>();
        }
        bool lobbyOpen = cachedLobbyUI != null && cachedLobbyUI.IsLobbyOpen;
        bool settingsOpen = SettingsMenuUI.Instance != null && SettingsMenuUI.Instance.IsOpen;

        if (Input.GetKeyDown(KeyCode.B) && !lobbyOpen && !settingsOpen)
        {
            ToggleShop();
        }

        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseShop();
        }

        UpdateAnimation();
        UpdateFeedback();

        if (isOpen)
        {
            RefreshContent();
        }
    }

    /// <summary>
    /// Przełącza widoczność panelu sklepu (otwiera jeśli zamknięty, zamyka jeśli otwarty).
    /// </summary>
    public void ToggleShop()
    {
        if (isOpen)
        {
            CloseShop();
        }
        else
        {
            OpenShop();
        }
    }

    /// <summary>
    /// Otwiera panel sklepu z ulepszeniami.
    /// </summary>
    /// <remarks>
    /// Aktywuje panel, odblokowuje kursor myszy i odświeża zawartość sklepu.
    /// Jeśli sklep jest już otwarty, metoda nie wykonuje żadnej operacji.
    /// </remarks>
    public void OpenShop()
    {
        if (isOpen)
        {
            return;
        }

        isOpen = true;
        shopPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        RefreshContent();
    }

    /// <summary>
    /// Zamyka panel sklepu z ulepszeniami.
    /// </summary>
    /// <remarks>
    /// Ukrywa panel, blokuje kursor myszy i ukrywa go.
    /// Jeśli sklep jest już zamknięty, metoda nie wykonuje żadnej operacji.
    /// Faktyczna dezaktywacja obiektu panelu następuje po zakończeniu animacji zamykania
    /// w metodzie <see cref="UpdateAnimation"/>.
    /// </remarks>
    public void CloseShop()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Zapewnia istnienie komponentu EventSystem w scenie.
    /// </summary>
    /// <remarks>
    /// EventSystem jest wymagany do obsługi interakcji z elementami Unity UI (przyciski, kliknięcia).
    /// Jeśli nie istnieje, tworzy nowy obiekt z komponentami <see cref="EventSystem"/>
    /// i <see cref="StandaloneInputModule"/>.
    /// </remarks>
    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    /// <summary>
    /// Aktualizuje animację otwierania/zamykania panelu sklepu.
    /// </summary>
    /// <remarks>
    /// Interpoluje wartość <see cref="panelAnimationProgress"/> w kierunku docelowym (0 lub 1)
    /// z użyciem <see cref="Time.unscaledDeltaTime"/> (niezależnie od skali czasu gry).
    /// Stosuje efekt SmoothStep na przezroczystości (alpha) oraz skalę panelu (0.92 → 1.0).
    /// Dezaktywuje obiekt panelu, gdy animacja zamykania dobiegnie końca.
    /// </remarks>
    private void UpdateAnimation()
    {
        float target = isOpen ? 1f : 0f;
        panelAnimationProgress = Mathf.MoveTowards(panelAnimationProgress, target, Time.unscaledDeltaTime * AnimationSpeed);

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, panelAnimationProgress);
        }

        if (shopPanel != null)
        {
            float scale = Mathf.Lerp(0.92f, 1f, Mathf.SmoothStep(0f, 1f, panelAnimationProgress));
            shopPanel.transform.localScale = new Vector3(scale, scale, 1f);
        }

        if (!isOpen && panelAnimationProgress <= 0.01f && shopPanel.activeSelf)
        {
            shopPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Aktualizuje timer komunikatu zwrotnego po zakupie.
    /// </summary>
    /// <remarks>
    /// Odlicza czas od <see cref="FeedbackDuration"/> do zera.
    /// Po upływie czasu czyści tekst komunikatu zwrotnego.
    /// Używa <see cref="Time.unscaledDeltaTime"/> dla niezależności od skali czasu.
    /// </remarks>
    private void UpdateFeedback()
    {
        if (feedbackTimer > 0f)
        {
            feedbackTimer -= Time.unscaledDeltaTime;
            if (feedbackTimer <= 0f && purchaseFeedbackText != null)
            {
                purchaseFeedbackText.text = string.Empty;
            }
        }
    }

    /// <summary>
    /// Odświeża zawartość panelu sklepu (saldo, stany wierszy ulepszeń).
    /// </summary>
    /// <remarks>
    /// Aktualizuje wyświetlane saldo gracza oraz stan każdego wiersza ulepszenia
    /// (nazwa, opis, poziom, koszt, efekt, stan przycisku).
    /// Wymaga dostępności <see cref="ShopManager.Instance"/>.
    /// </remarks>
    private void RefreshContent()
    {
        if (ShopManager.Instance == null)
        {
            return;
        }

        float balance = EconomyManager.Instance != null ? EconomyManager.Instance.CurrentBalance : 0f;
        if (balanceText != null)
        {
            balanceText.text = balance.ToString("F0") + " zl";
        }

        List<UpgradeDefinition> definitions = ShopManager.Instance.GetAllDefinitions();
        for (int i = 0; i < upgradeRows.Count && i < definitions.Count; i++)
        {
            RefreshRow(upgradeRows[i], definitions[i]);
        }
    }

    /// <summary>
    /// Odświeża pojedynczy wiersz ulepszenia w panelu sklepu.
    /// </summary>
    /// <param name="row">Wiersz UI do zaktualizowania.</param>
    /// <param name="definition">Definicja ulepszenia odpowiadająca wierszowi.</param>
    /// <remarks>
    /// Aktualizuje:
    /// - Nazwę i opis ulepszenia
    /// - Wskaźnik poziomu (np. "Poziom 2/4")
    /// - Koszt następnego poziomu lub informację o maksymalnym poziomie
    /// - Opis efektu następnego poziomu
    /// - Stan i kolor przycisku zakupu (aktywny/wyłączony/MAX)
    /// </remarks>
    private void RefreshRow(ShopUpgradeRow row, UpgradeDefinition definition)
    {
        int currentLevel = ShopManager.Instance.GetUpgradeLevel(definition.type);
        bool isMaxed = currentLevel >= definition.maxLevel;
        bool canAfford = ShopManager.Instance.CanAffordUpgrade(definition.type);

        row.nameText.text = definition.displayName;
        row.descriptionText.text = definition.description;

        string levelDisplay = BuildLevelIndicator(currentLevel, definition.maxLevel);
        row.levelText.text = levelDisplay;

        if (isMaxed)
        {
            row.costText.text = string.Empty;
            row.effectText.text = "Maksymalny poziom";
            row.effectText.color = ButtonMaxedColor;
            row.buttonText.text = "MAX";
            row.buttonImage.color = ButtonMaxedColor;
            row.button.interactable = false;
        }
        else
        {
            float cost = ShopManager.Instance.GetNextUpgradeCost(definition.type);
            row.costText.text = cost.ToString("F0") + " zl";
            row.costText.color = canAfford ? CostColor : FeedbackFailColor;
            row.effectText.text = definition.GetEffectDescription(currentLevel);
            row.effectText.color = definition.accentColor;
            row.buttonText.text = "KUP";
            row.buttonImage.color = canAfford ? ButtonNormalColor : ButtonDisabledColor;
            row.button.interactable = canAfford;
        }
    }

    /// <summary>
    /// Buduje tekstowy wskaźnik poziomu ulepszenia w formacie "Poziom X/Y".
    /// </summary>
    /// <param name="currentLevel">Aktualny poziom ulepszenia.</param>
    /// <param name="maxLevel">Maksymalny możliwy poziom ulepszenia.</param>
    /// <returns>Sformatowany ciąg tekstowy, np. "Poziom 2/4".</returns>
    private string BuildLevelIndicator(int currentLevel, int maxLevel)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append("Poziom ");
        builder.Append(currentLevel);
        builder.Append("/");
        builder.Append(maxLevel);

        return builder.ToString();
    }

    /// <summary>
    /// Obsługuje kliknięcie przycisku zakupu ulepszenia.
    /// </summary>
    /// <param name="type">Typ ulepszenia, które gracz chce zakupić.</param>
    /// <remarks>
    /// Metoda weryfikuje warunki zakupu (maksymalny poziom, dostępne środki)
    /// i wyświetla odpowiedni komunikat zwrotny.
    /// W trybie sieciowym rozróżnia zachowanie serwera i klienta:
    /// - Serwer: bezpośrednio wykonuje zakup przez <see cref="ShopManager"/>
    /// - Klient: wysyła żądanie zakupu do serwera przez <c>NetworkPlayer.PurchaseUpgradeServerRpc</c>
    /// W trybie offline zakup jest wykonywany bezpośrednio.
    /// </remarks>
    private void OnUpgradeButtonClicked(UpgradeType type)
    {
        if (ShopManager.Instance == null)
        {
            return;
        }

        if (ShopManager.Instance.IsMaxLevel(type))
        {
            ShowFeedback("To ulepszenie jest juz na maksymalnym poziomie.", false);
            return;
        }

        if (!ShopManager.Instance.CanAffordUpgrade(type))
        {
            float cost = ShopManager.Instance.GetNextUpgradeCost(type);
            ShowFeedback("Za malo pieniedzy. Potrzebujesz " + cost.ToString("F0") + " zl.", false);
            return;
        }

        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening)
        {
            if (Unity.Netcode.NetworkManager.Singleton.IsServer)
            {
                bool success = ShopManager.Instance.TryPurchaseUpgrade(type);
                HandlePurchaseResult(success, type);
            }
            else
            {
                NetworkPlayer localPlayer = NetworkPlayer.FindLocalPlayer();
                if (localPlayer != null)
                {
                    localPlayer.PurchaseUpgradeServerRpc((int)type);
                }
            }
        }
        else
        {
            bool success = ShopManager.Instance.TryPurchaseUpgrade(type);
            HandlePurchaseResult(success, type);
        }
    }

    /// <summary>
    /// Obsługuje wynik próby zakupu ulepszenia, wyświetlając odpowiedni komunikat i efekty.
    /// </summary>
    /// <param name="success">Czy zakup się powiódł.</param>
    /// <param name="type">Typ zakupionego (lub nieudanego) ulepszenia.</param>
    /// <remarks>
    /// W przypadku sukcesu:
    /// - Wyświetla komunikat z nazwą ulepszenia i nowym poziomem
    /// - Odtwarza efekty wizualne i dźwiękowe zakupu
    /// 
    /// W przypadku porażki:
    /// - Wyświetla komunikat o niepowodzeniu
    /// - Odtwarza dźwięk błędu
    /// 
    /// Metoda jest publiczna, aby mogła być wywoływana z systemu sieciowego
    /// po otrzymaniu odpowiedzi z serwera.
    /// </remarks>
    public void HandlePurchaseResult(bool success, UpgradeType type)
    {
        if (success)
        {
            UpgradeDefinition definition = ShopManager.Instance.GetDefinition(type);
            string upgradeName = definition != null ? definition.displayName : type.ToString();
            int newLevel = ShopManager.Instance.GetUpgradeLevel(type);
            ShowFeedback("Zakupiono " + upgradeName + " (poz. " + newLevel + ")!", true);
            PlayPurchaseFeedback(definition);
        }
        else
        {
            ShowFeedback("Nie udalo sie zakupic ulepszenia.", false);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayFailSound();
        }
    }

    /// <summary>
    /// Odtwarza efekty wizualne i dźwiękowe towarzyszące udanemu zakupowi ulepszenia.
    /// </summary>
    /// <param name="definition">
    /// Definicja zakupionego ulepszenia, używana do pobrania koloru akcentu dla efektów VFX.
    /// Może być <c>null</c>, wtedy używany jest domyślny kolor tytułu.
    /// </param>
    /// <remarks>
    /// Uruchamia następujące efekty (jeśli odpowiednie menedżery są dostępne):
    /// - Dźwięk zakupu ulepszenia (<see cref="AudioManager"/>)
    /// - Efekt cząsteczkowy w kolorze akcentu ulepszenia (<see cref="VFXManager"/>)
    /// - Pulsacja efektu bloom na ekranie (<see cref="PostProcessSetup"/>)
    /// - Lekkie wstrząśnięcie kamery (<see cref="CameraEffects"/>)
    /// </remarks>
    private void PlayPurchaseFeedback(UpgradeDefinition definition)
    {
        Color accent = definition != null ? definition.accentColor : TitleColor;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUpgradeSound();
        }

        if (VFXManager.Instance != null)
        {
            Vector3 effectPosition = VFXManager.Instance.GetCameraFacingPosition(2.0f, -0.18f);
            VFXManager.Instance.PlayUpgradeEffect(effectPosition, accent);
        }

        if (PostProcessSetup.Instance != null)
        {
            PostProcessSetup.Instance.PulseBloom(0.28f, 0.32f);
        }

        if (CameraEffects.Instance != null)
        {
            CameraEffects.Instance.ShakeCamera(0.025f, 0.12f);
        }
    }

    /// <summary>
    /// Wyświetla komunikat zwrotny w panelu sklepu.
    /// </summary>
    /// <param name="message">Treść komunikatu do wyświetlenia.</param>
    /// <param name="success">
    /// Czy komunikat dotyczy sukcesu (<c>true</c> = kolor zielony)
    /// czy porażki (<c>false</c> = kolor czerwony).
    /// </param>
    /// <remarks>
    /// Ustawia tekst i kolor komunikatu oraz resetuje timer wyświetlania
    /// na wartość <see cref="FeedbackDuration"/>.
    /// </remarks>
    private void ShowFeedback(string message, bool success)
    {
        if (purchaseFeedbackText != null)
        {
            purchaseFeedbackText.text = message;
            purchaseFeedbackText.color = success ? FeedbackSuccessColor : FeedbackFailColor;
        }

        feedbackTimer = FeedbackDuration;
    }

    /// <summary>
    /// Tworzy kompletną hierarchię obiektów UI panelu sklepu.
    /// </summary>
    /// <remarks>
    /// Buduje całą strukturę interfejsu programowo:
    /// 1. Ładuje czcionkę (LegacyRuntime z fallbackiem na Arial)
    /// 2. Tworzy Canvas z CanvasScaler i GraphicRaycaster
    /// 3. Tworzy główny panel z CanvasGroup (do animacji)
    /// 4. Tworzy nakładkę tła (overlay)
    /// 5. Tworzy panel zawartości z obramowaniem
    /// 6. Tworzy nagłówek z tytułem i saldem
    /// 7. Tworzy wiersze ulepszeń
    /// 8. Tworzy stopkę z komunikatem i podpowiedzią
    /// 
    /// Panel jest domyślnie nieaktywny po utworzeniu.
    /// </remarks>
    private void CreateShopCanvas()
    {
        cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (cachedFont == null)
        {
            cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        GameObject canvasObject = new GameObject("ShopCanvas");
        canvasObject.transform.SetParent(transform, false);

        shopCanvas = canvasObject.AddComponent<Canvas>();
        shopCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        shopCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        shopPanel = new GameObject("ShopPanel");
        shopPanel.transform.SetParent(canvasObject.transform, false);

        panelCanvasGroup = shopPanel.AddComponent<CanvasGroup>();
        panelCanvasGroup.alpha = 0f;

        RectTransform panelRect = shopPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        CreateOverlay(shopPanel.transform);
        Transform contentPanel = CreateContentPanel(shopPanel.transform);
        CreateHeader(contentPanel);
        CreateUpgradeRows(contentPanel);
        CreateFooter(contentPanel);

        shopPanel.SetActive(false);
    }

    /// <summary>
    /// Tworzy półprzezroczystą nakładkę zasłaniającą ekran za panelem sklepu.
    /// </summary>
    /// <param name="parent">Obiekt nadrzędny, do którego nakładka zostanie dołączona.</param>
    /// <remarks>
    /// Nakładka rozciąga się na cały ekran i przechwytuje kliknięcia (raycastTarget = true),
    /// zapobiegając interakcji z elementami UI znajdującymi się pod panelem sklepu.
    /// </remarks>
    private void CreateOverlay(Transform parent)
    {
        GameObject overlay = new GameObject("Overlay");
        overlay.transform.SetParent(parent, false);

        Image image = overlay.AddComponent<Image>();
        image.color = BackgroundOverlay;
        image.raycastTarget = true;

        RectTransform rect = overlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Tworzy główny panel zawartości sklepu z obramowaniem.
    /// </summary>
    /// <param name="parent">Obiekt nadrzędny, do którego panel zostanie dołączony.</param>
    /// <returns>Transform panelu zawartości, do którego dodawane będą elementy sklepu.</returns>
    /// <remarks>
    /// Tworzy dwuwarstwową strukturę: zewnętrzny panel obramowania (820x704 px)
    /// i wewnętrzny panel zawartości z marginesem 3 px od krawędzi obramowania.
    /// Panel jest wycentrowany na ekranie.
    /// </remarks>
    private Transform CreateContentPanel(Transform parent)
    {
        GameObject borderPanel = new GameObject("PanelBorder");
        borderPanel.transform.SetParent(parent, false);

        Image borderImage = borderPanel.AddComponent<Image>();
        borderImage.color = PanelBorderColor;

        RectTransform borderRect = borderPanel.GetComponent<RectTransform>();
        borderRect.anchorMin = new Vector2(0.5f, 0.5f);
        borderRect.anchorMax = new Vector2(0.5f, 0.5f);
        borderRect.pivot = new Vector2(0.5f, 0.5f);
        borderRect.sizeDelta = new Vector2(820f, 704f);
        borderRect.anchoredPosition = Vector2.zero;

        GameObject contentObject = new GameObject("ContentPanel");
        contentObject.transform.SetParent(borderPanel.transform, false);

        Image contentImage = contentObject.AddComponent<Image>();
        contentImage.color = PanelColor;

        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(3f, 3f);
        contentRect.offsetMax = new Vector2(-3f, -3f);

        return contentObject.transform;
    }

    /// <summary>
    /// Tworzy sekcję nagłówka panelu sklepu z tytułem i saldem.
    /// </summary>
    /// <param name="parent">Obiekt nadrzędny (panel zawartości).</param>
    /// <remarks>
    /// Nagłówek zawiera:
    /// - Pasek tła o wysokości 90 px przyciągnięty do górnej krawędzi
    /// - Tytuł "ULEPSZENIA" wyrównany do lewej (55% szerokości)
    /// - Saldo gracza wyrównane do prawej (45% szerokości)
    /// - Linię separatora pod nagłówkiem
    /// </remarks>
    private void CreateHeader(Transform parent)
    {
        GameObject headerBand = new GameObject("HeaderBand");
        headerBand.transform.SetParent(parent, false);

        Image headerImage = headerBand.AddComponent<Image>();
        headerImage.color = HeaderColor;

        RectTransform headerRect = headerBand.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.sizeDelta = new Vector2(0f, 90f);
        headerRect.anchoredPosition = Vector2.zero;

        titleText = CreateTextElement(
            headerBand.transform,
            "Title",
            "ULEPSZENIA",
            30,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            TitleColor);

        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0f);
        titleRect.anchorMax = new Vector2(0.55f, 1f);
        titleRect.offsetMin = new Vector2(28f, 0f);
        titleRect.offsetMax = new Vector2(0f, 0f);

        balanceText = CreateTextElement(
            headerBand.transform,
            "Balance",
            "0 zl",
            24,
            FontStyle.Bold,
            TextAnchor.MiddleRight,
            BalanceValueColor);

        RectTransform balanceRect = balanceText.GetComponent<RectTransform>();
        balanceRect.anchorMin = new Vector2(0.55f, 0f);
        balanceRect.anchorMax = new Vector2(1f, 1f);
        balanceRect.offsetMin = new Vector2(0f, 0f);
        balanceRect.offsetMax = new Vector2(-28f, 0f);

        CreateDivider(parent, new Vector2(0f, -90f));
    }

    /// <summary>
    /// Tworzy wiersze ulepszeń w panelu sklepu na podstawie definicji z <see cref="ShopManager"/>.
    /// </summary>
    /// <param name="parent">Obiekt nadrzędny (panel zawartości).</param>
    /// <remarks>
    /// Pobiera listę definicji ulepszeń i dla każdej tworzy osobny wiersz UI.
    /// Wiersze są rozmieszczone pionowo z odstępem 10 px, zaczynając od pozycji Y = -102 px
    /// (pod nagłówkiem). Każdy wiersz ma wysokość 96 px.
    /// </remarks>
    private void CreateUpgradeRows(Transform parent)
    {
        if (ShopManager.Instance == null)
        {
            return;
        }

        List<UpgradeDefinition> definitions = ShopManager.Instance.GetAllDefinitions();
        float startY = -102f;
        float rowHeight = 96f;
        float rowSpacing = 10f;

        for (int i = 0; i < definitions.Count; i++)
        {
            float yPos = startY - i * (rowHeight + rowSpacing);
            ShopUpgradeRow row = CreateSingleUpgradeRow(parent, definitions[i], yPos, rowHeight);
            upgradeRows.Add(row);
        }
    }

    /// <summary>
    /// Tworzy pojedynczy wiersz ulepszenia w panelu sklepu.
    /// </summary>
    /// <param name="parent">Obiekt nadrzędny (panel zawartości).</param>
    /// <param name="definition">Definicja ulepszenia do wyświetlenia w wierszu.</param>
    /// <param name="yPos">Pozycja Y wiersza (ujemna, od góry panelu).</param>
    /// <param name="height">Wysokość wiersza w pikselach.</param>
    /// <returns>Obiekt <see cref="ShopUpgradeRow"/> z referencjami do wszystkich elementów UI wiersza.</returns>
    /// <remarks>
    /// Każdy wiersz zawiera:
    /// - Tło wiersza
    /// - Nazwę ulepszenia (u góry po lewej)
    /// - Opis ulepszenia (pod nazwą)
    /// - Opis efektu następnego poziomu (pod opisem, w kolorze akcentu)
    /// - Wskaźnik poziomu (po prawej, u góry)
    /// - Koszt następnego poziomu (po prawej, na dole)
    /// - Przycisk zakupu "KUP" z obsługą kliknięcia
    /// </remarks>
    private ShopUpgradeRow CreateSingleUpgradeRow(Transform parent, UpgradeDefinition definition, float yPos, float height)
    {
        ShopUpgradeRow row = new ShopUpgradeRow();

        GameObject rowObject = new GameObject("Row_" + definition.type);
        rowObject.transform.SetParent(parent, false);

        Image rowImage = rowObject.AddComponent<Image>();
        rowImage.color = RowBackgroundColor;

        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.sizeDelta = new Vector2(-36f, height);
        rowRect.anchoredPosition = new Vector2(0f, yPos);

        row.rowImage = rowImage;

        row.nameText = CreateTextElement(
            rowObject.transform,
            "Name",
            definition.displayName,
            18,
            FontStyle.Bold,
            TextAnchor.UpperLeft,
            NameColor);

        RectTransform nameRect = row.nameText.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(0f, 1f);
        nameRect.pivot = new Vector2(0f, 1f);
        nameRect.sizeDelta = new Vector2(360f, 26f);
        nameRect.anchoredPosition = new Vector2(28f, -14f);

        row.descriptionText = CreateTextElement(
            rowObject.transform,
            "Description",
            definition.description,
            13,
            FontStyle.Normal,
            TextAnchor.UpperLeft,
            DescriptionColor);

        RectTransform descRect = row.descriptionText.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0f, 1f);
        descRect.anchorMax = new Vector2(0f, 1f);
        descRect.pivot = new Vector2(0f, 1f);
        descRect.sizeDelta = new Vector2(390f, 20f);
        descRect.anchoredPosition = new Vector2(28f, -40f);

        row.effectText = CreateTextElement(
            rowObject.transform,
            "Effect",
            "",
            14,
            FontStyle.Bold,
            TextAnchor.UpperLeft,
            definition.accentColor);

        RectTransform effectRect = row.effectText.GetComponent<RectTransform>();
        effectRect.anchorMin = new Vector2(0f, 1f);
        effectRect.anchorMax = new Vector2(0f, 1f);
        effectRect.pivot = new Vector2(0f, 1f);
        effectRect.sizeDelta = new Vector2(390f, 22f);
        effectRect.anchoredPosition = new Vector2(28f, -64f);

        row.levelText = CreateTextElement(
            rowObject.transform,
            "Level",
            "",
            14,
            FontStyle.Normal,
            TextAnchor.MiddleRight,
            LevelActiveColor);

        RectTransform levelRect = row.levelText.GetComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(1f, 0.5f);
        levelRect.anchorMax = new Vector2(1f, 0.5f);
        levelRect.pivot = new Vector2(1f, 0.5f);
        levelRect.sizeDelta = new Vector2(150f, 24f);
        levelRect.anchoredPosition = new Vector2(-160f, 18f);

        row.costText = CreateTextElement(
            rowObject.transform,
            "Cost",
            "",
            16,
            FontStyle.Bold,
            TextAnchor.MiddleRight,
            CostColor);

        RectTransform costRect = row.costText.GetComponent<RectTransform>();
        costRect.anchorMin = new Vector2(1f, 0.5f);
        costRect.anchorMax = new Vector2(1f, 0.5f);
        costRect.pivot = new Vector2(1f, 0.5f);
        costRect.sizeDelta = new Vector2(150f, 24f);
        costRect.anchoredPosition = new Vector2(-160f, -14f);

        GameObject buttonObject = new GameObject("BuyButton");
        buttonObject.transform.SetParent(rowObject.transform, false);

        row.buttonImage = buttonObject.AddComponent<Image>();
        row.buttonImage.color = ButtonNormalColor;

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0.5f);
        buttonRect.anchorMax = new Vector2(1f, 0.5f);
        buttonRect.pivot = new Vector2(1f, 0.5f);
        buttonRect.sizeDelta = new Vector2(112f, 44f);
        buttonRect.anchoredPosition = new Vector2(-20f, 0f);

        row.buttonText = CreateTextElement(
            buttonObject.transform,
            "ButtonLabel",
            "KUP",
            16,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            ButtonTextColor);

        RectTransform buttonTextRect = row.buttonText.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = row.buttonImage;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.disabledColor = Color.white;
        button.colors = colors;

        UpgradeType capturedType = definition.type;
        button.onClick.AddListener(() => OnUpgradeButtonClicked(capturedType));

        row.button = button;

        return row;
    }

    /// <summary>
    /// Tworzy sekcję stopki panelu sklepu z komunikatem zwrotnym i podpowiedzią.
    /// </summary>
    /// <param name="parent">Obiekt nadrzędny (panel zawartości).</param>
    /// <remarks>
    /// Stopka zawiera:
    /// - Tekst komunikatu zwrotnego o wyniku zakupu (pozycja Y = -690 px)
    /// - Tekst podpowiedzi "TAB Zamknij" przyciągnięty do dolnej krawędzi panelu
    /// </remarks>
    private void CreateFooter(Transform parent)
    {
        float footerY = -690f;

        purchaseFeedbackText = CreateTextElement(
            parent,
            "PurchaseFeedback",
            "",
            17,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            FeedbackSuccessColor);

        RectTransform feedbackRect = purchaseFeedbackText.GetComponent<RectTransform>();
        feedbackRect.anchorMin = new Vector2(0f, 1f);
        feedbackRect.anchorMax = new Vector2(1f, 1f);
        feedbackRect.pivot = new Vector2(0.5f, 1f);
        feedbackRect.sizeDelta = new Vector2(-36f, 28f);
        feedbackRect.anchoredPosition = new Vector2(0f, footerY);

        hintText = CreateTextElement(
            parent,
            "HintText",
            "TAB  Zamknij",
            13,
            FontStyle.Normal,
            TextAnchor.MiddleCenter,
            HintColor);

        RectTransform hintRect = hintText.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.sizeDelta = new Vector2(0f, 34f);
        hintRect.anchoredPosition = new Vector2(0f, 8f);
    }

    /// <summary>
    /// Tworzy poziomą linię separatora w panelu sklepu.
    /// </summary>
    /// <param name="parent">Obiekt nadrzędny, do którego separator zostanie dołączony.</param>
    /// <param name="position">Pozycja zakotwiczenia separatora względem górnej krawędzi rodzica.</param>
    /// <remarks>
    /// Separator ma wysokość 2 px, szerokość pomniejszoną o 36 px (marginesy boczne)
    /// i jest przyciągnięty do górnej krawędzi rodzica.
    /// </remarks>
    private void CreateDivider(Transform parent, Vector2 position)
    {
        GameObject divider = new GameObject("Divider");
        divider.transform.SetParent(parent, false);

        Image image = divider.AddComponent<Image>();
        image.color = DividerColor;

        RectTransform rect = divider.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(-36f, 2f);
        rect.anchoredPosition = position;
    }

    /// <summary>
    /// Tworzy element tekstowy Unity UI z podanymi parametrami.
    /// </summary>
    /// <param name="parent">Obiekt nadrzędny, do którego element tekstowy zostanie dołączony.</param>
    /// <param name="objectName">Nazwa obiektu GameObject w hierarchii.</param>
    /// <param name="defaultText">Domyślna treść tekstu.</param>
    /// <param name="fontSize">Rozmiar czcionki w pikselach.</param>
    /// <param name="style">Styl czcionki (Normal, Bold, Italic, BoldAndItalic).</param>
    /// <param name="alignment">Wyrównanie tekstu (np. MiddleCenter, UpperLeft).</param>
    /// <param name="color">Kolor tekstu.</param>
    /// <returns>Komponent <see cref="Text"/> utworzonego elementu tekstowego.</returns>
    /// <remarks>
    /// Używa buforowanej czcionki <see cref="cachedFont"/>.
    /// Tekst jest konfigurowany z zawijaniem horyzontalnym i przepełnieniem wertykalnym.
    /// Raycast jest wyłączony (raycastTarget = false), aby tekst nie blokował kliknięć.
    /// </remarks>
    private Text CreateTextElement(
        Transform parent,
        string objectName,
        string defaultText,
        int fontSize,
        FontStyle style,
        TextAnchor alignment,
        Color color)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.font = cachedFont;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.text = defaultText;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        return text;
    }

    /// <summary>
    /// Wewnętrzna klasa reprezentująca pojedynczy wiersz ulepszenia w interfejsie sklepu.
    /// </summary>
    /// <remarks>
    /// Przechowuje referencje do wszystkich elementów UI składających się na wiersz ulepszenia,
    /// umożliwiając ich łatwą aktualizację podczas odświeżania zawartości panelu.
    /// </remarks>
    private class ShopUpgradeRow
    {
        /// <summary>
        /// Komponent obrazu tła wiersza.
        /// </summary>
        public Image rowImage;

        /// <summary>
        /// Komponent obrazu przycisku zakupu (używany jako tło przycisku i element docelowy kolorów).
        /// </summary>
        public Image buttonImage;

        /// <summary>
        /// Tekst wyświetlający nazwę ulepszenia.
        /// </summary>
        public Text nameText;

        /// <summary>
        /// Tekst wyświetlający opis działania ulepszenia.
        /// </summary>
        public Text descriptionText;

        /// <summary>
        /// Tekst wyświetlający opis efektu następnego poziomu ulepszenia.
        /// </summary>
        public Text effectText;

        /// <summary>
        /// Tekst wyświetlający aktualny poziom ulepszenia (np. "Poziom 2/4").
        /// </summary>
        public Text levelText;

        /// <summary>
        /// Tekst wyświetlający koszt następnego poziomu ulepszenia.
        /// </summary>
        public Text costText;

        /// <summary>
        /// Tekst wyświetlany na przycisku zakupu ("KUP" lub "MAX").
        /// </summary>
        public Text buttonText;

        /// <summary>
        /// Komponent przycisku zakupu umożliwiający interakcję gracza.
        /// </summary>
        public Button button;
    }
}
