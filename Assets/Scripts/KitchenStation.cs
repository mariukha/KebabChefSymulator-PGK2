/// \file KitchenStation.cs
/// \brief Plik zawierający implementację klasy KitchenStation, która zarządza
/// logiką stanowisk kuchennych w symulatorze kebaba.
/// \details Klasa obsługuje różne typy stanowisk kuchennych: źródła składników,
/// deski do krojenia, grille, stanowiska montażu kebaba oraz punkty wydania.
/// Każde stanowisko posiada własną logikę przetwarzania, wizualizację stanu
/// oraz integrację z systemami sieciowymi, efektami dźwiękowymi i wizualnymi.

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Klasa reprezentująca stanowisko kuchenne w symulatorze kebaba.
/// Dziedziczy po klasie <see cref="Interactable"/> i obsługuje interakcje gracza
/// z różnymi typami stanowisk kuchennych.
/// </summary>
/// <remarks>
/// Stanowisko kuchenne może pełnić jedną z pięciu ról:
/// <list type="bullet">
/// <item><description><c>IngredientSource</c> — źródło surowych składników (w tym taca z mięsem)</description></item>
/// <item><description><c>CuttingBoard</c> — deska do krojenia warzyw</description></item>
/// <item><description><c>Grill</c> — grill do pieczenia mięsa lub doner kebab</description></item>
/// <item><description><c>Assembly</c> — stanowisko montażu gotowego kebaba</description></item>
/// <item><description><c>Delivery</c> — punkt wydania gotowego dania klientowi</description></item>
/// </list>
/// Klasa zarządza stanem przetwarzania składników, wizualizacją dynamicznych obiektów
/// na stanowisku oraz synchronizacją stanu przez sieć.
/// </remarks>
public class KitchenStation : Interactable
{
    /// <summary>
    /// Nazwa wyświetlana stanowiska kuchennego, używana w komunikatach dla gracza.
    /// </summary>
    [SerializeField] private string stationName = "Stacja";

    /// <summary>
    /// Typ stanowiska kuchennego określający jego funkcję i zachowanie.
    /// </summary>
    /// <seealso cref="KitchenStationType"/>
    [SerializeField] private KitchenStationType stationType = KitchenStationType.IngredientSource;

    /// <summary>
    /// Dane składnika źródłowego dostarczanego przez to stanowisko.
    /// Używane tylko dla stanowisk typu <see cref="KitchenStationType.IngredientSource"/>.
    /// </summary>
    [SerializeField] private IngredientData sourceIngredient;

    /// <summary>
    /// Bazowy czas trwania przetwarzania składnika na stanowisku (w sekundach).
    /// Może być modyfikowany przez mnożnik prędkości ze sklepu.
    /// </summary>
    [SerializeField] private float processingDuration = 2.5f;

    /// <summary>
    /// Renderer wizualny stanowiska, którego kolor zmienia się w zależności od stanu.
    /// </summary>
    [SerializeField] private Renderer visualRenderer;

    /// <summary>
    /// Kolor stanowiska w stanie bezczynności (brak przetwarzania, brak przedmiotu).
    /// </summary>
    [SerializeField] private Color idleColor = new Color(0.35f, 0.35f, 0.35f);

    /// <summary>
    /// Kolor stanowiska podczas aktywnego przetwarzania składnika.
    /// </summary>
    [SerializeField] private Color busyColor = new Color(0.95f, 0.65f, 0.2f);

    /// <summary>
    /// Kolor stanowiska gdy składnik jest gotowy do odebrania.
    /// </summary>
    [SerializeField] private Color readyColor = new Color(0.35f, 0.8f, 0.35f);

    /// <summary>
    /// Przedmiot kuchenny aktualnie znajdujący się na stanowisku (przetwarzany lub gotowy).
    /// </summary>
    [SerializeField] private KitchenItem stationItem;

    /// <summary>
    /// Flaga określająca, czy na stanowisku montażu znajduje się ławasz.
    /// </summary>
    [SerializeField] private bool hasLavash;

    /// <summary>
    /// Lista przygotowanych składników dodanych do montażu kebaba na stanowisku Assembly.
    /// </summary>
    [SerializeField] private List<PreparedIngredientData> assemblyIngredients = new List<PreparedIngredientData>();

    /// <summary>
    /// Liczba przygotowanych porcji mięsa dostępnych na tacy z mięsem.
    /// </summary>
    [SerializeField] private int preparedMeatServings;

    /// <summary>
    /// Domyślna wielkość partii mięsa wytwarzanej przy jednorazowym ścięciu z donera.
    /// Może być nadpisana przez wartość z <see cref="ShopManager"/>.
    /// </summary>
    [SerializeField] private int preparedMeatBatchSize = 3;

    /// <summary>
    /// Flaga wskazująca, czy stanowisko aktualnie przetwarza składnik.
    /// </summary>
    private bool isProcessing;

    /// <summary>
    /// Czas zakończenia bieżącego procesu przetwarzania (w <c>Time.time</c>).
    /// </summary>
    private float processEndTime;

    /// <summary>
    /// Powiązane stanowisko tacy z mięsem, na którą trafia mięso ścięte z donera.
    /// Używane przez stanowiska typu grill-doner.
    /// </summary>
    private KitchenStation linkedMeatTray;

    /// <summary>
    /// Transform wizualizacji mięsa na tacy, wyszukiwany jako obiekt potomny "MeatVisual".
    /// </summary>
    private Transform meatVisual;

    /// <summary>
    /// Dynamicznie tworzony obiekt wizualny reprezentujący przedmiot na stanowisku.
    /// </summary>
    private GameObject dynamicStationItemVisual;

    /// <summary>
    /// Dynamicznie tworzony obiekt wizualny reprezentujący ławasz na stanowisku montażu.
    /// </summary>
    private GameObject dynamicLavashVisual;

    /// <summary>
    /// Lista dynamicznie tworzonych obiektów wizualnych reprezentujących składniki montażu kebaba.
    /// </summary>
    private List<GameObject> dynamicAssemblyVisuals = new List<GameObject>();

    /// <summary>
    /// Bieżąca faza animacji pulsowania stanowiska, przyrastająca w czasie.
    /// </summary>
    private float pulsePhase;

    /// <summary>
    /// Bazowa skala stanowiska zapamiętana przed rozpoczęciem efektu pulsowania.
    /// </summary>
    private Vector3 baseScale;

    /// <summary>
    /// Hash ostatniego stanu wizualnego, używany do optymalizacji odświeżania wizualizacji.
    /// Jeśli hash się nie zmienił, wizualizacja nie jest przerysowywana.
    /// </summary>
    private int lastVisualHash;

    /// <summary>
    /// Informuje, czy stanowisko aktualnie przetwarza składnik.
    /// </summary>
    /// <value><c>true</c> jeśli trwa przetwarzanie; w przeciwnym razie <c>false</c>.</value>
    public bool IsProcessing => isProcessing;

