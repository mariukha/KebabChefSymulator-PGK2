/// \file SaveManager.cs
/// \brief Plik zawierający system zapisu i odczytu stanu gry Kebab Chef Symulator.
/// \details Definiuje klasę SaveManager odpowiedzialną za serializację i deserializację
/// stanu gry do/z pliku JSON. Obsługuje automatyczny zapis w określonych interwałach,
/// dwa oddzielne sloty zapisu (solo i online), oraz integrację z systemem sieciowym Unity Netcode.
/// Zawiera również klasy danych zapisu: GameSaveData, EconomySaveData, OrderProgressSaveData i OrderSaveData.

using System.IO;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Klasa zarządzająca systemem zapisu i odczytu stanu gry.
/// </summary>
/// <remarks>
/// Implementuje wzorzec Singleton. Odpowiada za:
/// - Zapis stanu gry do pliku JSON w katalogu persistentDataPath
/// - Odczyt stanu gry z pliku JSON
/// - Automatyczny zapis w konfigurowalnych interwałach czasowych
/// - Obsługę dwóch oddzielnych slotów zapisu (tryb solo i online)
/// - Integrację z systemem sieciowym (zapis/odczyt tylko na serwerze)
/// - Zapis przy wyjściu z aplikacji
///
/// Współpracuje z <see cref="EconomyManager"/>, <see cref="OrderManager"/>
/// oraz <see cref="ShopManager"/> w celu przechwycenia i przywrócenia ich stanów.
/// </remarks>
public class SaveManager : MonoBehaviour
{
    /// <summary>
    /// Statyczna instancja Singleton klasy <see cref="SaveManager"/>.
    /// </summary>
    /// <value>Jedyna instancja menedżera zapisu dostępna globalnie.</value>
    public static SaveManager Instance { get; private set; }

    /// <summary>
    /// Typ wyliczeniowy definiujący dostępne sloty zapisu.
    /// </summary>
    /// <remarks>
    /// Oddzielne sloty zapisu umożliwiają zachowanie niezależnego postępu
    /// dla trybu gry jednoosobowej i wieloosobowej.
    /// </remarks>
    private enum SaveSlot
    {
        /// <summary>
        /// Slot zapisu dla trybu gry jednoosobowej (solo/offline).
        /// </summary>
        Solo,

        /// <summary>
        /// Slot zapisu dla trybu gry wieloosobowej (online).
        /// </summary>
        Online
    }

    /// <summary>
    /// Interwał automatycznego zapisu w sekundach.
    /// </summary>
    /// <remarks>
    /// Konfigurowalny z poziomu inspektora Unity. Domyślnie 15 sekund.
    /// Automatyczny zapis wykonywany jest tylko gdy dane zostały oznaczone jako zmienione
    /// (flaga <see cref="isDirty"/>).
    /// </remarks>
    [SerializeField] private float autoSaveInterval = 15f;

    /// <summary>
    /// Nazwa pliku zapisu dla trybu gry jednoosobowej.
    /// </summary>
    private const string SoloSaveFileName = "kebab-save-solo.json";

    /// <summary>
    /// Nazwa pliku zapisu dla trybu gry wieloosobowej.
    /// </summary>
    private const string OnlineSaveFileName = "kebab-save-online.json";

    /// <summary>
    /// Licznik czasu odliczający do następnego automatycznego zapisu.
    /// </summary>
    private float autoSaveTimer;

    /// <summary>
    /// Flaga oznaczająca, czy stan gry został zmieniony od ostatniego zapisu.
    /// </summary>
    /// <remarks>
    /// Ustawiana na <c>true</c> przez <see cref="MarkDirty"/>.
    /// Resetowana po każdym automatycznym zapisie.
    /// Zapobiega zbędnym operacjom zapisu, gdy stan nie uległ zmianie.
    /// </remarks>
    private bool isDirty = false;

    /// <summary>
    /// Flaga oznaczająca, czy istnieje aktywna sesja gry.
    /// </summary>
    /// <remarks>
    /// Ustawiana na <c>true</c> po uruchomieniu serwera.
    /// Automatyczny zapis i zapis przy wyjściu wykonywane są tylko gdy sesja jest aktywna.
    /// </remarks>
    private bool hasActiveSession;

    /// <summary>
    /// Aktualnie używany slot zapisu.
    /// </summary>
    /// <remarks>
    /// Domyślnie ustawiony na <see cref="SaveSlot.Online"/>.
    /// Można zmienić za pomocą metody <see cref="UseSaveSlot"/>.
    /// </remarks>
    private SaveSlot currentSaveSlot = SaveSlot.Online;

    /// <summary>
    /// Pobiera nazwę pliku zapisu odpowiadającą aktualnemu slotowi.
    /// </summary>
    /// <value>Nazwa pliku: "kebab-save-solo.json" lub "kebab-save-online.json".</value>
    private string CurrentSaveFileName => currentSaveSlot == SaveSlot.Solo ? SoloSaveFileName : OnlineSaveFileName;

