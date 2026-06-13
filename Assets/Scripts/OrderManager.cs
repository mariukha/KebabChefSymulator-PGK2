/// \file OrderManager.cs
/// \brief Plik zawierający klasę OrderManager odpowiedzialną za system zamówień w grze.
/// \details Zarządza generowaniem, śledzeniem i walidacją zamówień na kebaby.
/// Obsługuje szablony zamówień, katalog składników, obliczanie nagród,
/// skalowanie trudności oraz synchronizację stanu w trybie wieloosobowym.

using System.Globalization;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Menedżer systemu zamówień w grze Kebab Chef Symulator.
/// Zarządza generowaniem nowych zamówień z predefiniowanych szablonów,
/// śledzeniem czasu na realizację, walidacją dostarczonych dań,
/// obliczaniem nagród pieniężnych oraz skalowaniem trudności gry.
/// Implementuje wzorzec Singleton — dostępny globalnie przez <see cref="Instance"/>.
/// </summary>
/// <remarks>
/// Menedżer automatycznie inicjalizuje katalog składników i domyślne szablony zamówień,
/// jeśli nie zostały one skonfigurowane w inspektorze Unity.
/// Obsługuje tryb wieloosobowy, w którym tylko serwer zarządza czasem i generowaniem zamówień.
/// </remarks>
public class OrderManager : MonoBehaviour
{
    /// <summary>
    /// Statyczna instancja Singletona menedżera zamówień, dostępna globalnie.
    /// </summary>
    public static OrderManager Instance { get; private set; }

    /// <summary>
    /// Lista dostępnych danych składników w grze.
    /// Może być konfigurowana z poziomu inspektora Unity lub uzupełniana automatycznie w czasie wykonywania.
    /// </summary>
    public List<IngredientData> dostepneSkladniki = new List<IngredientData>();

    /// <summary>
    /// Lista szablonów zamówień, z których losowane są nowe zamówienia.
    /// Szablony definiują wymagane składniki, czas realizacji i inne parametry zamówienia.
    /// </summary>
    [SerializeField] private List<Order> orderTemplates = new List<Order>();

    /// <summary>
    /// Aktualnie aktywne zamówienie, które gracz musi zrealizować.
    /// Wartość null oznacza brak aktywnego zamówienia.
    /// </summary>
    [SerializeField] private Order activeOrder;

    /// <summary>
    /// Indeks aktualnego szablonu zamówienia w liście <see cref="orderTemplates"/>.
    /// Wartość -1 oznacza, że żadne zamówienie nie zostało jeszcze wygenerowane.
    /// </summary>
    public int ActiveTemplateIndex { get; private set; } = -1;

    /// <summary>
    /// Tekstowy opis aktywnego zamówienia wyświetlany graczowi.
    /// </summary>
    private string activeOrderDescription = "";

    /// <summary>
    /// Pozostały czas na realizację aktywnego zamówienia w sekundach.
    /// </summary>
    private float remainingTime;

    /// <summary>
    /// Licznik pomyślnie zrealizowanych zamówień od początku sesji gry.
    /// </summary>
    private int completedOrders;

    /// <summary>
    /// Licznik nieudanych zamówień (przeterminowanych) od początku sesji gry.
    /// </summary>
    private int failedOrders;

    /// <summary>
    /// Ostatni komunikat statusowy dotyczący zamówień, wyświetlany w interfejsie użytkownika.
    /// </summary>
    private string lastMessage = "Przygotuj pierwszy kebab.";

    /// <summary>
    /// Słownik mapujący typ składnika na jego dane, umożliwiający szybkie wyszukiwanie.
    /// </summary>
    private readonly Dictionary<IngredientKind, IngredientData> ingredientLookup =
        new Dictionary<IngredientKind, IngredientData>();

    /// <summary>
    /// Aktualnie aktywne zamówienie.
    /// </summary>
    /// <value>Obiekt <see cref="Order"/> lub <c>null</c>, gdy brak aktywnego zamówienia.</value>
    public Order ActiveOrder => activeOrder;

    /// <summary>
    /// Pozostały czas na realizację zamówienia w sekundach (alias dla <see cref="RemainingTime"/>).
    /// </summary>
    public float RemainingOrderTime => remainingTime;

    /// <summary>
    /// Liczba zrealizowanych zamówień.
    /// </summary>
    public int CompletedOrders => completedOrders;