    /// <summary>
    /// Czas (<c>Time.time</c>) zakończenia bieżącego przetwarzania.
    /// </summary>
    /// <value>Moment zakończenia procesu lub 0 jeśli nic nie jest przetwarzane.</value>
    public float ProcessEndTime => processEndTime;

    /// <summary>
    /// Liczba gotowych porcji mięsa na tacy.
    /// </summary>
    /// <value>Nieujemna liczba dostępnych porcji mięsa.</value>
    public int PreparedMeatServings => preparedMeatServings;

    /// <summary>
    /// Informuje, czy na stanowisku montażu leży ławasz.
    /// </summary>
    /// <value><c>true</c> jeśli ławasz jest obecny; w przeciwnym razie <c>false</c>.</value>
    public bool HasLavash => hasLavash;

    /// <summary>
    /// Liczba składników dodanych do montażu kebaba.
    /// </summary>
    /// <value>Liczba elementów na liście składników montażu.</value>
    public int AssemblyCount => assemblyIngredients.Count;

    /// <summary>
    /// Przedmiot kuchenny aktualnie znajdujący się na stanowisku.
    /// </summary>
    /// <value>Instancja <see cref="KitchenItem"/> lub <c>null</c> jeśli stanowisko jest puste.</value>
    public KitchenItem StationItem => stationItem;

    /// <summary>
    /// Typ tego stanowiska kuchennego.
    /// </summary>
    /// <value>Wartość enumeracji <see cref="KitchenStationType"/>.</value>
    public KitchenStationType StationType => stationType;

    /// <summary>
    /// Lista przygotowanych składników na stanowisku montażu.
    /// </summary>
    /// <value>Lista obiektów <see cref="PreparedIngredientData"/> zawierających rodzaj i stan składników.</value>
    public List<PreparedIngredientData> AssemblyIngredients => assemblyIngredients;

    /// <summary>
    /// Synchronizuje stan stanowiska na podstawie migawki sieciowej (snapshot).
    /// Nadpisuje bieżący stan lokalny wartościami otrzymanymi z serwera.
    /// </summary>
    /// <param name="snapshot">Migawka stanu stanowiska otrzymana z serwera, zawierająca
    /// informacje o przetwarzaniu, mięsie, ławaszu, przedmiocie i składnikach montażu.</param>
    /// <remarks>
    /// Metoda konwertuje pozostały czas przetwarzania z migawki na bezwzględny czas zakończenia
    /// względem <c>Time.time</c>. Składniki montażu są ograniczone do maksymalnie 8 slotów.
    /// Po synchronizacji odświeża wizualizację stanowiska.
    /// </remarks>
    public void SyncNetworkState(StationStateSnapshot snapshot)
    {
        isProcessing = snapshot.isProcessing;
        processEndTime = snapshot.isProcessing
            ? Time.time + snapshot.remainingProcessTime
            : 0f;
        preparedMeatServings = snapshot.preparedMeatServings;
        hasLavash = snapshot.hasLavash;
        stationItem = snapshot.stationItem.exists ? snapshot.stationItem.ToKitchenItem() : null;

        assemblyIngredients.Clear();
        int asmCount = Mathf.Min(snapshot.assemblyCount, 8);
        for (int i = 0; i < asmCount; i++)
        {
            snapshot.GetAssemblySlot(i, out IngredientKind kind, out IngredientProcessState state);
            assemblyIngredients.Add(new PreparedIngredientData(kind, state));
        }

        RefreshVisualState();
    }

    /// <summary>
    /// Zapisuje aktualny stan składników montażu do migawki sieciowej.
    /// </summary>
    /// <param name="snapshot">Referencja do migawki stanu stanowiska, do której zostaną
    /// zapisane dane o składnikach montażu (maksymalnie 8 slotów).</param>
    public void WriteAssemblyToSnapshot(ref StationStateSnapshot snapshot)
    {
        int count = Mathf.Min(assemblyIngredients.Count, 8);
        snapshot.assemblyCount = count;
        for (int i = 0; i < count; i++)
        {
            snapshot.SetAssemblySlot(i, assemblyIngredients[i].ingredientKind, assemblyIngredients[i].state);
        }
    }

    /// <summary>
    /// Konfiguruje stanowisko kuchenne z podanymi parametrami.
    /// Ustawia nazwę, typ, składnik źródłowy, czas przetwarzania i renderer wizualny.
    /// </summary>
    /// <param name="stationName">Nazwa wyświetlana stanowiska.</param>
    /// <param name="stationType">Typ stanowiska kuchennego.</param>
    /// <param name="sourceIngredient">Dane składnika źródłowego (może być <c>null</c> dla stanowisk nie będących źródłami).</param>
    /// <param name="processingDuration">Bazowy czas przetwarzania w sekundach.</param>
    /// <param name="visualRenderer">Renderer wizualny do zmiany koloru stanowiska.</param>
    public void Configure(
        string stationName,
        KitchenStationType stationType,
        IngredientData sourceIngredient,
        float processingDuration,
        Renderer visualRenderer)
    {
        this.stationName = stationName;
        this.stationType = stationType;
        this.sourceIngredient = sourceIngredient;
        this.processingDuration = processingDuration;
        this.visualRenderer = visualRenderer;
        PromptMessage = stationName;
        RefreshVisualState();
    }

    /// <summary>
    /// Ustawia powiązane stanowisko tacy z mięsem dla donera.
    /// Grill-doner po zakończeniu procesu przesyła gotowe mięso na powiązaną tacę.
    /// </summary>
    /// <param name="trayStation">Stanowisko tacy z mięsem do powiązania.</param>
    public void SetLinkedMeatTray(KitchenStation trayStation)
    {
        linkedMeatTray = trayStation;
        RefreshVisualState();
    }

    /// <summary>
    /// Odświeża kompletny stan wizualny stanowiska, w tym kolor i dynamiczne obiekty 3D.
    /// </summary>
    public void RefreshVisualState()
    {
        ApplyCurrentColor();
        UpdateDynamicVisuals();
    }

    /// <summary>
    /// Metoda Update wywoływana w każdej klatce przez Unity.
    /// Na kliencie sieciowym aktualizuje jedynie efekt pulsowania.
    /// Na serwerze sprawdza, czy przetwarzanie dobiegło końca i je finalizuje.
    /// </summary>
    private void Update()
    {
        if (Unity.Netcode.NetworkManager.Singleton != null &&
            Unity.Netcode.NetworkManager.Singleton.IsListening &&
            !Unity.Netcode.NetworkManager.Singleton.IsServer)
        {
            UpdatePulseEffect();
            return;
        }

        if (!isProcessing || Time.time < processEndTime)
        {
            UpdatePulseEffect();
            return;
        }

        FinishProcessing();
    }