    /// <summary>
    /// Pobiera czytelną nazwę aktualnego slotu zapisu.
    /// </summary>
    /// <value>Ciąg tekstowy "solo" lub "online" do użycia w komunikatach logów.</value>
    private string CurrentSaveSlotName => currentSaveSlot == SaveSlot.Solo ? "solo" : "online";

    /// <summary>
    /// Pobiera pełną ścieżkę do pliku zapisu.
    /// </summary>
    /// <value>
    /// Ścieżka złożona z <see cref="Application.persistentDataPath"/> oraz nazwy pliku zapisu
    /// odpowiadającej aktualnemu slotowi.
    /// </value>
    public string SavePath => Path.Combine(Application.persistentDataPath, CurrentSaveFileName);

    /// <summary>
    /// Ustawia slot zapisu na podstawie trybu gry.
    /// </summary>
    /// <param name="isSolo">
    /// <c>true</c> aby wybrać slot solo (gra jednoosobowa);
    /// <c>false</c> aby wybrać slot online (gra wieloosobowa).
    /// </param>
    public void UseSaveSlot(bool isSolo)
    {
        currentSaveSlot = isSolo ? SaveSlot.Solo : SaveSlot.Online;
    }

    /// <summary>
    /// Oznacza zakończenie aktywnej sesji gry.
    /// </summary>
    /// <remarks>
    /// Resetuje flagę aktywnej sesji, flagę zmian oraz timer automatycznego zapisu.
    /// Po wywołaniu tej metody automatyczny zapis nie będzie wykonywany
    /// do momentu rozpoczęcia nowej sesji.
    /// </remarks>
    public void MarkSessionEnded()
    {
        hasActiveSession = false;
        isDirty = false;
        autoSaveTimer = 0f;
    }

    /// <summary>
    /// Oznacza stan gry jako zmieniony, wymagający zapisu.
    /// </summary>
    /// <remarks>
    /// Wywoływana przez inne systemy (np. <see cref="EconomyManager"/>) po dokonaniu zmian
    /// w stanie gry. Automatyczny zapis wykona się przy następnym upływie interwału
    /// <see cref="autoSaveInterval"/>.
    /// </remarks>
    public void MarkDirty()
    {
        isDirty = true;
    }