    /// <summary>
    /// Liczba nieudanych zamówień.
    /// </summary>
    public int FailedOrders => failedOrders;

    /// <summary>
    /// Ostatni komunikat dotyczący statusu zamówienia.
    /// </summary>
    public string LastOrderMessage => lastMessage;

    /// <summary>
    /// Tekstowy opis aktywnego zamówienia.
    /// </summary>
    public string ActiveOrderDescription => activeOrderDescription;

    /// <summary>
    /// Określa, czy istnieje aktywne zamówienie do realizacji.
    /// </summary>
    /// <value><c>true</c> jeśli istnieje aktywne zamówienie; w przeciwnym razie <c>false</c>.</value>
    public bool HasActiveOrder => activeOrder != null;

    /// <summary>
    /// Pozostały czas na realizację zamówienia w sekundach.
    /// </summary>
    public float RemainingTime => remainingTime;

    /// <summary>
    /// Synchronizuje stan zamówienia z serwera sieciowego.
    /// Ustawia aktywne zamówienie na podstawie indeksu szablonu oraz aktualizuje
    /// opis, czas, liczbę zrealizowanych i nieudanych zamówień.
    /// </summary>
    /// <param name="templateIndex">Indeks szablonu zamówienia na serwerze.</param>
    /// <param name="desc">Opis aktywnego zamówienia.</param>
    /// <param name="time">Pozostały czas na realizację w sekundach.</param>
    /// <param name="comp">Liczba zrealizowanych zamówień.</param>
    /// <param name="fail">Liczba nieudanych zamówień.</param>
    public void SyncNetworkState(int templateIndex, string desc, float time, int comp, int fail)
    {
        InitializeCatalogIfNeeded();
        BuildDefaultTemplatesIfNeeded();

        if (templateIndex >= 0 && templateIndex < orderTemplates.Count && ActiveTemplateIndex != templateIndex)
        {
            ActiveTemplateIndex = templateIndex;
            activeOrder = orderTemplates[templateIndex].Clone();
            activeOrder.nagrodaPieniezna = CalculateReward(activeOrder.wymaganeSkladniki);
        }

        activeOrderDescription = desc;
        remainingTime = time;
        completedOrders = comp;
        failedOrders = fail;
        lastMessage = desc;
    }