    /// <summary>
    /// Aktualizuje efekt pulsowania skali stanowiska.
    /// Pulsowanie jest aktywne gdy stanowisko posiada gotowy przedmiot do odebrania
    /// lub tacę z przygotowanym mięsem.
    /// </summary>
    /// <remarks>
    /// Efekt pulsowania polega na cyklicznej zmianie skali obiektu w oparciu o funkcję sinus.
    /// Gdy pulsowanie nie jest potrzebne, skala jest płynnie przywracana do wartości bazowej.
    /// </remarks>
    private void UpdatePulseEffect()
    {
        bool shouldPulse = !isProcessing && (
            stationItem != null ||
            (IsMeatTrayStation() && preparedMeatServings > 0));

        if (!shouldPulse)
        {
            if (baseScale != Vector3.zero)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, baseScale, Time.deltaTime * 8f);
            }
            return;
        }

        if (baseScale == Vector3.zero)
        {
            baseScale = transform.localScale;
        }

        pulsePhase += Time.deltaTime * 2.5f;
        float pulse = 1f + Mathf.Sin(pulsePhase) * 0.018f;
        transform.localScale = baseScale * pulse;
    }

    /// <summary>
    /// Zwraca komunikat podpowiedzi wyświetlany graczowi w zależności od typu stanowiska
    /// i bieżącego stanu przetwarzania.
    /// </summary>
    /// <param name="player">Gracz, dla którego generowany jest komunikat podpowiedzi.</param>
    /// <returns>
    /// Tekst podpowiedzi opisujący dostępną akcję lub aktualny stan stanowiska.
    /// </returns>
    /// <remarks>
    /// Komunikaty różnią się w zależności od typu stanowiska:
    /// źródła składników, deski do krojenia, grilla/donera, montażu i wydania.
    /// Dla stanowisk przetwarzających wyświetlany jest pozostały czas.
    /// </remarks>
    public override string GetPrompt(PlayerInteraction player)
    {
        switch (stationType)
        {
            case KitchenStationType.IngredientSource:
                if (IsMeatTrayStation())
                {
                    return preparedMeatServings > 0
                        ? "E: Wez mieso"
                        : "Potnij mieso z donera";
                }

                return "E: Wez " + (sourceIngredient != null ? sourceIngredient.DisplayName : "skladnik");
            case KitchenStationType.CuttingBoard:
                if (isProcessing)
                {
                    return "Deska: krojenie " + Mathf.CeilToInt(processEndTime - Time.time) + " s";
                }

                if (stationItem != null)
                {
                    return "E: Odbierz z deski";
                }

                return "E: Poloz warzywo do krojenia";
            case KitchenStationType.Grill:
                if (IsDonerStation())
                {
                    if (isProcessing)
                    {
                        return "Doner: krojenie " + Mathf.CeilToInt(processEndTime - Time.time) + " s";
                    }

                    if (linkedMeatTray != null && linkedMeatTray.HasPreparedMeat())
                    {
                        return "Doner: taca z miesem jest pelna";
                    }

                    return "E: Potnij mieso z donera";
                }

                if (isProcessing)
                {
                    return "Grill: pieczenie " + Mathf.CeilToInt(processEndTime - Time.time) + " s";
                }

                if (stationItem != null)
                {
                    return "E: Odbierz mieso z grilla";
                }

                return "E: Poloz mieso na grillu";
            case KitchenStationType.Assembly:
                if (player != null && player.HasItemInHand)
                {
                    return "E: Dodaj skladnik do kebaba";
                }

                return CanCreateDish()
                    ? "E: Zawin gotowego kebaba"
                    : "E: Dodaj lawasz i przygotowane skladniki";
            case KitchenStationType.Delivery:
                return "E: Wydaj gotowego kebaba klientowi";
            default:
                return base.GetPrompt(player);
        }
    }

    /// <summary>
    /// Obsługuje interakcję gracza ze stanowiskiem kuchennym.
    /// Deleguje logikę do odpowiedniej metody w zależności od typu stanowiska.
    /// </summary>
    /// <param name="player">Gracz wykonujący interakcję ze stanowiskiem.</param>
    /// <remarks>
    /// Jeśli <paramref name="player"/> jest <c>null</c>, wywołuje implementację bazową.
    /// W przeciwnym razie rozdziela logikę między:
    /// <see cref="HandleIngredientSource"/>, <see cref="HandleProcessingStation"/>,
    /// <see cref="HandleAssembly"/> oraz <see cref="HandleDelivery"/>.
    /// </remarks>
    public override void Interact(PlayerInteraction player)
    {
        if (player == null)
        {
            base.Interact(player);
            return;
        }

        switch (stationType)
        {
            case KitchenStationType.IngredientSource:
                HandleIngredientSource(player);
                break;
            case KitchenStationType.CuttingBoard:
                HandleProcessingStation(player, IngredientProcessState.Chopped);
                break;
            case KitchenStationType.Grill:
                HandleProcessingStation(player, IngredientProcessState.Cooked);
                break;
            case KitchenStationType.Assembly:
                HandleAssembly(player);
                break;
            case KitchenStationType.Delivery:
                HandleDelivery(player);
                break;
        }
    }

    /// <summary>
    /// Obsługuje interakcję ze stanowiskiem źródła składników.
    /// Pozwala graczowi pobrać surowy składnik lub mięso z tacy.
    /// </summary>
    /// <param name="player">Gracz próbujący pobrać składnik ze stanowiska.</param>
    /// <remarks>
    /// Jeśli stanowisko jest tacą z mięsem, deleguje do <see cref="HandleMeatTraySource"/>.
    /// W przeciwnym razie tworzy nowy przedmiot kuchenny na podstawie <see cref="sourceIngredient"/>
    /// i przekazuje go graczowi. Gracz musi mieć puste ręce.
    /// </remarks>
    private void HandleIngredientSource(PlayerInteraction player)
    {
        if (IsMeatTrayStation())
        {
            HandleMeatTraySource(player);
            return;
        }

        if (player.HasItemInHand)
        {
            player.SetFeedback("Najpierw odloz to, co trzymasz.");
            return;
        }

        KitchenItem item = KitchenItem.FromIngredient(sourceIngredient);
        if (player.TryReceiveItem(item))
        {
            player.SetFeedback("Pobrano: " + item.BuildSummary());
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPickupSound();
            PlayPickupFeedback(item);
        }
    }

    /// <summary>
    /// Obsługuje interakcję ze stanowiskiem przetwarzającym (deska do krojenia lub grill).
    /// Umożliwia położenie składnika do przetworzenia, odebranie gotowego produktu
    /// lub wyświetlenie komunikatu o zajętości stanowiska.
    /// </summary>
    /// <param name="player">Gracz wchodzący w interakcję ze stanowiskiem.</param>
    /// <param name="outputState">Docelowy stan przetworzenia składnika
    /// (<see cref="IngredientProcessState.Chopped"/> dla deski,
    /// <see cref="IngredientProcessState.Cooked"/> dla grilla).</param>
    /// <remarks>
    /// Logika działania:
    /// <list type="number">
    /// <item><description>Jeśli stanowisko jest donerem — deleguje do <see cref="HandleDonerStation"/>.</description></item>
    /// <item><description>Jeśli trwa przetwarzanie — informuje gracza o zajętości.</description></item>
    /// <item><description>Jeśli gotowy produkt czeka — pozwala go odebrać.</description></item>
    /// <item><description>Jeśli gracz trzyma odpowiedni składnik — rozpoczyna przetwarzanie.</description></item>
    /// </list>
    /// Czas przetwarzania jest modyfikowany przez mnożnik prędkości ze sklepu.
    /// Uruchamiane są odpowiednie efekty wizualne i dźwiękowe.
    /// </remarks>
    private void HandleProcessingStation(PlayerInteraction player, IngredientProcessState outputState)
    {
        if (stationType == KitchenStationType.Grill && IsDonerStation())
        {
            HandleDonerStation(player);
            return;
        }

        if (isProcessing)
        {
            player.SetFeedback(stationName + " jest zajeta.");
            return;
        }

        if (stationItem != null)
        {
            KitchenItem completedItem = stationItem;
            if (!player.TryReceiveItem(completedItem))
            {
                player.SetFeedback("Masz juz cos w rece.");
                return;
            }

            player.SetFeedback("Odebrano: " + completedItem.BuildSummary());
            stationItem = null;
            ApplyCurrentColor();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPickupSound();
            PlayPickupFeedback(completedItem);
            return;
        }

        if (!player.HasItemInHand)
        {
            player.SetFeedback("Nie trzymasz zadnego skladnika.");
            return;
        }

        KitchenItem item = player.HeldItem;
        if (!CanBeProcessedHere(item, outputState))
        {
            player.SetFeedback("Ten skladnik nie pasuje do stacji " + stationName + ".");
            return;
        }

        stationItem = player.RemoveHeldItem();
        isProcessing = true;
        float adjustedDuration = processingDuration * GetShopSpeedMultiplier();
        processEndTime = Time.time + adjustedDuration;
        player.SetFeedback("Rozpoczeto przygotowanie: " + KitchenNaming.GetIngredientLabel(stationItem.ingredientKind));
        ApplyCurrentColor();

        if (stationType == KitchenStationType.CuttingBoard)
        {
            if (VFXManager.Instance != null)
            {
                Color chopColor = stationItem != null
                    ? KitchenItemVisualFactory.GetIngredientColor(stationItem.ingredientKind, stationItem.state)
                    : new Color(0.6f, 0.8f, 0.3f);
                VFXManager.Instance.PlayChopEffect(transform.position, chopColor);
            }
            if (AudioManager.Instance != null) AudioManager.Instance.PlayChopSound();
        }

        if (stationType == KitchenStationType.Grill)
        {
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.PlaySteamEffect(transform.position);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayDropSound();
                AudioManager.Instance.StartGrillAmbient();
            }
        }
    }

    /// <summary>
    /// Obsługuje pobieranie mięsa z tacy (stanowisko źródła mięsa powiązane z donerem).
    /// Gracz może pobrać jedną porcję gotowego mięsa, jeśli taca nie jest pusta.
    /// </summary>
    /// <param name="player">Gracz próbujący pobrać mięso z tacy.</param>
    /// <remarks>
    /// Wymaga, aby na tacy były dostępne porcje mięsa (<see cref="preparedMeatServings"/> > 0)
    /// oraz aby gracz miał puste ręce. Pobrane mięso ma stan <see cref="IngredientProcessState.Cooked"/>.
    /// Po pobraniu zmniejsza licznik porcji i odświeża wizualizację tacy i powiązanego donera.
    /// </remarks>
    private void HandleMeatTraySource(PlayerInteraction player)
    {
        if (preparedMeatServings <= 0)
        {
            player.SetFeedback("Najpierw potnij mieso z donera.");
            return;
        }

        if (player.HasItemInHand)
        {
            player.SetFeedback("Najpierw odloz to, co trzymasz.");
            return;
        }

        KitchenItem item = KitchenItem.FromIngredient(sourceIngredient);
        item.state = IngredientProcessState.Cooked;

        if (!player.TryReceiveItem(item))
        {
            player.SetFeedback("Masz juz cos w rece.");
            return;
        }

        preparedMeatServings = Mathf.Max(0, preparedMeatServings - 1);
        player.SetFeedback("Pobrano: " + item.BuildSummary());
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPickupSound();
        PlayPickupFeedback(item);
        ApplyCurrentColor();
        linkedMeatTray?.ApplyCurrentColor();
    }

    /// <summary>
    /// Obsługuje interakcję ze stanowiskiem doner kebab (grill powiązany z tacą na mięso).
    /// Rozpoczyna proces ścinania mięsa z szpikulca donera.
    /// </summary>
    /// <param name="player">Gracz inicjujący ścinanie mięsa z donera.</param>
    /// <remarks>
    /// Wymagania rozpoczęcia procesu:
    /// <list type="bullet">
    /// <item><description>Stanowisko nie jest zajęte przetwarzaniem.</description></item>
    /// <item><description>Gracz ma puste ręce.</description></item>
    /// <item><description>Istnieje powiązana taca na mięso.</description></item>
    /// <item><description>Powiązana taca nie jest już pełna.</description></item>
    /// </list>
    /// Po rozpoczęciu uruchamia efekt dymu donera oraz dźwięk krojenia.
    /// </remarks>
    private void HandleDonerStation(PlayerInteraction player)
    {
        if (isProcessing)
        {
            player.SetFeedback(stationName + " jest zajeta.");
            return;
        }

        if (player.HasItemInHand)
        {
            player.SetFeedback("Najpierw odloz to, co trzymasz.");
            return;
        }

        if (linkedMeatTray == null)
        {
            player.SetFeedback("Brak tacy na gotowe mieso.");
            return;
        }

        if (linkedMeatTray.HasPreparedMeat())
        {
            player.SetFeedback("Taca na mieso jest juz pelna.");
            return;
        }

        isProcessing = true;
        float adjustedDonerDuration = processingDuration * GetShopSpeedMultiplier();
        processEndTime = Time.time + adjustedDonerDuration;
        player.SetFeedback("Rozpoczeto scinanie miesa z donera.");
        ApplyCurrentColor();

        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.PlayDonerSmokeEffect(transform.position);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayChopSound();
            AudioManager.Instance.StartGrillAmbient();
        }
    }

    /// <summary>
    /// Obsługuje interakcję ze stanowiskiem montażu kebaba.
    /// Pozwala dodawać składniki, kłaść ławasz lub zawinąć gotowego kebaba.
    /// </summary>
    /// <param name="player">Gracz montujący kebaba na stanowisku.</param>
    /// <remarks>
    /// Jeśli gracz trzyma przedmiot:
    /// <list type="bullet">
    /// <item><description>Gotowy kebab — informuje, że jest już złożony.</description></item>
    /// <item><description>Ławasz — kładzie go na stanowisku (max 1 ławasz).</description></item>
    /// <item><description>Składnik — dodaje do listy montażu jeśli jest odpowiednio przygotowany.</description></item>
    /// </list>
    /// Jeśli gracz nie trzyma przedmiotu i warunki są spełnione — tworzy gotowego kebaba
    /// z ławasza i zebranych składników, a następnie resetuje stanowisko montażu.
    /// </remarks>
    private void HandleAssembly(PlayerInteraction player)
    {
        if (player.HasItemInHand)
        {
            KitchenItem item = player.HeldItem;

            if (item.isDish)
            {
                player.SetFeedback("Ten kebab jest juz zlozony.");
                return;
            }

            if (item.ingredientKind == IngredientKind.Lavash)
            {
                if (hasLavash)
                {
                    player.SetFeedback("Lawasz jest juz przygotowany na stanowisku.");
                    return;
                }

                hasLavash = true;
                player.RemoveHeldItem();
                player.SetFeedback("Polozono lawasz na stanowisku.");
                RefreshVisualState();
                PlayPlaceFeedback(item);
                return;
            }

            if (!CanBeAddedToAssembly(item))
            {
                player.SetFeedback("Ten skladnik nie jest gotowy do zlozenia kebaba.");
                return;
            }

            assemblyIngredients.Add(new PreparedIngredientData(item.ingredientKind, item.state));
            player.RemoveHeldItem();
            player.SetFeedback("Dodano do kebaba: " + item.BuildSummary());
            RefreshVisualState();
            PlayPlaceFeedback(item);
            return;
        }

        if (!CanCreateDish())
        {
            player.SetFeedback("Potrzebujesz lawasza, upieczonego miesa i przygotowanych dodatkow.");
            return;
        }

        KitchenItem dish = BuildDish();
        if (!player.TryReceiveItem(dish))
        {
            player.SetFeedback("Masz juz cos w rece.");
            return;
        }

        ResetAssembly();
        player.SetFeedback("Kebab zostal zawiniety.");
        RefreshVisualState();
        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.PlayWrapEffect(transform.position);
            VFXManager.Instance.PlayReadyEffect(transform.position, KitchenItemVisualFactory.GetIngredientColor(IngredientKind.Kebab, IngredientProcessState.Assembled));
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlayWrapSound();
    }

    /// <summary>
    /// Obsługuje wydawanie gotowego kebaba klientowi na stanowisku dostawy.
    /// Próbuje dostarczyć danie do systemu zamówień i uruchamia odpowiednie efekty.
    /// </summary>
    /// <param name="player">Gracz próbujący wydać kebaba klientowi.</param>
    /// <remarks>
    /// Wymaga, aby gracz trzymał gotowy kebab (przedmiot z flagą <c>isDish</c>).
    /// Przy udanym wydaniu:
    /// <list type="bullet">
    /// <item><description>Czyści ręce gracza.</description></item>
    /// <item><description>Wyświetla animację wydanego kebaba na tacy.</description></item>
    /// <item><description>Odtwarza efekty sukcesu (pieniądze, bloom, trzęsienie kamery, zielony błysk).</description></item>
    /// </list>
    /// Przy nieudanym wydaniu uruchamiane są efekty porażki (czerwony błysk, trzęsienie kamery).
    /// </remarks>
    private void HandleDelivery(PlayerInteraction player)
    {
        if (!player.HasItemInHand)
        {
            player.SetFeedback("Podejdz z gotowym kebabem.");
            return;
        }

        if (!player.HeldItem.isDish)
        {
            player.SetFeedback("Klient czeka na gotowego kebaba, a nie luzny skladnik.");
            return;
        }

        if (OrderManager.Instance == null)
        {
            player.SetFeedback("Brak systemu zamowien.");
            return;
        }

        string message;
        if (OrderManager.Instance.TryDeliverDish(player.HeldItem, out message))
        {
            player.ClearHeldItem();
            DeliveryTrayDisplay.ShowServedKebab();

            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.PlayDeliverySuccessEffect(transform.position);
                VFXManager.Instance.PlayMoneyEffect(transform.position);
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayMoneySound();

            if (PostProcessSetup.Instance != null)
            {
                PostProcessSetup.Instance.PulseBloom(0.6f, 0.5f);
            }

            if (CameraEffects.Instance != null)
            {
                CameraEffects.Instance.ShakeCamera(0.08f, 0.3f);
                CameraEffects.Instance.FlashScreen(new Color(0.15f, 0.6f, 0.3f, 0.15f), 0.3f);
            }
        }
        else
        {
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.PlayDeliveryFailEffect(transform.position);
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayFailSound();

            if (CameraEffects.Instance != null)
            {
                CameraEffects.Instance.ShakeCamera(0.06f, 0.25f);
                CameraEffects.Instance.FlashScreen(new Color(0.6f, 0.12f, 0.1f, 0.15f), 0.25f);
            }
        }

        player.SetFeedback(message, 3.5f);
    }

    /// <summary>
    /// Sprawdza, czy dany przedmiot kuchenny może być przetworzony na tym stanowisku.
    /// </summary>
    /// <param name="item">Przedmiot kuchenny do sprawdzenia.</param>
    /// <param name="outputState">Oczekiwany stan wyjściowy przetwarzania.</param>
    /// <returns>
    /// <c>true</c> jeśli przedmiot jest surowy i pasuje do typu stanowiska:
    /// mięso na grill, warzywa (pomidor, cebula, sałata) na deskę do krojenia.
    /// W przeciwnym razie <c>false</c>.
    /// </returns>
    private bool CanBeProcessedHere(KitchenItem item, IngredientProcessState outputState)
    {
        if (item == null || item.isDish || item.state != IngredientProcessState.Raw)
        {
            return false;
        }

        if (outputState == IngredientProcessState.Cooked)
        {
            return item.ingredientKind == IngredientKind.Meat;
        }

        if (outputState == IngredientProcessState.Chopped)
        {
            return item.ingredientKind == IngredientKind.Tomato ||
                item.ingredientKind == IngredientKind.Onion ||
                item.ingredientKind == IngredientKind.Lettuce;
        }

        return false;
    }

    /// <summary>
    /// Sprawdza, czy dany przedmiot kuchenny może zostać dodany do montażu kebaba.
    /// </summary>
    /// <param name="item">Przedmiot kuchenny do weryfikacji.</param>
    /// <returns>
    /// <c>true</c> jeśli przedmiot spełnia wymagania montażu:
    /// <list type="bullet">
    /// <item><description>Mięso — musi być upieczone (<see cref="IngredientProcessState.Cooked"/>).</description></item>
    /// <item><description>Pomidor, cebula, sałata — muszą być pokrojone (<see cref="IngredientProcessState.Chopped"/>).</description></item>
    /// <item><description>Sos czosnkowy — może być surowy (<see cref="IngredientProcessState.Raw"/>).</description></item>
    /// </list>
    /// W przeciwnym razie <c>false</c>.
    /// </returns>
    private bool CanBeAddedToAssembly(KitchenItem item)
    {
        if (item == null || item.isDish)
        {
            return false;
        }

        if (item.ingredientKind == IngredientKind.Meat)
        {
            return item.state == IngredientProcessState.Cooked;
        }

        if (item.ingredientKind == IngredientKind.Tomato ||
            item.ingredientKind == IngredientKind.Onion ||
            item.ingredientKind == IngredientKind.Lettuce)
        {
            return item.state == IngredientProcessState.Chopped;
        }

        if (item.ingredientKind == IngredientKind.GarlicSauce)
        {
            return item.state == IngredientProcessState.Raw;
        }

        return false;
    }

    /// <summary>
    /// Sprawdza, czy na stanowisku montażu zebrano wystarczającą liczbę składników
    /// do utworzenia gotowego kebaba.
    /// </summary>
    /// <returns>
    /// <c>true</c> jeśli stanowisko posiada ławasz oraz co najmniej jedno upieczone mięso
    /// wśród składników montażu. W przeciwnym razie <c>false</c>.
    /// </returns>
    private bool CanCreateDish()
    {
        if (!hasLavash || assemblyIngredients.Count == 0)
        {
            return false;
        }

        bool hasCookedMeat = false;
        foreach (PreparedIngredientData ingredient in assemblyIngredients)
        {
            if (ingredient.ingredientKind == IngredientKind.Meat &&
                ingredient.state == IngredientProcessState.Cooked)
            {
                hasCookedMeat = true;
                break;
            }
        }

        return hasCookedMeat;
    }

    /// <summary>
    /// Tworzy gotowy kebab (danie) na podstawie ławasza i zebranych składników montażu.
    /// </summary>
    /// <returns>
    /// Nowy obiekt <see cref="KitchenItem"/> reprezentujący zawinięty kebab z flagą
    /// <c>isDish = true</c>, zawierający ławasz oraz wszystkie dodane składniki.
    /// </returns>
    private KitchenItem BuildDish()
    {
        KitchenItem dish = new KitchenItem
        {
            itemName = "Zawiniety kebab",
            ingredientKind = IngredientKind.Kebab,
            state = IngredientProcessState.Assembled,
            isDish = true
        };

        dish.contents.Add(new PreparedIngredientData(IngredientKind.Lavash, IngredientProcessState.Raw));
        foreach (PreparedIngredientData ingredient in assemblyIngredients)
        {
            dish.contents.Add(new PreparedIngredientData(ingredient.ingredientKind, ingredient.state));
        }

        return dish;
    }

    /// <summary>
    /// Finalizuje proces przetwarzania składnika na stanowisku.
    /// Zmienia stan przedmiotu, odświeża wizualizację i uruchamia efekty gotowości.
    /// </summary>
    /// <remarks>
    /// Dla stanowiska doner:
    /// <list type="bullet">
    /// <item><description>Zatrzymuje efekt dymu i dźwięk grilla.</description></item>
    /// <item><description>Przesyła partię mięsa na powiązaną tacę.</description></item>
    /// </list>
    /// Dla deski do krojenia — ustawia stan na <see cref="IngredientProcessState.Chopped"/>.
    /// Dla grilla — ustawia stan na <see cref="IngredientProcessState.Cooked"/>
    /// i zatrzymuje efekt pary oraz dźwięk grilla.
    /// </remarks>
    private void FinishProcessing()
    {
        isProcessing = false;
        if (stationType == KitchenStationType.Grill && IsDonerStation())
        {
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.StopDonerSmokeEffect(transform.position);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopGrillAmbient();
            }

            if (linkedMeatTray != null)
            {
                int batchSize = ShopManager.Instance != null
                    ? ShopManager.Instance.GetMeatBatchSize()
                    : preparedMeatBatchSize;
                linkedMeatTray.ReceivePreparedMeat(batchSize);
                if (VFXManager.Instance != null)
                {
                    VFXManager.Instance.PlayReadyEffect(
                        linkedMeatTray.transform.position,
                        KitchenItemVisualFactory.GetIngredientColor(IngredientKind.Meat, IngredientProcessState.Cooked));
                }
                if (AudioManager.Instance != null) AudioManager.Instance.PlayReadySound();
            }

            RefreshVisualState();
            PlayStationPulse(1.035f);
            return;
        }

        if (stationItem == null)
        {
            RefreshVisualState();
            return;
        }

        if (stationType == KitchenStationType.CuttingBoard)
        {
            stationItem.state = IngredientProcessState.Chopped;
        }
        else if (stationType == KitchenStationType.Grill)
        {
            stationItem.state = IngredientProcessState.Cooked;
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.StopSteamEffect(transform.position);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopGrillAmbient();
            }
        }

        RefreshVisualState();
        PlayReadyFeedback(stationItem);
        PlayStationPulse(1.035f);
    }

    /// <summary>
    /// Resetuje stanowisko montażu do stanu początkowego.
    /// Usuwa ławasz i czyści listę składników montażu.
    /// </summary>
    private void ResetAssembly()
    {
        hasLavash = false;
        assemblyIngredients.Clear();
    }

    /// <summary>
    /// Przyjmuje gotowe porcje mięsa na tacę.
    /// Wywoływane przez stanowisko donera po zakończeniu procesu ścinania.
    /// </summary>
    /// <param name="servings">Liczba porcji mięsa do dodania (ujemne wartości są zerowane).</param>
    private void ReceivePreparedMeat(int servings)
    {
        preparedMeatServings = Mathf.Max(0, servings);
        RefreshVisualState();
    }

    /// <summary>
    /// Sprawdza, czy na tacy z mięsem są dostępne gotowe porcje.
    /// </summary>
    /// <returns><c>true</c> jeśli liczba porcji jest większa od zera; w przeciwnym razie <c>false</c>.</returns>
    private bool HasPreparedMeat()
    {
        return preparedMeatServings > 0;
    }

    /// <summary>
    /// Odtwarza efekt wizualny i kamerowy związany z pobraniem przedmiotu ze stanowiska.
    /// </summary>
    /// <param name="item">Pobrany przedmiot kuchenny, na podstawie którego dobierany jest kolor efektu.</param>
    private void PlayPickupFeedback(KitchenItem item)
    {
        if (VFXManager.Instance == null || item == null)
        {
            return;
        }

        VFXManager.Instance.PlayPickupEffect(transform.position, GetItemFeedbackColor(item));
        PlayMicroCameraFeedback(0.012f, 0.08f);
        PlayStationPulse(1.018f);
    }

    /// <summary>
    /// Odtwarza efekt wizualny i dźwiękowy związany z położeniem przedmiotu na stanowisku.
    /// </summary>
    /// <param name="item">Położony przedmiot kuchenny (może być <c>null</c>).</param>
    private void PlayPlaceFeedback(KitchenItem item)
    {
        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.PlayDropEffect(transform.position);
            if (item != null)
            {
                VFXManager.Instance.PlayPickupEffect(transform.position, GetItemFeedbackColor(item));
            }
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDropSound();
        }

        PlayMicroCameraFeedback(0.010f, 0.07f);
        PlayStationPulse(1.015f);
    }

    /// <summary>
    /// Odtwarza efekt wizualny i dźwiękowy sygnalizujący gotowość przedmiotu na stanowisku.
    /// </summary>
    /// <param name="item">Gotowy przedmiot kuchenny, na podstawie którego dobierany jest kolor efektu.</param>
    private void PlayReadyFeedback(KitchenItem item)
    {
        if (item == null)
        {
            return;
        }

        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.PlayReadyEffect(transform.position, GetItemFeedbackColor(item));
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayReadySound();
        }

        PlayMicroCameraFeedback(0.018f, 0.10f);
    }

    /// <summary>
    /// Uruchamia krótki efekt pulsowania (powiększenia) stanowiska za pomocą animatora przedmiotów.
    /// </summary>
    /// <param name="intensity">Intensywność pulsowania (np. 1.035 = powiększenie o 3.5%).</param>
    private void PlayStationPulse(float intensity)
    {
        if (ItemAnimator.Instance != null)
        {
            ItemAnimator.Instance.AnimatePop(gameObject, intensity);
        }
    }

    /// <summary>
    /// Uruchamia subtelne trzęsienie kamery jako informację zwrotną dla gracza.
    /// </summary>
    /// <param name="intensity">Siła trzęsienia kamery.</param>
    /// <param name="duration">Czas trwania trzęsienia w sekundach.</param>
    private void PlayMicroCameraFeedback(float intensity, float duration)
    {
        if (CameraEffects.Instance != null)
        {
            CameraEffects.Instance.ShakeCamera(intensity, duration);
        }
    }

    /// <summary>
    /// Pobiera kolor odpowiadający danemu przedmiotowi kuchennemu do użycia w efektach wizualnych.
    /// </summary>
    /// <param name="item">Przedmiot kuchenny, dla którego pobierany jest kolor.</param>
    /// <returns>
    /// Kolor odpowiadający rodzajowi i stanowi składnika.
    /// Zwraca <c>Color.white</c> jeśli przedmiot jest <c>null</c>.
    /// </returns>
    private Color GetItemFeedbackColor(KitchenItem item)
    {
        if (item == null)
        {
            return Color.white;
        }

        return KitchenItemVisualFactory.GetIngredientColor(item.ingredientKind, item.state);
    }

    /// <summary>
    /// Sprawdza, czy to stanowisko jest tacą z mięsem (źródło składnika typu mięso).
    /// </summary>
    /// <returns>
    /// <c>true</c> jeśli stanowisko jest typu <see cref="KitchenStationType.IngredientSource"/>,
    /// posiada ustawiony składnik źródłowy i jest to mięso. W przeciwnym razie <c>false</c>.
    /// </returns>
    private bool IsMeatTrayStation()
    {
        return stationType == KitchenStationType.IngredientSource &&
            sourceIngredient != null &&
            sourceIngredient.typSkladnika == IngredientKind.Meat;
    }

    /// <summary>
    /// Sprawdza, czy to stanowisko jest donerem (grill powiązany z tacą na mięso).
    /// </summary>
    /// <returns>
    /// <c>true</c> jeśli stanowisko jest typu <see cref="KitchenStationType.Grill"/>
    /// i posiada powiązaną tacę z mięsem. W przeciwnym razie <c>false</c>.
    /// </returns>
    private bool IsDonerStation()
    {
        return stationType == KitchenStationType.Grill && linkedMeatTray != null;
    }

    /// <summary>
    /// Ustala i nakłada odpowiedni kolor materiału na renderer stanowiska
    /// w zależności od bieżącego stanu (przetwarzanie, gotowość, bezczynność).
    /// </summary>
    /// <remarks>
    /// Priorytet kolorów:
    /// <list type="number">
    /// <item><description><see cref="busyColor"/> — stanowisko przetwarza składnik.</description></item>
    /// <item><description><see cref="readyColor"/> — gotowy produkt, mięso na tacy, ławasz lub składniki montażu.</description></item>
    /// <item><description>Kolor debugowy składnika źródłowego — jeśli istnieje składnik źródłowy.</description></item>
    /// <item><description><see cref="idleColor"/> — domyślny kolor bezczynności.</description></item>
    /// </list>
    /// Wywołuje również <see cref="RefreshSpecialVisuals"/> do aktualizacji wizualizacji tacy z mięsem.
    /// </remarks>
    private void ApplyCurrentColor()
    {
        RefreshSpecialVisuals();

        if (visualRenderer == null)
        {
            return;
        }

        Color color = idleColor;
        if (isProcessing)
        {
            color = busyColor;
        }
        else if (stationType == KitchenStationType.Grill && linkedMeatTray != null && linkedMeatTray.HasPreparedMeat())
        {
            color = readyColor;
        }
        else if (IsMeatTrayStation() && preparedMeatServings > 0)
        {
            color = readyColor;
        }
        else if (stationItem != null || hasLavash || assemblyIngredients.Count > 0)
        {
            color = readyColor;
        }
        else if (sourceIngredient != null)
        {
            color = sourceIngredient.kolorDebug;
        }

        visualRenderer.material.color = color;
    }

    /// <summary>
    /// Oblicza hash bieżącego stanu wizualnego stanowiska.
    /// Używany do optymalizacji — wizualizacja jest odświeżana tylko gdy hash się zmieni.
    /// </summary>
    /// <returns>
    /// Wartość hash obliczona na podstawie stanu przetwarzania, przedmiotu na stanowisku,
    /// flagi ławasza oraz listy składników montażu.
    /// </returns>
    private int ComputeVisualHash()
    {
        int hash = 17;
        hash = hash * 31 + (isProcessing ? 1 : 0);
        if (stationItem != null)
        {
            hash = hash * 31 + (int)stationItem.ingredientKind;
            hash = hash * 31 + (int)stationItem.state;
            hash = hash * 31 + (stationItem.isDish ? 1 : 0);
        }
        hash = hash * 31 + (hasLavash ? 1 : 0);
        hash = hash * 31 + assemblyIngredients.Count;
        for (int i = 0; i < assemblyIngredients.Count; i++)
        {
            hash = hash * 31 + (int)assemblyIngredients[i].ingredientKind;
            hash = hash * 31 + (int)assemblyIngredients[i].state;
        }
        return hash;
    }

    /// <summary>
    /// Aktualizuje dynamiczne obiekty wizualne 3D na stanowisku (przedmioty, ławasz, składniki montażu).
    /// Używa systemu hashowania do unikania niepotrzebnych odświeżeń.
    /// </summary>
    /// <remarks>
    /// Metoda niszczy poprzednie obiekty wizualne i tworzy nowe za pomocą <see cref="KitchenItemVisualFactory"/>.
    /// Dla stanowiska montażu tworzy wizualizacje ławasza oraz poszczególnych składników
    /// rozmieszczonych w predefiniowanych slotach z offsetami i rotacjami.
    /// Składniki typu sałata i sos czosnkowy są renderowane jako rozsypane kawałki.
    /// </remarks>
    private void UpdateDynamicVisuals()
    {
        int currentHash = ComputeVisualHash();
        if (currentHash == lastVisualHash) return;
        lastVisualHash = currentHash;

        if (dynamicStationItemVisual != null) Destroy(dynamicStationItemVisual);
        if (dynamicLavashVisual != null) Destroy(dynamicLavashVisual);
        foreach (var vis in dynamicAssemblyVisuals) if (vis != null) Destroy(vis);
        dynamicAssemblyVisuals.Clear();

        if (stationItem != null)
        {

            Vector3 itemPos = new Vector3(0f, 0.42f, 0f);
            Vector3 itemRot = Vector3.zero;
            float itemSize = 0.30f;

            if (stationItem.isDish)
            {
                itemRot = new Vector3(0f, 25f, 90f);
                itemSize = 0.35f;
            }
            else if (stationType == KitchenStationType.CuttingBoard)
            {

                itemRot = new Vector3(-5f, 30f, 0f);
                itemSize = 0.28f;
            }
            else if (stationType == KitchenStationType.Grill)
            {

                itemRot = new Vector3(0f, 15f, 0f);
                itemSize = 0.30f;
            }

            dynamicStationItemVisual = KitchenItemVisualFactory.CreateItemVisual(
                stationItem.ingredientKind, stationItem.state, stationItem.isDish,
                transform, itemPos, itemRot, itemSize);
        }
        else if (stationType == KitchenStationType.Assembly)
        {

            float baseHeight = 0.425f;
            Vector3 centerOffset = new Vector3(0.14f, 0f, 0f);

            if (hasLavash)
            {
                dynamicLavashVisual = KitchenItemVisualFactory.CreateItemVisual(
                    IngredientKind.Lavash, IngredientProcessState.Raw, false,
                    transform, centerOffset + new Vector3(0f, baseHeight, 0f), new Vector3(0f, 12f, 0f), 0.65f);
            }

            float layerHeight = baseHeight + 0.012f;

            Vector3[] slotPositions = new Vector3[]
            {
                new Vector3( 0.00f, 0f,  0.00f),
                new Vector3(-0.06f, 0f,  0.05f),
                new Vector3( 0.06f, 0f,  0.04f),
                new Vector3( 0.00f, 0f, -0.06f),
                new Vector3(-0.05f, 0f, -0.04f),
                new Vector3( 0.05f, 0f, -0.05f),
                new Vector3(-0.03f, 0f,  0.00f),
                new Vector3( 0.03f, 0f,  0.00f),
            };

            float[] slotRotations = new float[] { 0f, 25f, -15f, 45f, -30f, 60f, 10f, -45f };

            for (int i = 0; i < assemblyIngredients.Count; i++)
            {
                var ingredient = assemblyIngredients[i];
                int slotIndex = i % slotPositions.Length;

                float stackOffset = i * 0.005f;
                Vector3 pos = centerOffset + new Vector3(
                    slotPositions[slotIndex].x,
                    layerHeight + stackOffset,
                    slotPositions[slotIndex].z);

                if (ingredient.ingredientKind == IngredientKind.Lettuce || ingredient.ingredientKind == IngredientKind.GarlicSauce)
                {
                    int pieces = ingredient.ingredientKind == IngredientKind.GarlicSauce ? 4 : 6;
                    float spread = 0.035f;
                    float pSize = ingredient.ingredientKind == IngredientKind.GarlicSauce ? 0.03f : 0.02f;

                    GameObject scatteredVis = KitchenItemVisualFactory.CreateScatteredVisual(
                        ingredient.ingredientKind, ingredient.state,
                        transform, pos, pieces, spread, pSize);

                    if (scatteredVis != null) dynamicAssemblyVisuals.Add(scatteredVis);
                }
                else
                {

                    float ingSize = 0.16f;
                    Vector3 ingRot = new Vector3(0f, slotRotations[slotIndex], 0f);

                    switch (ingredient.ingredientKind)
                    {
                        case IngredientKind.Meat:
                            ingSize = 0.18f;
                            ingRot.x = -10f;
                            break;
                        case IngredientKind.Tomato:
                            ingSize = 0.14f;
                            pos.y += 0.02f;
                            break;
                        case IngredientKind.Onion:
                            ingSize = 0.13f;
                            pos.y += 0.02f;
                            break;
                    }

                    GameObject ingVis = KitchenItemVisualFactory.CreateItemVisual(
                        ingredient.ingredientKind, ingredient.state, false,
                        transform, pos, ingRot, ingSize);

                    if (ingVis != null) dynamicAssemblyVisuals.Add(ingVis);
                }
            }
        }
    }

    /// <summary>
    /// Odświeża specjalne wizualizacje stanowiska, w szczególności widoczność mięsa na tacy.
    /// </summary>
    /// <remarks>
    /// Dotyczy wyłącznie stanowisk typu taca z mięsem. Wyszukuje obiekt potomny "MeatVisual"
    /// i ustawia jego aktywność w zależności od liczby dostępnych porcji mięsa.
    /// </remarks>
    private void RefreshSpecialVisuals()
    {
        if (!IsMeatTrayStation())
        {
            return;
        }

        if (meatVisual == null)
        {
            meatVisual = transform.Find("MeatVisual");
        }

        if (meatVisual != null)
        {
            meatVisual.gameObject.SetActive(preparedMeatServings > 0);
        }
    }

    /// <summary>
    /// Pobiera mnożnik prędkości przetwarzania ze sklepu dla aktualnego typu stanowiska.
    /// </summary>
    /// <returns>
    /// Mnożnik prędkości z <see cref="ShopManager"/>, lub <c>1.0f</c> jeśli
    /// <see cref="ShopManager"/> nie jest dostępny.
    /// </returns>
    private float GetShopSpeedMultiplier()
    {
        if (ShopManager.Instance == null)
        {
            return 1f;
        }

        return ShopManager.Instance.GetProcessingSpeedMultiplier(stationType);
    }
}