    /// <summary>
    /// Inicjalizuje instancję Singleton przy starcie obiektu.
    /// </summary>
    /// <remarks>
    /// Jeśli instancja nie istnieje, ustawia ją na bieżący obiekt.
    /// Jeśli instancja już istnieje, niszczy duplikat obiektu.
    /// </remarks>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Wykonuje inicjalizację po uruchomieniu komponentu.
    /// </summary>
    /// <remarks>
    /// Subskrybuje zdarzenie uruchomienia serwera w <see cref="NetworkManager"/>.
    /// Jeśli serwer jest już uruchomiony w momencie startu, natychmiast rozpoczyna
    /// sesję i wczytuje zapisany stan gry.
    /// </remarks>
    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            if (NetworkManager.Singleton.IsServer)
            {
                hasActiveSession = true;
                LoadGame();
            }
        }
    }

    /// <summary>
    /// Obsługuje zdarzenie uruchomienia serwera sieciowego.
    /// </summary>
    /// <remarks>
    /// Ustawia flagę aktywnej sesji i wczytuje zapisany stan gry z pliku.
    /// </remarks>
    private void OnServerStarted()
    {
        hasActiveSession = true;
        LoadGame();
    }

    /// <summary>
    /// Obsługuje logikę automatycznego zapisu w każdej klatce.
    /// </summary>
    /// <remarks>
    /// Wykonuje automatyczny zapis po upływie <see cref="autoSaveInterval"/> sekund,
    /// ale tylko jeśli:
    /// - Istnieje aktywna sesja (<see cref="hasActiveSession"/> = true)
    /// - Dane zostały zmienione (<see cref="isDirty"/> = true)
    /// 
    /// Po zapisie resetuje flagę zmian i timer.
    /// </remarks>
    private void Update()
    {
        if (!hasActiveSession)
        {
            return;
        }

        autoSaveTimer += Time.deltaTime;
        if (autoSaveTimer >= autoSaveInterval)
        {
            if (isDirty)
            {
                SaveGame();
                isDirty = false;
            }
            autoSaveTimer = 0f;
        }
    }

    /// <summary>
    /// Zapisuje bieżący stan gry do pliku JSON.
    /// </summary>
    /// <remarks>
    /// Operacja zapisu jest wykonywana tylko na serwerze w trybie sieciowym.
    /// Klienci nie mogą zapisywać stanu gry bezpośrednio.
    /// 
    /// Przechwytuje stany z następujących systemów (jeśli dostępne):
    /// - <see cref="EconomyManager"/> → dane ekonomii
    /// - <see cref="OrderManager"/> → postęp zamówień
    /// - <see cref="ShopManager"/> → poziomy ulepszeń
    /// 
    /// Dane są serializowane do formatu JSON z formatowaniem (pretty print)
    /// i zapisywane do pliku w <see cref="SavePath"/>.
    /// </remarks>
    /// <exception cref="System.Exception">
    /// Błędy zapisu do pliku są przechwytywane i logowane jako błąd,
    /// ale nie przerywają działania aplikacji.
    /// </exception>
    public void SaveGame()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        try
        {
            GameSaveData data = new GameSaveData();
            string path = SavePath;

            if (EconomyManager.Instance != null)
            {
                data.economy = EconomyManager.Instance.CaptureState();
            }

            if (OrderManager.Instance != null)
            {
                data.orderProgress = OrderManager.Instance.CaptureProgress();
            }

            if (ShopManager.Instance != null)
            {
                data.shop = ShopManager.Instance.CaptureState();
            }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
            Debug.Log("Stan gry zapisany (" + CurrentSaveSlotName + "): " + path);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[SaveManager] Blad zapisu: " + e.Message);
        }
    }

    /// <summary>
    /// Wczytuje zapisany stan gry z pliku JSON.
    /// </summary>
    /// <remarks>
    /// Operacja odczytu jest wykonywana tylko na serwerze w trybie sieciowym.
    /// 
    /// Proces wczytywania:
    /// 1. Sprawdza istnienie pliku zapisu
    /// 2. Deserializuje dane JSON do obiektu <see cref="GameSaveData"/>
    /// 3. Waliduje wartości ekonomii (zabezpiecza przed ujemnymi wartościami)
    /// 4. Przywraca stany do odpowiednich menedżerów:
    ///    - <see cref="EconomyManager.RestoreState"/>
    ///    - <see cref="OrderManager.RestoreProgress"/>
    ///    - <see cref="ShopManager.RestoreState"/>
    /// 
    /// Jeśli plik nie istnieje, loguje informację i rozpoczyna nową sesję bez wczytywania.
    /// </remarks>
    /// <exception cref="System.Exception">
    /// Błędy odczytu pliku lub deserializacji JSON są przechwytywane i logowane jako błąd,
    /// ale nie przerywają działania aplikacji.
    /// </exception>
    public void LoadGame()
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        string path = SavePath;
        if (!File.Exists(path))
        {
            Debug.Log("Brak pliku zapisu (" + CurrentSaveSlotName + "). Start nowej sesji.");
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            if (data == null)
            {
                Debug.LogWarning("Nie udalo sie odczytac pliku zapisu.");
                return;
            }

            if (data.economy != null)
            {
                data.economy.currentBalance = Mathf.Max(0f, data.economy.currentBalance);
                data.economy.totalEarned = Mathf.Max(0f, data.economy.totalEarned);
                data.economy.totalSpent = Mathf.Max(0f, data.economy.totalSpent);
            }

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.RestoreState(data.economy);
            }

            if (OrderManager.Instance != null)
            {
                OrderManager.Instance.RestoreProgress(data.orderProgress);
            }

            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.RestoreState(data.shop);
            }

            Debug.Log("Stan gry wczytany (" + CurrentSaveSlotName + "): " + path);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[SaveManager] Blad wczytywania: " + e.Message);
        }
    }

    /// <summary>
    /// Obsługuje zdarzenie zamknięcia aplikacji.
    /// </summary>
    /// <remarks>
    /// Automatycznie zapisuje stan gry jeśli istnieje aktywna sesja,
    /// zapewniając zachowanie postępu gracza przy wyjściu z gry.
    /// </remarks>
    private void OnApplicationQuit()
    {
        if (hasActiveSession)
        {
            SaveGame();
        }
    }

    /// <summary>
    /// Czyści referencje i odsubskrybowuje zdarzenia przy niszczeniu obiektu.
    /// </summary>
    /// <remarks>
    /// Odsubskrybowuje zdarzenie <c>OnServerStarted</c> z <see cref="NetworkManager"/>
    /// i czyści referencję Singletona, zapobiegając wyciekom pamięci i nieaktualnym referencjom.
    /// </remarks>
    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }
}

/// <summary>
/// Główna klasa danych zapisu gry, agregująca stany wszystkich systemów.
/// </summary>
/// <remarks>
/// Serializowana struktura przechowująca kompletny stan gry w formacie JSON.
/// Zawiera numer wersji dla potencjalnej kompatybilności wstecznej
/// oraz zagnieżdżone obiekty danych dla każdego podsystemu gry.
/// </remarks>
[System.Serializable]
public class GameSaveData
{
    /// <summary>
    /// Numer wersji formatu zapisu, używany do zarządzania kompatybilnością wsteczną.
    /// </summary>
    /// <remarks>
    /// Aktualna wersja: 1. Może być inkrementowana przy zmianach struktury danych zapisu.
    /// </remarks>
    public int version = 1;