    /// <summary>
    /// Inicjalizacja Singletona oraz katalogu składników i szablonów zamówień.
    /// Jeśli istnieje już instancja, niszczy duplikat.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeCatalogIfNeeded();
        BuildDefaultTemplatesIfNeeded();
    }

    /// <summary>
    /// Wywoływana po inicjalizacji. Generuje pierwsze zamówienie,
    /// chyba że gra działa w trybie wieloosobowym jako klient (nie serwer).
    /// </summary>
    private void Start()
    {
        bool isMultiplayerClient = Unity.Netcode.NetworkManager.Singleton != null && 
                                   Unity.Netcode.NetworkManager.Singleton.IsListening &&
                                   !Unity.Netcode.NetworkManager.Singleton.IsServer;

        if (activeOrder == null && !isMultiplayerClient)
        {
            NoweZamowienie();
        }
    }

    /// <summary>
    /// Aktualizacja wywoływana co klatkę. Odlicza czas na realizację aktywnego zamówienia.
    /// Jeśli czas się skończy, zamówienie jest oznaczane jako nieudane,
    /// odtwarzany jest efekt zwrotny i generowane jest nowe zamówienie.
    /// Działa tylko po stronie serwera w trybie wieloosobowym.
    /// </summary>
    private void Update()
    {
        bool isServer = Unity.Netcode.NetworkManager.Singleton == null ||
                        !Unity.Netcode.NetworkManager.Singleton.IsListening ||
                        Unity.Netcode.NetworkManager.Singleton.IsServer;

        if (!isServer || activeOrder == null)
        {
            return;
        }

        remainingTime -= Time.deltaTime;
        if (remainingTime > 0f)
        {
            return;
        }

        failedOrders++;
        lastMessage = "Klient odszedl. Zamowienie nie zostalo przygotowane na czas.";
        PlayTimeoutFeedback();
        NoweZamowienie();
    }

    /// <summary>
    /// Inicjalizuje katalog składników, jeśli nie został jeszcze zbudowany.
    /// Przetwarza ręcznie przypisane składniki z listy <see cref="dostepneSkladniki"/>
    /// oraz dodaje brakujące składniki domyślne (mięso, pomidor, cebula, sałata, sos, ławasz, kebab).
    /// </summary>
    public void InitializeCatalogIfNeeded()
    {
        if (ingredientLookup.Count > 0)
        {
            return;
        }

        foreach (IngredientData ingredient in dostepneSkladniki)
        {
            if (ingredient == null)
            {
                continue;
            }

            NormalizeAssignedIngredient(ingredient);
            ingredientLookup[ingredient.typSkladnika] = ingredient;
        }

        EnsureRuntimeIngredient(IngredientKind.Meat, "Mieso", IngredientProcessState.Raw, new Color(0.65f, 0.25f, 0.18f), 7f, 14f);
        EnsureRuntimeIngredient(IngredientKind.Tomato, "Pomidor", IngredientProcessState.Raw, new Color(0.86f, 0.2f, 0.2f), 2f, 5f);
        EnsureRuntimeIngredient(IngredientKind.Onion, "Cebula", IngredientProcessState.Raw, new Color(0.93f, 0.9f, 0.75f), 1.5f, 4f);
        EnsureRuntimeIngredient(IngredientKind.Lettuce, "Salata", IngredientProcessState.Raw, new Color(0.35f, 0.7f, 0.25f), 1.2f, 3f);
        EnsureRuntimeIngredient(IngredientKind.GarlicSauce, "Sos czosnkowy", IngredientProcessState.Raw, new Color(0.95f, 0.95f, 0.85f), 1.5f, 4f);
        EnsureRuntimeIngredient(IngredientKind.Lavash, "Lawasz", IngredientProcessState.Raw, new Color(0.86f, 0.74f, 0.5f), 3f, 6f);
        EnsureRuntimeIngredient(IngredientKind.Kebab, "Kebab", IngredientProcessState.Assembled, new Color(0.76f, 0.57f, 0.35f), 0f, 20f);
    }

    /// <summary>
    /// Pobiera definicję danych składnika na podstawie jego typu.
    /// Automatycznie inicjalizuje katalog, jeśli jeszcze nie został zbudowany.
    /// </summary>
    /// <param name="kind">Typ składnika do wyszukania.</param>
    /// <returns>Dane składnika <see cref="IngredientData"/> lub <c>null</c>, jeśli nie znaleziono.</returns>
    public IngredientData GetIngredientDefinition(IngredientKind kind)
    {
        InitializeCatalogIfNeeded();
        IngredientData data;
        ingredientLookup.TryGetValue(kind, out data);
        return data;
    }

    /// <summary>
    /// Próbuje dostarczyć przygotowane danie i zwalidować je względem aktywnego zamówienia.
    /// W przypadku sukcesu nalicza nagrodę pieniężną, generuje nowe zamówienie i zapisuje grę.
    /// </summary>
    /// <param name="item">Przedmiot kuchenny (danie) do dostarczenia.</param>
    /// <param name="message">Komunikat wyjściowy opisujący wynik operacji.</param>
    /// <returns><c>true</c> jeśli danie pasuje do zamówienia; <c>false</c> w przeciwnym razie.</returns>
    public bool TryDeliverDish(KitchenItem item, out string message)
    {
        if (activeOrder == null)
        {
            message = "Brak aktywnego zamowienia.";
            return false;
        }

        string failureReason;
        if (!KitchenOrderValidator.MatchesOrder(activeOrder, item, out failureReason))
        {
            message = "Zly kebab: " + failureReason;
            lastMessage = message;
            return false;
        }

        completedOrders++;
        float reward = activeOrder.nagrodaPieniezna;

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddMoney(reward);
        }

        message = "Zamowienie zrealizowane. Nagroda: " + reward + " zl.";
        lastMessage = message;
        NoweZamowienie();
        SaveManager.Instance?.SaveGame();
        return true;
    }

    /// <summary>
    /// Generuje nowe losowe zamówienie z dostępnych szablonów.
    /// Uwzględnia progresję trudności (więcej szablonów w miarę ukończonych zamówień),
    /// oblicza nagrodę pieniężną, stosuje bonus czasowy ze sklepu
    /// i mnożnik trudności do czasu realizacji.
    /// </summary>
    public void NoweZamowienie()
    {
        InitializeCatalogIfNeeded();
        BuildDefaultTemplatesIfNeeded();

        int maxIndex = GetAvailableTemplateCount();
        int index = Random.Range(0, maxIndex);
        ActiveTemplateIndex = index;
        activeOrder = orderTemplates[index].Clone();
        activeOrder.nagrodaPieniezna = CalculateReward(activeOrder.wymaganeSkladniki);
        float timeBonus = ShopManager.Instance != null ? ShopManager.Instance.GetOrderTimeBonus() : 0f;
        float difficultyMultiplier = GetDifficultyTimeMultiplier();
        remainingTime = (activeOrder.czasNaRealizacje * difficultyMultiplier) + timeBonus;
        string desc = "Nowe zamowienie: " + activeOrder.BuildDescription();
        lastMessage = desc;
        activeOrderDescription = desc;
        Debug.Log(desc);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayNewOrderSound();
    }

    /// <summary>
    /// Odtwarza efekty zwrotne po przekroczeniu czasu zamówienia.
    /// Obejmuje dźwięk porażki, efekt wizualny timeout oraz wstrząs i błysk kamery.
    /// </summary>
    private void PlayTimeoutFeedback()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayFailSound();
        }

        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.PlayTimeoutEffect();
        }

        if (CameraEffects.Instance != null)
        {
            CameraEffects.Instance.ShakeCamera(0.045f, 0.22f);
            CameraEffects.Instance.FlashScreen(new Color(0.55f, 0.08f, 0.06f, 0.12f), 0.22f);
        }
    }

    /// <summary>
    /// Oblicza mnożnik czasu trudności na podstawie liczby ukończonych zamówień.
    /// Im więcej zamówień ukończono, tym mniej czasu gracz otrzymuje (minimum 30% oryginalnego czasu).
    /// </summary>
    /// <returns>Mnożnik czasu w zakresie [0.3, 1.0].</returns>
    private float GetDifficultyTimeMultiplier()
    {

        return Mathf.Max(0.3f, 1f - (completedOrders * 0.02f));
    }

    /// <summary>
    /// Zwraca liczbę dostępnych szablonów zamówień na podstawie postępu gracza.
    /// Na początku dostępne są 3 szablony, po 3 zamówieniach — 6, a po 6 — wszystkie.
    /// Implementuje progresywne odblokowanie trudniejszych zamówień.
    /// </summary>
    /// <returns>Liczba dostępnych szablonów zamówień.</returns>
    private int GetAvailableTemplateCount()
    {

        if (orderTemplates.Count <= 3)
        {
            return orderTemplates.Count;
        }

        if (completedOrders < 3)
        {
            return 3;
        }

        if (completedOrders < 6)
        {
            return Mathf.Min(orderTemplates.Count, 6);
        }

        return orderTemplates.Count;
    }

    /// <summary>
    /// Ustawia stan zamówienia na podstawie danych otrzymanych z sieci (dla klientów).
    /// Używane do synchronizacji stanu zamówienia w trybie wieloosobowym.
    /// </summary>
    /// <param name="description">Opis aktywnego zamówienia.</param>
    /// <param name="time">Pozostały czas na realizację.</param>
    /// <param name="completed">Liczba ukończonych zamówień.</param>
    /// <param name="failed">Liczba nieudanych zamówień.</param>
    /// <param name="message">Ostatni komunikat statusowy.</param>
    public void SetOrderStateFromNetwork(string description, float time, int completed, int failed, string message)
    {
        activeOrderDescription = description;
        remainingTime = time;
        completedOrders = completed;
        failedOrders = failed;
        lastMessage = message;
    }

    /// <summary>
    /// Przechwytuje bieżący postęp zamówień do struktury danych przeznaczonej do zapisu.
    /// Obejmuje statystyki ukończonych/nieudanych zamówień, czas i dane aktywnego zamówienia.
    /// </summary>
    /// <returns>Obiekt <see cref="OrderProgressSaveData"/> zawierający aktualny stan postępu.</returns>
    public OrderProgressSaveData CaptureProgress()
    {
        OrderProgressSaveData data = new OrderProgressSaveData
        {
            completedOrders = completedOrders,
            failedOrders = failedOrders,
            remainingOrderTime = remainingTime,
            lastOrderMessage = lastMessage
        };

        if (activeOrder == null)
        {
            return data;
        }

        data.activeOrder = new OrderSaveData
        {
            orderId = activeOrder.orderId,
            clientName = activeOrder.nazwaKlienta,
            orderName = activeOrder.nazwaZamowienia,
            timeLimit = activeOrder.czasNaRealizacje,
            reward = activeOrder.nagrodaPieniezna
        };

        foreach (IngredientRequirement requirement in activeOrder.wymaganeSkladniki)
        {
            data.activeOrder.requirements.Add(new IngredientRequirement(
                requirement.ingredientKind,
                requirement.requiredState,
                requirement.quantity));
        }

        return data;
    }

    /// <summary>
    /// Przywraca postęp zamówień z wcześniej zapisanych danych.
    /// Odtwarza statystyki, aktywne zamówienie z wymaganiami składników
    /// oraz pozostały czas realizacji (minimum 5 sekund).
    /// Jeśli dane nie zawierają aktywnego zamówienia, generowane jest nowe.
    /// </summary>
    /// <param name="data">Dane zapisu postępu zamówień do przywrócenia.</param>
    public void RestoreProgress(OrderProgressSaveData data)
    {
        if (data == null)
        {
            return;
        }

        completedOrders = Mathf.Max(0, data.completedOrders);
        failedOrders = Mathf.Max(0, data.failedOrders);
        if (!string.IsNullOrWhiteSpace(data.lastOrderMessage))
        {
            lastMessage = data.lastOrderMessage;
        }

        if (data.activeOrder == null || data.activeOrder.requirements == null || data.activeOrder.requirements.Count == 0)
        {
            activeOrder = null;
            NoweZamowienie();
            return;
        }

        activeOrder = new Order
        {
            orderId = data.activeOrder.orderId,
            nazwaKlienta = data.activeOrder.clientName,
            nazwaZamowienia = data.activeOrder.orderName,
            czasNaRealizacje = data.activeOrder.timeLimit,
            nagrodaPieniezna = data.activeOrder.reward
        };

        foreach (IngredientRequirement requirement in data.activeOrder.requirements)
        {
            activeOrder.wymaganeSkladniki.Add(new IngredientRequirement(
                requirement.ingredientKind,
                requirement.requiredState,
                requirement.quantity));
        }

        remainingTime = Mathf.Max(5f, data.remainingOrderTime);
        activeOrderDescription = "Wczytane: " + activeOrder.BuildDescription();
    }

    /// <summary>
    /// Tworzy składnik w czasie wykonywania, jeśli nie istnieje jeszcze w katalogu.
    /// Tworzy instancję ScriptableObject z podanymi parametrami i dodaje ją do katalogu.
    /// </summary>
    /// <param name="kind">Typ składnika do utworzenia.</param>
    /// <param name="ingredientName">Wyświetlana nazwa składnika.</param>
    /// <param name="defaultState">Domyślny stan przetworzenia składnika.</param>
    /// <param name="debugColor">Kolor debugowania używany do wizualizacji.</param>
    /// <param name="purchasePrice">Cena zakupu składnika.</param>
    /// <param name="saleValue">Wartość sprzedaży składnika.</param>
    private void EnsureRuntimeIngredient(
        IngredientKind kind,
        string ingredientName,
        IngredientProcessState defaultState,
        Color debugColor,
        float purchasePrice,
        float saleValue)
    {
        if (ingredientLookup.ContainsKey(kind))
        {
            return;
        }

        IngredientData ingredient = ScriptableObject.CreateInstance<IngredientData>();
        ingredient.nazwaSkladnika = ingredientName;
        ingredient.typSkladnika = kind;
        ingredient.stanPoczatkowy = defaultState;
        ingredient.cenaZakupu = purchasePrice;
        ingredient.wartoscSprzedazy = saleValue;
        ingredient.kolorDebug = debugColor;

        dostepneSkladniki.Add(ingredient);
        ingredientLookup[kind] = ingredient;
    }

    /// <summary>
    /// Normalizuje ręcznie przypisany składnik, wykrywając jego typ na podstawie nazwy.
    /// Rozpoznaje mięso i pomidor po polskich nazwach (z uwzględnieniem znaków diakrytycznych).
    /// Ustawia odpowiedni kolor debugowania, wartość sprzedaży oraz stan początkowy.
    /// </summary>
    /// <param name="ingredient">Dane składnika do znormalizowania.</param>
    private void NormalizeAssignedIngredient(IngredientData ingredient)
    {
        string sourceName = RemoveDiacritics(
            ((ingredient.DisplayName ?? string.Empty) + " " + ingredient.name).ToLowerInvariant());

        if (sourceName.Contains("mies"))
        {
            ingredient.typSkladnika = IngredientKind.Meat;
            ingredient.kolorDebug = new Color(0.65f, 0.25f, 0.18f);
            ingredient.wartoscSprzedazy = ingredient.wartoscSprzedazy <= 0f ? 14f : ingredient.wartoscSprzedazy;
        }
        else if (sourceName.Contains("pomid"))
        {
            ingredient.typSkladnika = IngredientKind.Tomato;
            ingredient.kolorDebug = new Color(0.86f, 0.2f, 0.2f);
            ingredient.wartoscSprzedazy = ingredient.wartoscSprzedazy <= 0f ? 5f : ingredient.wartoscSprzedazy;
        }

        if (ingredient.typSkladnika != IngredientKind.Kebab)
        {
            ingredient.stanPoczatkowy = IngredientProcessState.Raw;
        }
    }

    /// <summary>
    /// Usuwa znaki diakrytyczne (akcenty, ogonki) z podanego tekstu.
    /// Używa normalizacji Unicode FormD, aby rozłożyć znaki na części składowe,
    /// a następnie odfiltrować znaki niebędące spacjami (NonSpacingMark).
    /// </summary>
    /// <param name="value">Tekst, z którego mają zostać usunięte znaki diakrytyczne.</param>
    /// <returns>Tekst bez znaków diakrytycznych lub pusty ciąg, jeśli wejście jest puste.</returns>
    private string RemoveDiacritics(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string normalized = value.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new StringBuilder(normalized.Length);

        foreach (char character in normalized)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Buduje domyślne szablony zamówień, jeśli lista szablonów jest pusta.
    /// Tworzy 8 predefiniowanych zamówień o rosnącej złożoności:
    /// klasyczny, mięsny, fresh, warzywny, czosnkowy, ekspresowy, podwójny deluxe i królewski.
    /// Każdy szablon ma unikalny identyfikator, nazwę klienta, wymagane składniki i limit czasu.
    /// </summary>
    private void BuildDefaultTemplatesIfNeeded()
    {
        if (orderTemplates.Count > 0)
        {
            return;
        }

        orderTemplates.Add(CreateTemplate(
            "classic",
            "Adam",
            "Klasyczny kebab",
            95f,
            new IngredientRequirement(IngredientKind.Lavash, IngredientProcessState.Raw),
            new IngredientRequirement(IngredientKind.Meat, IngredientProcessState.Cooked),
            new IngredientRequirement(IngredientKind.Tomato, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.Onion, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.GarlicSauce, IngredientProcessState.Raw)));

        orderTemplates.Add(CreateTemplate(
            "meaty",
            "Basia",
            "Kebab miesny",
            85f,
            new IngredientRequirement(IngredientKind.Lavash, IngredientProcessState.Raw),
            new IngredientRequirement(IngredientKind.Meat, IngredientProcessState.Cooked, 2),
            new IngredientRequirement(IngredientKind.Onion, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.GarlicSauce, IngredientProcessState.Raw)));

        orderTemplates.Add(CreateTemplate(
            "fresh",
            "Celina",
            "Kebab fresh",
            90f,
            new IngredientRequirement(IngredientKind.Lavash, IngredientProcessState.Raw),
            new IngredientRequirement(IngredientKind.Meat, IngredientProcessState.Cooked),
            new IngredientRequirement(IngredientKind.Tomato, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.Lettuce, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.GarlicSauce, IngredientProcessState.Raw)));

        orderTemplates.Add(CreateTemplate(
            "veggie",
            "Dawid",
            "Kebab warzywny",
            80f,
            new IngredientRequirement(IngredientKind.Lavash, IngredientProcessState.Raw),
            new IngredientRequirement(IngredientKind.Meat, IngredientProcessState.Cooked),
            new IngredientRequirement(IngredientKind.Tomato, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.Onion, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.Lettuce, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.GarlicSauce, IngredientProcessState.Raw)));

        orderTemplates.Add(CreateTemplate(
            "garlic-bomb",
            "Ewa",
            "Kebab czosnkowy",
            75f,
            new IngredientRequirement(IngredientKind.Lavash, IngredientProcessState.Raw),
            new IngredientRequirement(IngredientKind.Meat, IngredientProcessState.Cooked, 2),
            new IngredientRequirement(IngredientKind.Tomato, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.GarlicSauce, IngredientProcessState.Raw)));

        orderTemplates.Add(CreateTemplate(
            "light-quick",
            "Filip",
            "Kebab ekspresowy",
            60f,
            new IngredientRequirement(IngredientKind.Lavash, IngredientProcessState.Raw),
            new IngredientRequirement(IngredientKind.Meat, IngredientProcessState.Cooked),
            new IngredientRequirement(IngredientKind.Lettuce, IngredientProcessState.Chopped)));

        orderTemplates.Add(CreateTemplate(
            "double-deluxe",
            "Grzegorz",
            "Kebab podwojny deluxe",
            100f,
            new IngredientRequirement(IngredientKind.Lavash, IngredientProcessState.Raw),
            new IngredientRequirement(IngredientKind.Meat, IngredientProcessState.Cooked, 2),
            new IngredientRequirement(IngredientKind.Tomato, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.Onion, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.Lettuce, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.GarlicSauce, IngredientProcessState.Raw)));

        orderTemplates.Add(CreateTemplate(
            "royal-feast",
            "Hanna",
            "Kebab krolewski",
            110f,
            new IngredientRequirement(IngredientKind.Lavash, IngredientProcessState.Raw),
            new IngredientRequirement(IngredientKind.Meat, IngredientProcessState.Cooked, 2),
            new IngredientRequirement(IngredientKind.Tomato, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.Onion, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.Lettuce, IngredientProcessState.Chopped)));
    }

    /// <summary>
    /// Tworzy szablon zamówienia z podanymi parametrami.
    /// Automatycznie oblicza nagrodę pieniężną na podstawie wymaganych składników.
    /// </summary>
    /// <param name="id">Unikalny identyfikator szablonu zamówienia.</param>
    /// <param name="clientName">Nazwa klienta składającego zamówienie.</param>
    /// <param name="orderName">Nazwa zamówienia wyświetlana graczowi.</param>
    /// <param name="timeLimit">Limit czasu na realizację zamówienia w sekundach.</param>
    /// <param name="requirements">Tablica wymaganych składników z ich stanami przetworzenia.</param>
    /// <returns>Nowy obiekt <see cref="Order"/> skonfigurowany jako szablon zamówienia.</returns>
    private Order CreateTemplate(
        string id,
        string clientName,
        string orderName,
        float timeLimit,
        params IngredientRequirement[] requirements)
    {
        Order order = new Order
        {
            orderId = id,
            nazwaKlienta = clientName,
            nazwaZamowienia = orderName,
            czasNaRealizacje = timeLimit
        };

        foreach (IngredientRequirement requirement in requirements)
        {
            order.wymaganeSkladniki.Add(requirement);
        }

        order.nagrodaPieniezna = CalculateReward(order.wymaganeSkladniki);
        return order;
    }

    /// <summary>
    /// Oblicza nagrodę pieniężną za realizację zamówienia na podstawie listy wymaganych składników.
    /// Bazowa nagroda wynosi 10 zł, do której dodawane są wartości sprzedaży każdego składnika
    /// pomnożone przez ilość. Przetworzenie składnika (stan inny niż surowy) dodaje bonus 1.5 zł za sztukę.
    /// Wynik jest mnożony przez mnożnik nagrody ze sklepu i zaokrąglany.
    /// </summary>
    /// <param name="requirements">Lista wymagań składnikowych zamówienia.</param>
    /// <returns>Obliczona nagroda pieniężna zaokrąglona do pełnej kwoty.</returns>
    private float CalculateReward(List<IngredientRequirement> requirements)
    {
        float total = 10f;
        foreach (IngredientRequirement requirement in requirements)
        {
            IngredientData ingredient;
            if (ingredientLookup.TryGetValue(requirement.ingredientKind, out ingredient))
            {
                total += ingredient.wartoscSprzedazy * requirement.quantity;
            }
            else
            {
                total += 4f * requirement.quantity;
            }

            if (requirement.requiredState != IngredientProcessState.Raw)
            {
                total += 1.5f * requirement.quantity;
            }
        }

        float rewardMultiplier = ShopManager.Instance != null ? ShopManager.Instance.GetRewardMultiplier() : 1f;
        return Mathf.Round(total * rewardMultiplier);
    }
}