    /// <summary>
    /// Dane stanu systemu ekonomii (saldo, zarobki, wydatki).
    /// </summary>
    /// <seealso cref="EconomyManager.CaptureState"/>
    public EconomySaveData economy = new EconomySaveData();

    /// <summary>
    /// Dane postępu zamówień (ukończone, nieudane, aktywne zamówienie).
    /// </summary>
    /// <seealso cref="OrderManager"/>
    public OrderProgressSaveData orderProgress = new OrderProgressSaveData();

    /// <summary>
    /// Dane stanu sklepu z ulepszeniami (poziomy ulepszeń).
    /// </summary>
    /// <seealso cref="ShopManager.CaptureState"/>
    public ShopSaveData shop = new ShopSaveData();
}

/// <summary>
/// Klasa danych zapisu stanu ekonomii gracza.
/// </summary>
/// <remarks>
/// Przechowuje informacje finansowe gracza: aktualne saldo,
/// łączne zarobki i łączne wydatki od początku gry.
/// </remarks>
[System.Serializable]
public class EconomySaveData
{
    /// <summary>
    /// Aktualne saldo gracza w momencie zapisu.
    /// </summary>
    /// <remarks>
    /// Domyślna wartość 100 odpowiada początkowej kwocie nowej gry.
    /// Przy wczytywaniu wartość jest zabezpieczona przed wartościami ujemnymi.
    /// </remarks>
    public float currentBalance = 100f;

    /// <summary>
    /// Łączna kwota pieniędzy zarobionych przez gracza od początku gry.
    /// </summary>
    public float totalEarned;

    /// <summary>
    /// Łączna kwota pieniędzy wydanych przez gracza od początku gry.
    /// </summary>
    public float totalSpent;
}

/// <summary>
/// Klasa danych zapisu postępu zamówień.
/// </summary>
/// <remarks>
/// Przechowuje statystyki zamówień gracza oraz informacje o aktywnym zamówieniu,
/// umożliwiając wznowienie gry od miejsca, w którym została przerwana.
/// </remarks>
[System.Serializable]
public class OrderProgressSaveData
{
    /// <summary>
    /// Łączna liczba pomyślnie ukończonych zamówień.
    /// </summary>
    public int completedOrders;

    /// <summary>
    /// Łączna liczba nieudanych (przeterminowanych) zamówień.
    /// </summary>
    public int failedOrders;

    /// <summary>
    /// Pozostały czas na realizację aktywnego zamówienia (w sekundach).
    /// </summary>
    public float remainingOrderTime;

    /// <summary>
    /// Ostatni komunikat zamówienia wyświetlony graczowi.
    /// </summary>
    public string lastOrderMessage = string.Empty;

    /// <summary>
    /// Dane aktywnego zamówienia w momencie zapisu.
    /// </summary>
    /// <remarks>
    /// Może być <c>null</c> jeśli w momencie zapisu nie było aktywnego zamówienia.
    /// </remarks>
    public OrderSaveData activeOrder;
}

/// <summary>
/// Klasa danych zapisu pojedynczego zamówienia.
/// </summary>
/// <remarks>
/// Przechowuje wszystkie informacje potrzebne do odtworzenia aktywnego zamówienia
/// po wczytaniu stanu gry, w tym identyfikator, dane klienta, parametry zamówienia
/// oraz listę wymaganych składników.
/// </remarks>
[System.Serializable]
public class OrderSaveData
{
    /// <summary>
    /// Unikalny identyfikator zamówienia.
    /// </summary>
    public string orderId;

    /// <summary>
    /// Nazwa klienta składającego zamówienie.
    /// </summary>
    public string clientName;

    /// <summary>
    /// Nazwa zamówionego dania (np. rodzaj kebaba).
    /// </summary>
    public string orderName;

    /// <summary>
    /// Limit czasu na realizację zamówienia w sekundach.
    /// </summary>
    public float timeLimit;

    /// <summary>
    /// Nagroda pieniężna za ukończenie zamówienia.
    /// </summary>
    public float reward;

    /// <summary>
    /// Lista wymaganych składników zamówienia.
    /// </summary>
    /// <remarks>
    /// Każdy element listy zawiera informacje o wymaganym składniku i jego ilości.
    /// Lista jest inicjalizowana jako pusta, aby uniknąć błędów null reference przy serializacji.
    /// </remarks>
    public System.Collections.Generic.List<IngredientRequirement> requirements =
        new System.Collections.Generic.List<IngredientRequirement>();
}
