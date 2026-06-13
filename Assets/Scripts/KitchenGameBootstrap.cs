/// \file KitchenGameBootstrap.cs
/// \brief Główny plik bootstrapowy gry kuchennej — odpowiada za inicjalizację i budowanie całego środowiska kuchni w czasie wykonania.
/// \details Zawiera klasę KitchenGameBootstrap, która jest punktem wejścia do procedurowego generowania
/// sceny kuchni kebabowej. Plik obejmuje również klasy pomocnicze: BillboardLabel do etykiet śledzących kamerę,
/// DeliveryTrayDisplay do wyświetlania serwowanego kebaba na tacy oraz CustomerAnimator do animacji klienta.
/// Klasy te wspólnie odpowiadają za tworzenie otoczenia 3D, stacji kuchennych, oświetlenia,
/// efektów wizualnych oraz konfigurację sieciową i menedżerów gry.

using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.Linq;

/// <summary>
/// Główna klasa bootstrapowa gry kuchennej.
/// Odpowiada za proceduralne generowanie całej sceny kuchni kebabowej w czasie wykonania,
/// włączając w to środowisko (podłoga, ściany, sufit), stacje kuchenne, oświetlenie,
/// modele 3D, konfigurację sieciową oraz inicjalizację wszystkich menedżerów gry.
/// </summary>
/// <remarks>
/// Klasa automatycznie uruchamia się po załadowaniu sceny dzięki atrybutowi
/// <see cref="RuntimeInitializeOnLoadMethodAttribute"/>. Jeśli instancja bootstrapa
/// nie istnieje w scenie, zostanie utworzona automatycznie. Wszystkie elementy kuchni
/// są budowane proceduralnie z prymitywów oraz importowanych modeli 3D.
/// </remarks>
public class KitchenGameBootstrap : MonoBehaviour
{
    /// <summary>
    /// Numer warstwy (layer) przypisywany obiektom interaktywnym w kuchni.
    /// Służy do filtrowania raycastów przy interakcji gracza ze stacjami.
    /// </summary>
    private const int InteractableLayer = 6;

    /// <summary>
    /// Licznik indeksów stacji kuchennych, inkrementowany przy tworzeniu każdej nowej stacji.
    /// Zapewnia unikalny identyfikator sieciowy dla każdej stacji.
    /// </summary>
    private int stationIndexCounter = 0;

    /// <summary>
    /// Ścieżka do katalogu z modelami 3D w folderze Resources.
    /// Używana przy ładowaniu prefabrykatów modeli za pomocą <see cref="Resources.Load"/>.
    /// </summary>
    private const string ModelPath = "Models/";

    /// <summary>
    /// Buforowany shader Lit, aby uniknąć wielokrotnego wyszukiwania go w każdym wywołaniu.
    /// </summary>
    private Shader cachedLitShader;

    /// <summary>
    /// Wysokość oczu gracza nad podłogą w metrach.
    /// Używana do pozycjonowania kamery i punktów patrzenia.
    /// </summary>
    private const float PlayerEyeHeight = 1.75f;

    /// <summary>
    /// Lokalna pozycja Y blatu roboczego stacji kuchennych.
    /// Określa wysokość, na której umieszczane są elementy wizualne na stacjach.
    /// </summary>
    private const float WorktopLocalY = 0.34f;

    /// <summary>
    /// Bazowy rozmiar wizualny modeli stołów przygotowawczych.
    /// Stosowany jako parametr targetMaxSize przy skalowaniu modeli stołów.
    /// </summary>
    private const float TableVisualSize = 2.35f;

    /// <summary>
    /// Mnożnik skali głębokości stołów przygotowawczych.
    /// Stosowany do rozciągnięcia modelu stołu wzdłuż osi Z.
    /// </summary>
    private const float TableDepthScale = 1.6f;

    /// <summary>
    /// Pozycja X stołu przy wejściu (stół z kasą fiskalną i tacą do wydawania).
    /// </summary>
    private const float EntranceTableX = 2.75f;

    /// <summary>
    /// Pozycja Z stołu przy wejściu (stół z kasą fiskalną i tacą do wydawania).
    /// </summary>
    private const float EntranceTableZ = -4.58f;

    /// <summary>
    /// Wysokość Y blatu stołu przy wejściu.
    /// Określa pozycję pionową, na której umieszczane są obiekty na stole wejściowym.
    /// </summary>
    private const float EntranceTableTopY = 1.20f;

    /// <summary>
    /// Domyślna pozycja spawnu gracza na scenie.
    /// </summary>
    private static readonly Vector3 PlayerSpawnPosition = new Vector3(0f, 0f, -1.9f);

    /// <summary>
    /// Pozycja klienta oczekującego na zamówienie.
    /// </summary>
    private static readonly Vector3 CustomerPosition = new Vector3(0f, 0f, -4.8f);

    /// <summary>
    /// Punkt, na który klient patrzy — używany do orientacji modelu klienta.
    /// </summary>
    private static readonly Vector3 CustomerLookTarget = new Vector3(0f, 1.55f, -4.8f);

    /// <summary>
    /// Statyczna metoda wywoływana automatycznie po załadowaniu sceny.
    /// Tworzy obiekt bootstrapowy, jeśli jeszcze nie istnieje na scenie.
    /// </summary>
    /// <remarks>
    /// Oznaczona atrybutem <see cref="RuntimeInitializeOnLoadMethodAttribute"/> z trybem
    /// <see cref="RuntimeInitializeLoadType.AfterSceneLoad"/>, co zapewnia wywołanie
    /// po pełnym załadowaniu sceny. Zapobiega duplikacji sprawdzając obecność istniejącej instancji.
    /// </remarks>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapScene()
    {
        if (FindFirstObjectByType<KitchenGameBootstrap>() != null)
        {
            return;
        }

        GameObject bootstrapper = new GameObject("KitchenBootstrap");
        bootstrapper.AddComponent<KitchenGameBootstrap>();
    }

    /// <summary>
    /// Pobiera buforowany shader URP Lit, a w razie jego braku — shader Standard.
    /// </summary>
    /// <returns>Shader Lit z Universal Render Pipeline lub Standard jako fallback.</returns>
    /// <remarks>
    /// Wynik jest buforowany w polu <see cref="cachedLitShader"/>, aby uniknąć
    /// kosztownego wyszukiwania shadera przy każdym wywołaniu.
    /// </remarks>
    private Shader GetLitShader()
    {
        if (cachedLitShader != null) return cachedLitShader;
        cachedLitShader = Shader.Find("Universal Render Pipeline/Lit");
        if (cachedLitShader == null) cachedLitShader = Shader.Find("Standard");
        return cachedLitShader;
    }

    /// <summary>
    /// Główna metoda inicjalizacyjna wywoływana przez Unity w momencie startu komponentu.
    /// Sekwencyjnie uruchamia wszystkie etapy budowania sceny kuchni.
    /// </summary>
    /// <remarks>
    /// Każdy etap inicjalizacji jest otoczony blokiem try-catch, aby awaria jednego
    /// podsystemu nie blokowała inicjalizacji pozostałych. W przypadku braku aktywnej
    /// sesji sieciowej, tworzy również system zdarzeń UI i menu główne.
    /// Kolejność inicjalizacji:
    /// 1. Konfiguracja sieci (NetworkSetup)
    /// 2. Menedżerowie gry (ekonomia, zamówienia, zapis, ustawienia, sklep, VFX, relay)
    /// 3. Budowa środowiska 3D (podłoga, ściany, sufit, stoły)
    /// 4. Budowa stacji kuchennych (grill, składniki, deska, montaż, wydanie)
    /// 5. Tablica zamówień
    /// 6. Konfiguracja oświetlenia
    /// 7. Efekty wizualne i dodatkowe systemy UI
    /// </remarks>
    private void Start()
    {
        try { EnsureNetworkSetup(); } catch (System.Exception e) { Debug.LogError($"[Bootstrap] NetworkSetup failed: {e}"); }
        try { EnsureManagers(); } catch (System.Exception e) { Debug.LogError($"[Bootstrap] Managers failed: {e}"); }
        try { BuildEnvironmentIfNeeded(); } catch (System.Exception e) { Debug.LogError($"[Bootstrap] Environment failed: {e}"); }
        try { BuildKitchenIfNeeded(); } catch (System.Exception e) { Debug.LogError($"[Bootstrap] Kitchen failed: {e}"); }
        try { BuildOrderBoardIfNeeded(); } catch (System.Exception e) { Debug.LogError($"[Bootstrap] OrderBoard failed: {e}"); }
        try { ConfigureLighting(); } catch (System.Exception e) { Debug.LogError($"[Bootstrap] Lighting failed: {e}"); }
        try { EnsureEffects(); } catch (System.Exception e) { Debug.LogError($"[Bootstrap] Effects failed: {e}"); }

        try
        {
            bool networkActive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            if (!networkActive)
            {

                if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    GameObject esObj = new GameObject("EventSystem");
                    esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }

                if (FindFirstObjectByType<MainMenuUI>() == null)
                {
                    new GameObject("MainMenuUI").AddComponent<MainMenuUI>();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Bootstrap] Post-init UI failed: {e}");
        }
    }

    /// <summary>
    /// Zapewnia istnienie obiektu konfiguracji sieciowej na scenie.
    /// Tworzy komponent <see cref="NetworkSetup"/> oraz <see cref="LobbyUI"/>, jeśli nie istnieją.
    /// </summary>
    private void EnsureNetworkSetup()
    {
        if (FindFirstObjectByType<NetworkSetup>() != null)
        {
            return;
        }

        GameObject networkObject = new GameObject("NetworkSetup");
        networkObject.AddComponent<NetworkSetup>();

        if (FindFirstObjectByType<LobbyUI>() == null)
        {
            GameObject lobbyObject = new GameObject("LobbyUI");
            lobbyObject.AddComponent<LobbyUI>();
        }
    }

    /// <summary>
    /// Zapewnia istnienie wszystkich menedżerów gry na scenie.
    /// Tworzy obiekt "GameManager" i dołącza do niego brakujące komponenty menedżerów.
    /// </summary>
    /// <remarks>
    /// Menedżerowie inicjalizowani przez tę metodę:
    /// <list type="bullet">
    /// <item><description><see cref="EconomyManager"/> — system ekonomii i salda gracza</description></item>
    /// <item><description><see cref="OrderManager"/> — system zamówień i katalogu składników</description></item>
    /// <item><description><see cref="SaveManager"/> — system zapisu i wczytywania stanu gry</description></item>
    /// <item><description><see cref="GameSettingsManager"/> — zarządzanie ustawieniami gry</description></item>
    /// <item><description><see cref="ShopManager"/> — system sklepu z ulepszeniami</description></item>
    /// <item><description><see cref="VFXManager"/> — zarządzanie efektami wizualnymi</description></item>
    /// <item><description><see cref="RelayManager"/> — menedżer połączeń relay dla gry wieloosobowej</description></item>
    /// </list>
    /// Na końcu wywołuje inicjalizację katalogu zamówień.
    /// </remarks>
    private void EnsureManagers()
    {
        GameObject managerObject = GameObject.Find("GameManager");
        if (managerObject == null)
        {
            managerObject = new GameObject("GameManager");
        }

        if (FindFirstObjectByType<EconomyManager>() == null)
        {
            managerObject.AddComponent<EconomyManager>();
        }

        OrderManager orderManager = FindFirstObjectByType<OrderManager>();
        if (orderManager == null)
        {
            orderManager = managerObject.AddComponent<OrderManager>();
        }

        if (FindFirstObjectByType<SaveManager>() == null)
        {
            managerObject.AddComponent<SaveManager>();
        }

        if (FindFirstObjectByType<GameSettingsManager>() == null)
        {
            managerObject.AddComponent<GameSettingsManager>();
        }

        if (FindFirstObjectByType<ShopManager>() == null)
        {
            managerObject.AddComponent<ShopManager>();
        }

        if (FindFirstObjectByType<VFXManager>() == null)
        {
            managerObject.AddComponent<VFXManager>();
        }

        if (FindFirstObjectByType<RelayManager>() == null)
        {
            managerObject.AddComponent<RelayManager>();
        }

        orderManager.InitializeCatalogIfNeeded();
    }

    /// <summary>
    /// Zapewnia istnienie efektów wizualnych, dźwiękowych i dodatkowych komponentów UI na scenie.
    /// </summary>
    /// <remarks>
    /// Tworzy następujące komponenty, jeśli jeszcze nie istnieją:
    /// <list type="bullet">
    /// <item><description><see cref="PostProcessSetup"/> — konfiguracja post-processingu</description></item>
    /// <item><description><see cref="AmbientParticles"/> — cząsteczki otoczenia (pył, para)</description></item>
    /// <item><description><see cref="AudioManager"/> — zarządzanie dźwiękiem i muzyką</description></item>
    /// <item><description><see cref="ItemAnimator"/> — animacja przedmiotów w kuchni</description></item>
    /// <item><description><see cref="PauseMenuUI"/> — interfejs menu pauzy</description></item>
    /// <item><description><see cref="LoadingScreen"/> — ekran ładowania</description></item>
    /// <item><description><see cref="AchievementPopup"/> — wyskakujące powiadomienia o osiągnięciach</description></item>
    /// </list>
    /// </remarks>
    private void EnsureEffects()
    {
        GameObject managerObject = GameObject.Find("GameManager");
        if (managerObject == null)
        {
            managerObject = new GameObject("GameManager");
        }

        if (FindFirstObjectByType<PostProcessSetup>() == null)
        {
            managerObject.AddComponent<PostProcessSetup>();
        }

        if (FindFirstObjectByType<AmbientParticles>() == null)
        {
            managerObject.AddComponent<AmbientParticles>();
        }

        if (FindFirstObjectByType<AudioManager>() == null)
        {
            managerObject.AddComponent<AudioManager>();
        }

        if (FindFirstObjectByType<ItemAnimator>() == null)
        {
            managerObject.AddComponent<ItemAnimator>();
        }

        if (FindFirstObjectByType<PauseMenuUI>() == null)
        {
            new GameObject("PauseMenuUI").AddComponent<PauseMenuUI>();
        }

        if (FindFirstObjectByType<LoadingScreen>() == null)
        {
            new GameObject("LoadingScreen").AddComponent<LoadingScreen>();
        }

        if (FindFirstObjectByType<AchievementPopup>() == null)
        {
            new GameObject("AchievementPopup").AddComponent<AchievementPopup>();
        }
    }

    /// <summary>
    /// Buduje wszystkie stacje kuchenne, jeśli jeszcze nie istnieją na scenie.
    /// Tworzy pełne wyposażenie kuchni kebabowej: grill, pojemniki na składniki,
    /// deskę do krojenia, stanowisko montażu oraz punkt wydawania.
    /// </summary>
    /// <remarks>
    /// Stacje kuchenne tworzone w kolejności:
    /// <list type="bullet">
    /// <item><description>Grill — grillowanie mięsa (z maszyną döner)</description></item>
    /// <item><description>Mięso — źródło składnika mięsnego (powiązane z grillem)</description></item>
    /// <item><description>Pomidor — źródło pomidorów</description></item>
    /// <item><description>Cebula — źródło cebuli</description></item>
    /// <item><description>Sałata — źródło sałaty</description></item>
    /// <item><description>Sos — źródło sosu czosnkowego</description></item>
    /// <item><description>Ławasz — źródło chlebka ławasz</description></item>
    /// <item><description>Deska — stacja do krojenia składników</description></item>
    /// <item><description>Zwijanie — stanowisko montażu/zawijania kebaba</description></item>
    /// <item><description>Wydanie — punkt wydawania gotowego zamówienia klientowi</description></item>
    /// </list>
    /// Po utworzeniu stacji, grill jest łączony z tacą na mięso, a na końcu
    /// tworzony jest model klienta przy okienku.
    /// </remarks>
    private void BuildKitchenIfNeeded()
    {
        if (FindFirstObjectByType<KitchenStation>() != null)
        {
            return;
        }

        OrderManager orderManager = FindFirstObjectByType<OrderManager>();
        if (orderManager == null)
        {
            return;
        }

        Transform parent = new GameObject("RuntimeKitchen").transform;

        KitchenStation grillStation = CreateStation(parent, "Grill", KitchenStationType.Grill, new Vector3(-5.55f, 0.6f, 5.25f), new Color(0.3f, 0.3f, 0.35f), null, 4f);
        KitchenStation meatTrayStation = CreateStation(parent, "Mieso", KitchenStationType.IngredientSource, new Vector3(-5.55f, 0.6f, 3.95f), new Color(0.65f, 0.25f, 0.18f), orderManager.GetIngredientDefinition(IngredientKind.Meat), 0f);

        CreateStation(parent, "Pomidor", KitchenStationType.IngredientSource, new Vector3(-5.55f, 0.6f, 2.15f), new Color(0.86f, 0.2f, 0.2f), orderManager.GetIngredientDefinition(IngredientKind.Tomato), 0f);
        CreateStation(parent, "Cebula", KitchenStationType.IngredientSource, new Vector3(-5.55f, 0.6f, 0.85f), new Color(0.93f, 0.9f, 0.75f), orderManager.GetIngredientDefinition(IngredientKind.Onion), 0f);
        CreateStation(parent, "Salata", KitchenStationType.IngredientSource, new Vector3(-5.55f, 0.6f, -0.45f), new Color(0.35f, 0.7f, 0.25f), orderManager.GetIngredientDefinition(IngredientKind.Lettuce), 0f);

        CreateStation(parent, "Sos", KitchenStationType.IngredientSource, new Vector3(-5.55f, 0.6f, -1.75f), new Color(0.95f, 0.95f, 0.85f), orderManager.GetIngredientDefinition(IngredientKind.GarlicSauce), 0f);
        CreateStation(parent, "Lawasz", KitchenStationType.IngredientSource, new Vector3(-5.55f, 0.6f, -3.05f), new Color(0.86f, 0.74f, 0.5f), orderManager.GetIngredientDefinition(IngredientKind.Lavash), 0f);

        CreateStation(parent, "Deska", KitchenStationType.CuttingBoard, new Vector3(-5.55f, 0.6f, -4.35f), new Color(0.73f, 0.56f, 0.32f), null, 2.5f);
        CreateStation(parent, "Zwijanie", KitchenStationType.Assembly, new Vector3(-3.85f, 0.6f, 5.45f), new Color(0.65f, 0.5f, 0.28f), null, 0f);
        CreateStation(parent, "Wydanie", KitchenStationType.Delivery, new Vector3(EntranceTableX + 0.45f, 0.6f, EntranceTableZ + 0.25f), new Color(0.2f, 0.55f, 0.8f), null, 0f);

        if (grillStation != null && meatTrayStation != null)
        {
            grillStation.SetLinkedMeatTray(meatTrayStation);
            meatTrayStation.RefreshVisualState();
            grillStation.RefreshVisualState();
        }

        CreateCustomer(parent, CustomerPosition);
    }

    /// <summary>
    /// Buduje środowisko 3D kuchni, jeśli jeszcze nie istnieje na scenie.
    /// Tworzy podłogę, ściany, sufit, panele ścienne, listwy sufitowe,
    /// blaty robocze oraz importowane detale wizualne (modele 3D).
    /// </summary>
    /// <remarks>
    /// Metoda sprawdza obecność obiektu "RuntimeEnvironment" — jeśli istnieje,
    /// budowa środowiska jest pomijana. Środowisko składa się z:
    /// <list type="bullet">
    /// <item><description>Podłoga bazowa i wkładka podłogi kuchennej</description></item>
    /// <item><description>Sufit</description></item>
    /// <item><description>Cztery ściany (tylna, lewa, prawa, przednia)</description></item>
    /// <item><description>Panele ścienne dekoracyjne</description></item>
    /// <item><description>Listwy sufitowe</description></item>
    /// <item><description>Blaty/stoły robocze wzdłuż lewej ściany i tylnej ściany</description></item>
    /// <item><description>Narożne blokery kolizji (prawy górny róg)</description></item>
    /// <item><description>Stół użytkowy przy wejściu</description></item>
    /// <item><description>Importowane detale wizualne (lampy, półki, modele stołów, kasa fiskalna itp.)</description></item>
    /// </list>
    /// </remarks>
    private void BuildEnvironmentIfNeeded()
    {
        if (GameObject.Find("RuntimeEnvironment") != null)
        {
            return;
        }

        Transform environmentRoot = new GameObject("RuntimeEnvironment").transform;

        CreateBlock(environmentRoot, "FloorBase", PrimitiveType.Cube, new Vector3(0f, -0.55f, 0f), new Vector3(14f, 1f, 14f), new Color(0.17f, 0.18f, 0.19f));
        CreateBlock(environmentRoot, "KitchenFloorInset", PrimitiveType.Cube, new Vector3(0f, -0.035f, 0f), new Vector3(12.6f, 0.03f, 12.2f), new Color(0.28f, 0.29f, 0.3f));
        CreateBlock(environmentRoot, "Ceiling", PrimitiveType.Cube, new Vector3(0f, 4.95f, 0f), new Vector3(13.6f, 0.18f, 13.6f), new Color(0.9f, 0.9f, 0.88f));

        CreateBlock(environmentRoot, "BackWall", PrimitiveType.Cube, new Vector3(0f, 2.5f, 6.8f), new Vector3(14f, 5f, 0.35f), new Color(0.84f, 0.83f, 0.8f));
        CreateBlock(environmentRoot, "LeftWall", PrimitiveType.Cube, new Vector3(-6.8f, 2.5f, 0f), new Vector3(0.35f, 5f, 14f), new Color(0.8f, 0.78f, 0.75f));
        CreateBlock(environmentRoot, "RightWall", PrimitiveType.Cube, new Vector3(6.8f, 2.5f, 0f), new Vector3(0.35f, 5f, 14f), new Color(0.8f, 0.78f, 0.75f));
        CreateBlock(environmentRoot, "FrontWall", PrimitiveType.Cube, new Vector3(0f, 2.5f, -5.65f), new Vector3(14f, 5f, 0.35f), new Color(0.82f, 0.8f, 0.77f));

        CreateBlock(environmentRoot, "BackWallPanel", PrimitiveType.Cube, new Vector3(0f, 1.4f, 6.6f), new Vector3(12.8f, 2.2f, 0.05f), new Color(0.73f, 0.74f, 0.76f));
        CreateBlock(environmentRoot, "LeftWallPanel", PrimitiveType.Cube, new Vector3(-6.6f, 1.4f, 0f), new Vector3(0.05f, 2.2f, 12.8f), new Color(0.72f, 0.73f, 0.75f));
        CreateBlock(environmentRoot, "RightWallPanel", PrimitiveType.Cube, new Vector3(6.6f, 1.4f, 0f), new Vector3(0.05f, 2.2f, 12.8f), new Color(0.72f, 0.73f, 0.75f));

        CreateBlock(environmentRoot, "CeilingTrimBack", PrimitiveType.Cube, new Vector3(0f, 4.77f, 6.52f), new Vector3(13f, 0.08f, 0.12f), new Color(0.62f, 0.61f, 0.58f));
        CreateBlock(environmentRoot, "CeilingTrimLeft", PrimitiveType.Cube, new Vector3(-6.52f, 4.77f, 0f), new Vector3(0.12f, 0.08f, 13f), new Color(0.62f, 0.61f, 0.58f));
        CreateBlock(environmentRoot, "CeilingTrimRight", PrimitiveType.Cube, new Vector3(6.52f, 4.77f, 0f), new Vector3(0.12f, 0.08f, 13f), new Color(0.62f, 0.61f, 0.58f));
        CreateCounter(environmentRoot, "LeftTableA", new Vector3(-5.55f, 0.25f, 4.6f), new Vector3(1.6f, 0.5f, 2.25f), false);
        CreateCounter(environmentRoot, "LeftTableB", new Vector3(-5.55f, 0.25f, 1.5f), new Vector3(1.6f, 0.5f, 2.25f), false);
        CreateCounter(environmentRoot, "LeftTableC", new Vector3(-5.55f, 0.25f, -1.1f), new Vector3(1.6f, 0.5f, 2.25f), false);
        CreateCounter(environmentRoot, "LeftTableD", new Vector3(-5.55f, 0.25f, -3.7f), new Vector3(1.6f, 0.5f, 2.25f), false);
        CreateCounter(environmentRoot, "BackTableA", new Vector3(-3.05f, 0.25f, 5.45f), new Vector3(2.25f, 0.5f, 1.6f), false);
        CreateCornerCounterBlockers(environmentRoot);
        CreateCounter(environmentRoot, "EntranceUtilityTableBlocker", new Vector3(EntranceTableX, 0.25f, EntranceTableZ), new Vector3(2.25f, 0.5f, 1.15f), false);

        CreateImportedEnvironmentDetails(environmentRoot);
    }

    /// <summary>
    /// Buduje tablicę zamówień kuchennych, jeśli jeszcze nie istnieje na scenie.
    /// Tablica jest umieszczana na tylnej ścianie kuchni i wyświetla aktualne zamówienia.
    /// </summary>
    private void BuildOrderBoardIfNeeded()
    {
        if (FindFirstObjectByType<KitchenOrderBoard>() != null)
        {
            return;
        }

        GameObject boardObject = new GameObject("KitchenOrderBoard");
        boardObject.transform.position = new Vector3(0f, 2.95f, 6.52f);
        boardObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        KitchenOrderBoard board = boardObject.AddComponent<KitchenOrderBoard>();
        board.Initialize();
    }

    /// <summary>
    /// Konfiguruje oświetlenie sceny kuchni — światło otoczenia, kierunkowe i reflektory punktowe.
    /// </summary>
    /// <remarks>
    /// Ustawia tryb oświetlenia otoczenia na Trilight (trzy kolory: niebo, równik, podłoże).
    /// Tworzy lub konfiguruje istniejące światło kierunkowe z miękkimi cieniami.
    /// Dodaje trzy reflektory punktowe (spot light) z efektem migotania lampy:
    /// <list type="bullet">
    /// <item><description>PrepTaskLightLeft — nad lewym stanowiskiem przygotowawczym</description></item>
    /// <item><description>PrepTaskLightRight — nad prawym stanowiskiem przygotowawczym</description></item>
    /// <item><description>AssemblyTaskLight — nad stanowiskiem montażu kebaba</description></item>
    /// </list>
    /// </remarks>
    private void ConfigureLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.74f, 0.76f, 0.8f);
        RenderSettings.ambientEquatorColor = new Color(0.4f, 0.38f, 0.34f);
        RenderSettings.ambientGroundColor = new Color(0.16f, 0.17f, 0.18f);
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.reflectionIntensity = 1f;
        RenderSettings.fog = false;

        Light directionalLight = FindDirectionalLight();
        if (directionalLight == null)
        {
            GameObject lightObject = new GameObject("Directional Light");
            directionalLight = lightObject.AddComponent<Light>();
        }

        directionalLight.type = LightType.Directional;
        directionalLight.transform.rotation = Quaternion.Euler(58f, -30f, 0f);
        directionalLight.color = new Color(1f, 0.96f, 0.88f);
        directionalLight.intensity = 1.2f;
        directionalLight.shadows = LightShadows.Soft;
        directionalLight.shadowStrength = 0.8f;
        directionalLight.shadowBias = 0.03f;
        directionalLight.shadowNormalBias = 0.35f;
        directionalLight.renderMode = LightRenderMode.ForcePixel;

        Transform lightingRoot = GameObject.Find("RuntimeLighting")?.transform;
        if (lightingRoot == null)
        {
            lightingRoot = new GameObject("RuntimeLighting").transform;
        }

        CreateSpotLight(
            lightingRoot,
            "PrepTaskLightLeft",
            new Vector3(-2.5f, 4.35f, 2.0f),
            new Vector3(90f, 0f, 0f),
            new Color(1f, 0.92f, 0.8f),
            6.5f,
            9.5f,
            88f);

        CreateSpotLight(
            lightingRoot,
            "PrepTaskLightRight",
            new Vector3(2.5f, 4.35f, 2.0f),
            new Vector3(90f, 0f, 0f),
            new Color(1f, 0.91f, 0.78f),
            6f,
            9.5f,
            88f);

        CreateSpotLight(
            lightingRoot,
            "AssemblyTaskLight",
            new Vector3(-3.2f, 4.25f, 5.1f),
            new Vector3(90f, 180f, 0f),
            new Color(1f, 0.93f, 0.82f),
            5.5f,
            8.5f,
            82f);
    }

    /// <summary>
    /// Tworzy pojedynczą stację kuchenną z prymitywu, konfiguruje ją i dodaje
    /// znacznik wizualny oraz komponenty sieciowe.
    /// </summary>
    /// <param name="parent">Transform rodzica, pod którym stacja zostanie umieszczona.</param>
    /// <param name="stationName">Nazwa stacji (np. "Grill", "Pomidor").</param>
    /// <param name="stationType">Typ stacji kuchennej definiujący jej zachowanie.</param>
    /// <param name="position">Pozycja stacji w przestrzeni świata.</param>
    /// <param name="color">Kolor bazowy materiału stacji.</param>
    /// <param name="sourceIngredient">Dane składnika źródłowego (null dla stacji niebędących źródłem składników).</param>
    /// <param name="processingDuration">Czas przetwarzania w sekundach (0 dla natychmiastowych operacji).</param>
    /// <returns>Utworzony komponent <see cref="KitchenStation"/>.</returns>
    /// <remarks>
    /// Stacja składa się z:
    /// <list type="bullet">
    /// <item><description>Sześcianu bazowego z koliderem i materiałem</description></item>
    /// <item><description>Znacznika sferycznego nad stacją (wizualny wskaźnik)</description></item>
    /// <item><description>Komponentu <see cref="KitchenStation"/> z konfiguracją logiki</description></item>
    /// <item><description>Komponentu <see cref="NetworkKitchenStation"/> do synchronizacji sieciowej</description></item>
    /// <item><description>Importowanych detali wizualnych specyficznych dla typu stacji</description></item>
    /// </list>
    /// Bazowe renderery prymitywów są ukrywane na rzecz importowanych modeli 3D.
    /// </remarks>
    private KitchenStation CreateStation(
        Transform parent,
        string stationName,
        KitchenStationType stationType,
        Vector3 position,
        Color color,
        IngredientData sourceIngredient,
        float processingDuration)
    {
        GameObject stationObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stationObject.name = stationName + "_Station";
        stationObject.transform.SetParent(parent);
        stationObject.transform.position = position;
        stationObject.transform.localScale = new Vector3(1.35f, 1.2f, 1.35f);
        stationObject.layer = InteractableLayer;

        Renderer renderer = stationObject.GetComponent<Renderer>();
        renderer.material = new Material(GetLitShader());
        renderer.material.color = color;

        KitchenStation station = stationObject.AddComponent<KitchenStation>();
        station.Configure(stationName, stationType, sourceIngredient, processingDuration, renderer);

        NetworkKitchenStation netStation = stationObject.AddComponent<NetworkKitchenStation>();
        netStation.StationIndex = stationIndexCounter++;

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = stationName + "_Marker";
        marker.transform.SetParent(stationObject.transform);
        marker.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        marker.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        marker.layer = InteractableLayer;

        Collider markerCollider = marker.GetComponent<Collider>();
        if (markerCollider != null)
        {
            markerCollider.enabled = false;
        }

        Renderer markerRenderer = marker.GetComponent<Renderer>();
        markerRenderer.material = new Material(GetLitShader());
        markerRenderer.material.color = sourceIngredient != null ? sourceIngredient.kolorDebug : color;

        CreateImportedStationDetails(stationObject.transform, stationName, stationType, sourceIngredient);
        renderer.enabled = false;
        markerRenderer.enabled = false;
        station.RefreshVisualState();
        return station;
    }

    /// <summary>
    /// Tworzy model klienta oczekującego przy okienku wydawania.
    /// Próbuje załadować importowany model 3D klienta, a w razie niepowodzenia
    /// tworzy zastępczą reprezentację z prymitywów (kapsuła + kula).
    /// </summary>
    /// <param name="parent">Transform rodzica dla obiektu klienta.</param>
    /// <param name="position">Pozycja klienta w przestrzeni świata.</param>
    private void CreateCustomer(Transform parent, Vector3 position)
    {
        Transform customerRoot = new GameObject("Customer").transform;
        customerRoot.SetParent(parent);
        customerRoot.position = position;

        GameObject customerModel = CreateImportedModel(customerRoot, "klient_idle", "CustomerVisual", Vector3.zero, new Vector3(0f, 0f, 0f), 1.95f);
        if (customerModel == null)
        {
            CreateBlock(customerRoot, "Body", PrimitiveType.Capsule, new Vector3(0f, 1f, 0f), new Vector3(0.9f, 1.8f, 0.9f), new Color(0.16f, 0.44f, 0.74f));
            CreateBlock(customerRoot, "Head", PrimitiveType.Sphere, new Vector3(0f, 2.25f, 0f), new Vector3(0.7f, 0.7f, 0.7f), new Color(0.92f, 0.78f, 0.63f));
        }
        else
        {
            customerModel.AddComponent<CustomerAnimator>();
        }
    }

    /// <summary>
    /// Tworzy blat roboczy (stół) składający się z bloku bazowego i bloku wierzchniego.
    /// </summary>
    /// <param name="parent">Transform rodzica dla blatu.</param>
    /// <param name="objectName">Nazwa obiektu blatu.</param>
    /// <param name="position">Pozycja lokalna blatu.</param>
    /// <param name="scale">Skala bazowego bloku blatu.</param>
    /// <param name="visible">Czy blat ma być widoczny. Jeśli false, renderery są wyłączone,
    /// a blok bazowy jest powiększony do roli niewidocznego blokera kolizji.</param>
    private void CreateCounter(Transform parent, string objectName, Vector3 position, Vector3 scale, bool visible = true)
    {
        Transform baseBlock = CreateBlock(parent, objectName, PrimitiveType.Cube, position, scale, new Color(0.36f, 0.31f, 0.27f));
        Transform topBlock = CreateBlock(parent, objectName + "_Top", PrimitiveType.Cube, position + new Vector3(0f, 0.31f, 0f), new Vector3(scale.x * 0.98f, 0.12f, scale.z * 0.98f), new Color(0.62f, 0.57f, 0.52f));

        if (!visible)
        {
            baseBlock.localPosition = new Vector3(position.x, 0.68f, position.z);
            baseBlock.localScale = new Vector3(scale.x, 1.36f, scale.z);
            SetRendererVisible(baseBlock, false);
            SetRendererVisible(topBlock, false);
        }
    }

    /// <summary>
    /// Tworzy grupę niewidocznych koliderów blokujących narożnik prawego górnego rogu kuchni.
    /// Zapobiega przechodzeniu gracza przez narożne blaty.
    /// </summary>
    /// <param name="parent">Transform rodzica dla grupy blokerów.</param>
    private void CreateCornerCounterBlockers(Transform parent)
    {
        Transform root = new GameObject("RightCornerCounterBlockers").transform;
        root.SetParent(parent);
        root.localPosition = Vector3.zero;

        CreateInvisibleCollider(root, "BackRun", new Vector3(5.05f, 0.62f, 5.3f), new Vector3(3.0f, 1.18f, 1.0f));
        CreateInvisibleCollider(root, "SideRun", new Vector3(5.95f, 0.62f, 4.15f), new Vector3(1.0f, 1.18f, 2.8f));
        CreateInvisibleCollider(root, "InnerCorner", new Vector3(5.45f, 0.62f, 4.75f), new Vector3(1.5f, 1.18f, 1.5f));
    }

    /// <summary>
    /// Tworzy niewidoczny obiekt z koliderem BoxCollider — używany jako bloker kolizji.
    /// </summary>
    /// <param name="parent">Transform rodzica dla kolidera.</param>
    /// <param name="objectName">Nazwa obiektu kolidera.</param>
    /// <param name="localPosition">Lokalna pozycja kolidera.</param>
    /// <param name="localScale">Rozmiar kolidera (ustawiany jako BoxCollider.size).</param>
    private void CreateInvisibleCollider(Transform parent, string objectName, Vector3 localPosition, Vector3 localScale)
    {
        GameObject colliderObject = new GameObject(objectName);
        colliderObject.transform.SetParent(parent);
        colliderObject.transform.localPosition = localPosition;
        colliderObject.transform.localRotation = Quaternion.identity;

        BoxCollider collider = colliderObject.AddComponent<BoxCollider>();
        collider.size = localScale;
    }

    /// <summary>
    /// Wyszukuje istniejące na scenie światło kierunkowe (Directional Light).
    /// </summary>
    /// <returns>
    /// Znalezione światło kierunkowe lub null, jeśli żadne nie istnieje na scenie.
    /// </returns>
    private Light FindDirectionalLight()
    {
        Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Light light in lights)
        {
            if (light != null && light.type == LightType.Directional)
            {
                return light;
            }
        }

        return null;
    }

    /// <summary>
    /// Tworzy lub konfiguruje istniejący reflektor punktowy (spot light) z efektem migotania lampy.
    /// </summary>
    /// <param name="parent">Transform rodzica dla światła.</param>
    /// <param name="lightName">Unikalna nazwa obiektu światła.</param>
    /// <param name="localPosition">Lokalna pozycja światła względem rodzica.</param>
    /// <param name="localRotation">Lokalna rotacja światła w stopniach Eulera.</param>
    /// <param name="color">Kolor światła.</param>
    /// <param name="intensity">Intensywność światła.</param>
    /// <param name="range">Zasięg światła w jednostkach sceny.</param>
    /// <param name="spotAngle">Kąt stożka świetlnego w stopniach.</param>
    /// <remarks>
    /// Jeśli światło o podanej nazwie już istnieje jako dziecko rodzica, jest rekonfigurowane.
    /// Każde światło otrzymuje komponent <see cref="LampFlicker"/> symulujący delikatne migotanie lampy.
    /// </remarks>
    private void CreateSpotLight(
        Transform parent,
        string lightName,
        Vector3 localPosition,
        Vector3 localRotation,
        Color color,
        float intensity,
        float range,
        float spotAngle)
    {
        Transform existing = parent.Find(lightName);
        GameObject lightObject = existing != null ? existing.gameObject : new GameObject(lightName);
        lightObject.transform.SetParent(parent);
        lightObject.transform.localPosition = localPosition;
        lightObject.transform.localRotation = Quaternion.Euler(localRotation);

        Light light = lightObject.GetComponent<Light>();
        if (light == null)
        {
            light = lightObject.AddComponent<Light>();
        }

        light.type = LightType.Spot;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.spotAngle = spotAngle;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.7f;
        light.shadowBias = 0.05f;
        light.shadowNormalBias = 0.4f;
        light.renderMode = LightRenderMode.ForcePixel;

        LampFlicker flicker = lightObject.GetComponent<LampFlicker>();
        if (flicker == null)
        {
            flicker = lightObject.AddComponent<LampFlicker>();
            flicker.Configure(light, intensity);
        }
    }

    /// <summary>
    /// Tworzy dekoracyjną lampę sufitową składającą się z cylindrycznego uchwytu i sferycznego klosza.
    /// </summary>
    /// <param name="parent">Transform rodzica dla lampy.</param>
    /// <param name="position">Pozycja lampy w przestrzeni lokalnej rodzica.</param>
    private void CreateCeilingLamp(Transform parent, Vector3 position)
    {
        Transform lamp = CreateBlock(parent, "Lamp", PrimitiveType.Cylinder, position, new Vector3(0.2f, 0.15f, 0.2f), new Color(0.15f, 0.15f, 0.15f));
        CreateBlock(lamp, "LightCone", PrimitiveType.Sphere, new Vector3(0f, -0.45f, 0f), new Vector3(0.5f, 0.18f, 0.5f), new Color(1f, 0.93f, 0.68f));
    }

    /// <summary>
    /// Tworzy wszystkie importowane detale wizualne środowiska kuchni:
    /// lampy, półki, stoły, narożny blat, stół użytkowy, kasę fiskalną i wystawę z tacą.
    /// </summary>
    /// <param name="parent">Transform rodzica dla detali środowiska.</param>
    /// <remarks>
    /// Lampy otrzymują komponent <see cref="LampEmissionPulse"/> do animacji emisji świetlnej.
    /// Stoły przygotowawcze są skalowane z mnożnikiem głębokości <see cref="TableDepthScale"/>.
    /// Na końcu tworzona jest wystawa z tacą do serwowania za pomocą <see cref="CreateDeliveryTrayDisplay"/>.
    /// </remarks>
    private void CreateImportedEnvironmentDetails(Transform parent)
    {
        GameObject lampLeft = CreateImportedModel(parent, "lamp", "ImportedLampLeft", new Vector3(-2.5f, 4.45f, 2f), new Vector3(0f, 0f, 0f), 0.9f);
        if (lampLeft != null && lampLeft.GetComponent<LampEmissionPulse>() == null)
        {
            lampLeft.AddComponent<LampEmissionPulse>();
        }

        GameObject lampRight = CreateImportedModel(parent, "lamp", "ImportedLampRight", new Vector3(2.5f, 4.45f, 2f), new Vector3(0f, 0f, 0f), 0.9f);
        if (lampRight != null && lampRight.GetComponent<LampEmissionPulse>() == null)
        {
            lampRight.AddComponent<LampEmissionPulse>();
        }

        CreateImportedModel(parent, "wall_shelf", "ImportedBackShelves", new Vector3(3.35f, 2.05f, 6.35f), new Vector3(0f, 180f, 0f), 2.2f);
        CreateImportedModel(parent, "prep_table", "ImportedLeftTableA", new Vector3(-5.55f, 0f, 4.6f), new Vector3(0f, 90f, 0f), TableVisualSize, new Vector3(1f, 1f, TableDepthScale));
        CreateImportedModel(parent, "prep_table", "ImportedLeftTableB", new Vector3(-5.55f, 0f, 1.5f), new Vector3(0f, 90f, 0f), TableVisualSize, new Vector3(1f, 1f, TableDepthScale));
        CreateImportedModel(parent, "prep_table", "ImportedLeftTableC", new Vector3(-5.55f, 0f, -1.1f), new Vector3(0f, 90f, 0f), TableVisualSize, new Vector3(1f, 1f, TableDepthScale));
        CreateImportedModel(parent, "prep_table", "ImportedLeftTableD", new Vector3(-5.55f, 0f, -3.7f), new Vector3(0f, 90f, 0f), TableVisualSize, new Vector3(1f, 1f, TableDepthScale));
        CreateImportedModel(parent, "prep_table", "ImportedBackTableA", new Vector3(-3.05f, 0f, 5.45f), new Vector3(0f, 180f, 0f), TableVisualSize, new Vector3(1f, 1f, TableDepthScale));
        CreateImportedModel(parent, "corner_counter", "ImportedRightCornerCounter", new Vector3(5.25f, 0f, 4.9f), new Vector3(0f, 180f, 0f), 2.9f, new Vector3(1.15f, 1f, 1.15f));
        CreateImportedModel(parent, "utility_table", "ImportedEntranceUtilityTable", new Vector3(EntranceTableX, 0f, EntranceTableZ), new Vector3(0f, 180f, 0f), 2.25f, new Vector3(1.25f, 1f, 1f));
        CreateImportedModel(parent, "cash_register", "EntranceCashRegisterVisual", new Vector3(EntranceTableX - 0.45f, EntranceTableTopY, EntranceTableZ + 0.1f), new Vector3(0f, 0f, 0f), 0.8f);
        CreateDeliveryTrayDisplay(parent);
    }

    /// <summary>
    /// Tworzy wystawę z tacą do serwowania i modelem kebaba przy wejściu.
    /// Kebab jest początkowo ukryty i pokazywany po wydaniu zamówienia.
    /// </summary>
    /// <param name="parent">Transform rodzica dla wystawy.</param>
    /// <remarks>
    /// Wystawa składa się z modelu tacy serwującej oraz modelu kebaba w zawinięciu.
    /// Komponent <see cref="DeliveryTrayDisplay"/> zarządza widocznością kebaba
    /// — jest on pokazywany na określony czas po każdym wydaniu zamówienia.
    /// </remarks>
    private void CreateDeliveryTrayDisplay(Transform parent)
    {
        Transform root = new GameObject("EntranceServingTrayDisplay").transform;
        root.SetParent(parent);
        root.localPosition = Vector3.zero;

        CreateImportedModel(root, "serving_tray", "EntranceServingTrayVisual", new Vector3(EntranceTableX + 0.52f, EntranceTableTopY + 0.02f, EntranceTableZ + 0.12f), new Vector3(0f, 180f, 0f), 1.2f);
        GameObject kebab = CreateImportedModel(root, "kebab_wrap", "EntranceServedKebabVisual", new Vector3(EntranceTableX + 0.52f, EntranceTableTopY + 0.1f, EntranceTableZ + 0.12f), new Vector3(0f, -18f, 0f), 0.5f);

        DeliveryTrayDisplay display = root.gameObject.AddComponent<DeliveryTrayDisplay>();
        display.Configure(kebab, 5f);
    }

    /// <summary>
    /// Tworzy importowane detale wizualne specyficzne dla danego typu stacji kuchennej.
    /// </summary>
    /// <param name="parent">Transform stacji, do którego dołączane są modele.</param>
    /// <param name="stationName">Nazwa stacji.</param>
    /// <param name="stationType">Typ stacji kuchennej determinujący, jakie modele zostaną utworzone.</param>
    /// <param name="sourceIngredient">Dane składnika źródłowego (używane dla stacji typu IngredientSource).</param>
    /// <remarks>
    /// W zależności od typu stacji tworzone są odpowiednie modele 3D:
    /// <list type="bullet">
    /// <item><description>CuttingBoard — deska do krojenia z pokrojonymi warzywami i nożem</description></item>
    /// <item><description>Grill — maszyna döner do grillowania mięsa</description></item>
    /// <item><description>Delivery — brak dodatkowych modeli</description></item>
    /// <item><description>Assembly — deska z chlebkiem ławasz do zawijania</description></item>
    /// <item><description>IngredientSource — tacka ze składnikami lub dozownik sosu</description></item>
    /// </list>
    /// </remarks>
    private void CreateImportedStationDetails(
        Transform parent,
        string stationName,
        KitchenStationType stationType,
        IngredientData sourceIngredient)
    {
        if (stationType == KitchenStationType.CuttingBoard)
        {
            CreateCuttingBoardDetails(parent);
            return;
        }

        if (stationType == KitchenStationType.Grill)
        {
            CreateImportedModel(parent, "doner_machine", "DonerGrillVisual", new Vector3(0f, WorktopLocalY, 0f), new Vector3(0f, 90f, 0f), 1.35f);
            return;
        }

        if (stationType == KitchenStationType.Delivery)
        {
            return;
        }

        if (stationType == KitchenStationType.Assembly)
        {
            Vector3 wrapBoardPosition = new Vector3(0.18f, WorktopLocalY, 0.04f);
            CreateImportedModel(parent, "cutting_board", "WrapBoardVisual", wrapBoardPosition, new Vector3(0f, -20f, 0f), 0.9f);
            CreateImportedModel(parent, "lavash", "LavashOnWrapStation", wrapBoardPosition + new Vector3(0f, 0.01f, -0.02f), new Vector3(0f, 12f, 0f), 0.55f);
            return;
        }

        if (stationType != KitchenStationType.IngredientSource)
        {
            return;
        }

        if (sourceIngredient != null && sourceIngredient.typSkladnika == IngredientKind.GarlicSauce)
        {
            CreateImportedModel(parent, "sauce_bottle", "SauceDispenserVisual", new Vector3(0f, WorktopLocalY, 0f), new Vector3(0f, 180f, 0f), 0.52f);
            return;
        }

        CreateImportedModel(parent, "ingredient_tray", stationName + "TrayVisual", new Vector3(0f, WorktopLocalY, 0f), new Vector3(0f, 0f, 0f), 0.85f);
        CreateIngredientVisual(parent, sourceIngredient);
    }

    /// <summary>
    /// Tworzy detale wizualne stacji deski do krojenia — deskę, pokrojone warzywa i nóż szefa kuchni.
    /// </summary>
    /// <param name="parent">Transform stacji deski do krojenia.</param>
    private void CreateCuttingBoardDetails(Transform parent)
    {
        float boardY = WorktopLocalY + 0.1f;
        float knifeY = WorktopLocalY + 0.04f;

        CreateImportedModel(parent, "cutting_board", "CuttingBoardVisual", new Vector3(0f, WorktopLocalY, 0f), new Vector3(0f, 15f, 0f), 0.95f);
        CreateImportedModel(parent, "tomato_chopped", "CutTomatoVisualA", new Vector3(-0.2f, boardY, 0.1f), new Vector3(0f, -24f, 0f), 0.3f);
        CreateImportedModel(parent, "onion_chopped", "CutOnionVisualA", new Vector3(0.18f, boardY, -0.02f), new Vector3(0f, 12f, 0f), 0.3f);
        CreateImportedModel(parent, "chef_knife", "KnifeVisual", new Vector3(0.36f, knifeY, 0.46f), new Vector3(90f, 63f, 0f), 0.5f);
    }

    /// <summary>
    /// Tworzy wizualne reprezentacje składników na stacji źródłowej —
    /// kilka instancji modeli 3D rozmieszczonych na tackce z różnymi rotacjami.
    /// </summary>
    /// <param name="parent">Transform stacji źródłowej składników.</param>
    /// <param name="sourceIngredient">Dane składnika określające, jaki model zostanie załadowany.</param>
    /// <remarks>
    /// Dla każdego typu składnika tworzone są inne modele:
    /// <list type="bullet">
    /// <item><description>Mięso — pojedynczy kawałek ugotowanego mięsa</description></item>
    /// <item><description>Pomidor — 5 całych pomidorów w różnych pozycjach</description></item>
    /// <item><description>Cebula — 5 całych cebul w różnych pozycjach</description></item>
    /// <item><description>Sałata — 3 główki sałaty</description></item>
    /// <item><description>Ławasz — chlebek ławasz (delegowane do <see cref="CreateLavashVisual"/>)</description></item>
    /// </list>
    /// </remarks>
    private void CreateIngredientVisual(Transform parent, IngredientData sourceIngredient)
    {
        if (sourceIngredient == null)
        {
            return;
        }

        float surfaceY = WorktopLocalY;

        switch (sourceIngredient.typSkladnika)
        {
            case IngredientKind.Meat:
                CreateImportedModel(parent, "meat_cooked", "MeatVisual", new Vector3(0f, surfaceY, 0f), new Vector3(0f, 0f, 0f), 0.55f);
                break;
            case IngredientKind.Tomato:
                surfaceY += 0.035f;
                CreateImportedModel(parent, "tomato_whole", "TomatoVisualA", new Vector3(-0.25f, surfaceY, -0.21f), new Vector3(0f, -11f, 0f), 0.25f);
                CreateImportedModel(parent, "tomato_whole", "TomatoVisualB", new Vector3(0.02f, surfaceY, -0.13f), new Vector3(0f, 37f, 0f), 0.22f);
                CreateImportedModel(parent, "tomato_whole", "TomatoVisualC", new Vector3(0.24f, surfaceY, -0.26f), new Vector3(0f, 94f, 0f), 0.24f);
                CreateImportedModel(parent, "tomato_whole", "TomatoVisualD", new Vector3(-0.07f, surfaceY, 0.12f), new Vector3(0f, -58f, 0f), 0.21f);
                CreateImportedModel(parent, "tomato_whole", "TomatoVisualE", new Vector3(0.2f, surfaceY, 0.07f), new Vector3(0f, 16f, 0f), 0.23f);
                break;
            case IngredientKind.Onion:
                surfaceY += 0.035f;
                CreateImportedModel(parent, "onion_whole", "OnionVisualA", new Vector3(-0.27f, surfaceY, -0.13f), new Vector3(0f, -31f, 0f), 0.25f);
                CreateImportedModel(parent, "onion_whole", "OnionVisualB", new Vector3(-0.03f, surfaceY, -0.22f), new Vector3(0f, 8f, 0f), 0.23f);
                CreateImportedModel(parent, "onion_whole", "OnionVisualC", new Vector3(0.22f, surfaceY, -0.04f), new Vector3(0f, 52f, 0f), 0.24f);
                CreateImportedModel(parent, "onion_whole", "OnionVisualD", new Vector3(-0.16f, surfaceY, 0.14f), new Vector3(0f, 89f, 0f), 0.22f);
                CreateImportedModel(parent, "onion_whole", "OnionVisualE", new Vector3(0.11f, surfaceY, 0.2f), new Vector3(0f, -48f, 0f), 0.24f);
                break;
            case IngredientKind.Lettuce:
                CreateImportedModel(parent, "lettuce_whole", "LettuceVisualA", new Vector3(-0.28f, surfaceY, -0.12f), new Vector3(0f, -10f, 0f), 0.34f);
                CreateImportedModel(parent, "lettuce_whole", "LettuceVisualB", new Vector3(0.18f, surfaceY, -0.03f), new Vector3(0f, 24f, 0f), 0.36f);
                CreateImportedModel(parent, "lettuce_whole", "LettuceVisualC", new Vector3(-0.36f, surfaceY, 0.24f), new Vector3(0f, 58f, 0f), 0.32f);
                break;
            case IngredientKind.Lavash:
                CreateLavashVisual(parent, surfaceY);
                break;
        }
    }

    /// <summary>
    /// Tworzy wizualną reprezentację chlebka ławasz na stacji źródłowej.
    /// </summary>
    /// <param name="parent">Transform stacji źródłowej ławasza.</param>
    /// <param name="surfaceY">Pozycja Y powierzchni blatu, na której umieszczany jest ławasz.</param>
    private void CreateLavashVisual(Transform parent, float surfaceY)
    {
        float lavashY = surfaceY + 0.055f;
        CreateImportedModel(parent, "lavash", "LavashVisual", new Vector3(0f, lavashY, 0f), new Vector3(0f, 12f, 0f), 0.72f);
    }

    /// <summary>
    /// Ładuje i tworzy instancję importowanego modelu 3D z folderu Resources.
    /// Przeciążenie bez mnożnika skali — deleguje do pełnej wersji z mnożnikiem Vector3.one.
    /// </summary>
    /// <param name="parent">Transform rodzica, pod którym model zostanie umieszczony.</param>
    /// <param name="resourceName">Nazwa zasobu modelu w katalogu Resources/Models/.</param>
    /// <param name="objectName">Nazwa nadawana instancji obiektu na scenie.</param>
    /// <param name="localPosition">Lokalna pozycja modelu względem rodzica.</param>
    /// <param name="localRotation">Lokalna rotacja modelu w stopniach Eulera.</param>
    /// <param name="targetMaxSize">Docelowy maksymalny rozmiar modelu (największy wymiar bounding box).</param>
    /// <returns>Utworzony obiekt modelu lub null, jeśli prefabrykat nie został znaleziony.</returns>
    private GameObject CreateImportedModel(
        Transform parent,
        string resourceName,
        string objectName,
        Vector3 localPosition,
        Vector3 localRotation,
        float targetMaxSize)
    {
        return CreateImportedModel(parent, resourceName, objectName, localPosition, localRotation, targetMaxSize, Vector3.one);
    }

    /// <summary>
    /// Ładuje i tworzy instancję importowanego modelu 3D z folderu Resources
    /// z pełną kontrolą nad skalowaniem.
    /// </summary>
    /// <param name="parent">Transform rodzica, pod którym model zostanie umieszczony.</param>
    /// <param name="resourceName">Nazwa zasobu modelu w katalogu Resources/Models/.</param>
    /// <param name="objectName">Nazwa nadawana instancji obiektu na scenie.</param>
    /// <param name="localPosition">Lokalna pozycja modelu względem rodzica.</param>
    /// <param name="localRotation">Lokalna rotacja modelu w stopniach Eulera.</param>
    /// <param name="targetMaxSize">Docelowy maksymalny rozmiar modelu (największy wymiar bounding box).</param>
    /// <param name="scaleMultiplier">Dodatkowy mnożnik skali stosowany po normalizacji rozmiaru.</param>
    /// <returns>Utworzony obiekt modelu lub null, jeśli prefabrykat nie został znaleziony.</returns>
    /// <remarks>
    /// Model jest najpierw skalowany uniformnie do docelowego rozmiaru za pomocą
    /// <see cref="ScaleModelToSize"/>, następnie stosowany jest mnożnik skali,
    /// wyrównywany jest spód modelu do żądanej wysokości Y, stosowane są materiały
    /// zastępcze tam gdzie brakuje, a kolidery importowane z modelu są wyłączane.
    /// </remarks>
    private GameObject CreateImportedModel(
        Transform parent,
        string resourceName,
        string objectName,
        Vector3 localPosition,
        Vector3 localRotation,
        float targetMaxSize,
        Vector3 scaleMultiplier)
    {
        GameObject prefab = Resources.Load<GameObject>(ModelPath + resourceName);
        if (prefab == null)
        {
            return null;
        }

        GameObject model = Instantiate(prefab, parent);
        model.name = objectName;
        model.transform.localPosition = localPosition;
        model.transform.localRotation = Quaternion.Euler(localRotation);
        model.transform.localScale = Vector3.one;

        ScaleModelToSize(model.transform, targetMaxSize);
        model.transform.localScale = Vector3.Scale(model.transform.localScale, scaleMultiplier);
        AlignModelBottomToLocalY(model.transform, localPosition.y);
        ApplyFallbackMaterialsIfMissing(model, resourceName);
        DisableImportedColliders(model);
        return model;
    }

    /// <summary>
    /// Wyrównuje spód modelu (najniższy punkt bounding box) do zadanej wysokości Y
    /// w przestrzeni lokalnej rodzica.
    /// </summary>
    /// <param name="modelRoot">Transform korzenia modelu do wyrównania.</param>
    /// <param name="targetBottomLocalY">Docelowa lokalna pozycja Y spodu modelu.</param>
    /// <remarks>
    /// Oblicza łączny bounding box wszystkich rendererów w modelu, przekształca
    /// najniższy punkt do przestrzeni lokalnej rodzica i przesuwa model pionowo
    /// tak, aby jego spód znajdował się dokładnie na żądanej wysokości.
    /// </remarks>
    private void AlignModelBottomToLocalY(Transform modelRoot, float targetBottomLocalY)
    {
        Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0 || modelRoot.parent == null)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 worldBottom = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        float currentBottomLocalY = modelRoot.parent.InverseTransformPoint(worldBottom).y;
        Vector3 position = modelRoot.localPosition;
        position.y += targetBottomLocalY - currentBottomLocalY;
        modelRoot.localPosition = position;
    }

    /// <summary>
    /// Stosuje materiały zastępcze (fallback) do rendererów importowanego modelu,
    /// które nie posiadają poprawnie przypisanych materiałów.
    /// </summary>
    /// <param name="model">Obiekt modelu do przetworzenia.</param>
    /// <param name="resourceName">Nazwa zasobu modelu — używana do doboru odpowiedniego koloru zastępczego.</param>
    /// <remarks>
    /// Iteruje po wszystkich rendererach w hierarchii modelu i dla tych,
    /// które nie mają importowanego materiału (brak tekstur, domyślna nazwa),
    /// tworzy nowy materiał z odpowiednim kolorem za pomocą <see cref="CreateImportedMaterial"/>.
    /// </remarks>
    private void ApplyFallbackMaterialsIfMissing(GameObject model, string resourceName)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (HasImportedMaterial(renderers[i]))
            {
                continue;
            }

            renderers[i].material = CreateImportedMaterial(resourceName, renderers[i].name, i);
        }
    }

    /// <summary>
    /// Sprawdza, czy renderer posiada poprawnie zaimportowany materiał
    /// (z teksturą lub niestandardową nazwą).
    /// </summary>
    /// <param name="renderer">Renderer do sprawdzenia.</param>
    /// <returns>True, jeśli renderer ma poprawny importowany materiał; false w przeciwnym razie.</returns>
    /// <remarks>
    /// Materiał jest uznawany za "importowany", jeśli:
    /// <list type="bullet">
    /// <item><description>Posiada przypisaną teksturę (BaseMap, MainTex lub mainTexture)</description></item>
    /// <item><description>Jego nazwa nie zawiera "default" ani "no name"</description></item>
    /// </list>
    /// </remarks>
    private bool HasImportedMaterial(Renderer renderer)
    {
        if (renderer == null || renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0)
        {
            return false;
        }

        foreach (Material material in renderer.sharedMaterials)
        {
            if (material == null)
            {
                continue;
            }

            if (HasMaterialTexture(material))
            {
                return true;
            }

            string materialName = material.name.ToLowerInvariant();
            if (!materialName.Contains("default") && !materialName.Contains("no name"))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Sprawdza, czy materiał posiada przypisaną teksturę w jednym ze standardowych slotów.
    /// </summary>
    /// <param name="material">Materiał do sprawdzenia.</param>
    /// <returns>True, jeśli materiał posiada teksturę w slocie _BaseMap, _MainTex lub mainTexture.</returns>
    private bool HasMaterialTexture(Material material)
    {
        if (material == null)
        {
            return false;
        }

        if (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null)
        {
            return true;
        }

        if (material.HasProperty("_MainTex") && material.GetTexture("_MainTex") != null)
        {
            return true;
        }

        return material.mainTexture != null;
    }

    /// <summary>
    /// Tworzy nowy materiał zastępczy z odpowiednim kolorem, metalicznością i gładkością
    /// dobranym na podstawie nazwy zasobu modelu.
    /// </summary>
    /// <param name="resourceName">Nazwa zasobu modelu — determinuje kolor i właściwości materiału.</param>
    /// <param name="rendererName">Nazwa renderera — używana w połączeniu z resourceName do identyfikacji.</param>
    /// <param name="index">Indeks renderera w modelu — wprowadza delikatną wariacę odcienia.</param>
    /// <returns>Nowo utworzony materiał z ustawionym kolorem, metalicznością i gładkością.</returns>
    /// <remarks>
    /// Dla modeli lamp materiał otrzymuje dodatkowo włączoną emisję świetlną o ciepłym odcieniu.
    /// Indeks renderera jest wykorzystywany do subtelnego przyciemniania kolejnych rendererów,
    /// co dodaje głębi wizualnej modelom wieloczęściowym.
    /// </remarks>
    private Material CreateImportedMaterial(string resourceName, string rendererName, int index)
    {
        Material material = new Material(GetLitShader());
        Color color = GetImportedModelColor(resourceName, rendererName, index);

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        float metallic = GetImportedModelMetallic(resourceName, rendererName);
        float smoothness = GetImportedModelSmoothness(resourceName, rendererName);

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", metallic);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", smoothness);
        }

        if (resourceName == "lamp" && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(1f, 0.78f, 0.35f) * 0.55f);
        }

        return material;
    }

    /// <summary>
    /// Zwraca odpowiedni kolor zastępczy dla importowanego modelu na podstawie jego nazwy.
    /// </summary>
    /// <param name="resourceName">Nazwa zasobu modelu.</param>
    /// <param name="rendererName">Nazwa renderera wewnątrz modelu.</param>
    /// <param name="index">Indeks renderera — używany do delikatnego przyciemniania (max 4 poziomy).</param>
    /// <returns>Kolor zastępczy dopasowany do typu modelu, z uwzględnieniem przyciemniania indeksem.</returns>
    /// <remarks>
    /// Mapowanie kolorów odbywa się na podstawie wyszukiwania słów kluczowych w połączonej
    /// nazwie zasobu i renderera (np. "meat", "doner_machine", "cutting_board" itp.).
    /// Domyślny kolor to neutralny szary w razie braku dopasowania.
    /// </remarks>
    private Color GetImportedModelColor(string resourceName, string rendererName, int index)
    {
        string name = (resourceName + " " + rendererName).ToLowerInvariant();
        float shade = 1f - Mathf.Min(index, 4) * 0.045f;

        if (name.Contains("meat"))
        {
            return new Color(0.62f, 0.25f, 0.15f) * shade;
        }

        if (name.Contains("doner_machine"))
        {
            return new Color(0.42f, 0.43f, 0.42f) * shade;
        }

        if (name.Contains("chef_knife"))
        {
            return new Color(0.72f, 0.75f, 0.76f) * shade;
        }

        if (name.Contains("cash_register"))
        {
            return new Color(0.08f, 0.1f, 0.12f) * shade;
        }

        if (name.Contains("sauce_bottle"))
        {
            return new Color(0.9f, 0.82f, 0.62f) * shade;
        }

        if (name.Contains("cutting_board"))
        {
            return new Color(0.58f, 0.38f, 0.19f) * shade;
        }

        if (name.Contains("prep_table") || name.Contains("utility_table"))
        {
            return new Color(0.45f, 0.46f, 0.47f) * shade;
        }

        if (name.Contains("corner_counter"))
        {
            return new Color(0.5f, 0.51f, 0.52f) * shade;
        }

        if (name.Contains("ingredient_tray") || name.Contains("serving_tray"))
        {
            return new Color(0.55f, 0.56f, 0.54f) * shade;
        }

        if (name.Contains("wall_shelf"))
        {
            return new Color(0.5f, 0.5f, 0.52f) * shade;
        }

        if (name.Contains("lamp"))
        {
            return new Color(1f, 0.83f, 0.48f) * shade;
        }

        if (name.Contains("wall"))
        {
            return new Color(0.72f, 0.74f, 0.75f) * shade;
        }

        return new Color(0.62f, 0.62f, 0.6f) * shade;
    }

    /// <summary>
    /// Zwraca wartość metaliczności materiału zastępczego na podstawie nazwy modelu.
    /// </summary>
    /// <param name="resourceName">Nazwa zasobu modelu.</param>
    /// <param name="rendererName">Nazwa renderera wewnątrz modelu.</param>
    /// <returns>
    /// Wartość metaliczności: 0.45 dla metalowych obiektów (nóż, maszyna, kasa, stoły, tace, półki),
    /// 0.0 dla pozostałych.
    /// </returns>
    private float GetImportedModelMetallic(string resourceName, string rendererName)
    {
        string name = (resourceName + " " + rendererName).ToLowerInvariant();
        if (name.Contains("chef_knife") ||
            name.Contains("doner_machine") ||
            name.Contains("cash_register") ||
            name.Contains("prep_table") ||
            name.Contains("utility_table") ||
            name.Contains("corner_counter") ||
            name.Contains("ingredient_tray") ||
            name.Contains("serving_tray") ||
            name.Contains("wall_shelf"))
        {
            return 0.45f;
        }

        return 0f;
    }

    /// <summary>
    /// Zwraca wartość gładkości materiału zastępczego na podstawie nazwy modelu.
    /// </summary>
    /// <param name="resourceName">Nazwa zasobu modelu.</param>
    /// <param name="rendererName">Nazwa renderera wewnątrz modelu.</param>
    /// <returns>
    /// Wartość gładkości: 0.55 dla metalowych obiektów, 0.25 dla mięsa i desek,
    /// 0.35 jako wartość domyślna dla pozostałych materiałów.
    /// </returns>
    private float GetImportedModelSmoothness(string resourceName, string rendererName)
    {
        string name = (resourceName + " " + rendererName).ToLowerInvariant();
        if (name.Contains("chef_knife") ||
            name.Contains("doner_machine") ||
            name.Contains("cash_register") ||
            name.Contains("prep_table") ||
            name.Contains("utility_table") ||
            name.Contains("corner_counter") ||
            name.Contains("ingredient_tray") ||
            name.Contains("serving_tray") ||
            name.Contains("wall_shelf"))
        {
            return 0.55f;
        }

        if (name.Contains("meat") || name.Contains("cutting_board"))
        {
            return 0.25f;
        }

        return 0.35f;
    }

    /// <summary>
    /// Skaluje model uniformnie tak, aby jego największy wymiar (bounding box)
    /// odpowiadał zadanemu rozmiarowi docelowemu.
    /// </summary>
    /// <param name="modelRoot">Transform korzenia modelu do przeskalowania.</param>
    /// <param name="targetMaxSize">Docelowy rozmiar maksymalnego wymiaru bounding box.
    /// Wartość 0 lub ujemna powoduje pominięcie skalowania.</param>
    /// <remarks>
    /// Oblicza łączny bounding box wszystkich rendererów, znajduje największy wymiar,
    /// a następnie mnoży skalę lokalną przez współczynnik (targetMaxSize / maxSize).
    /// Chroni przed dzieleniem przez zero dla modeli o minimalnym rozmiarze.
    /// </remarks>
    private void ScaleModelToSize(Transform modelRoot, float targetMaxSize)
    {
        if (targetMaxSize <= 0f)
        {
            return;
        }

        Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float maxSize = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        if (maxSize <= 0.001f)
        {
            return;
        }

        modelRoot.localScale *= targetMaxSize / maxSize;
    }

    /// <summary>
    /// Wyłącza wszystkie kolidery w hierarchii importowanego modelu.
    /// Zapobiega interferencji koliderów modelu z systemem fizyki gry.
    /// </summary>
    /// <param name="model">Obiekt modelu, w którym kolidery zostaną wyłączone.</param>
    private void DisableImportedColliders(GameObject model)
    {
        foreach (Collider collider in model.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }
    }

    /// <summary>
    /// Ustawia widoczność wszystkich rendererów w hierarchii danego transformu.
    /// </summary>
    /// <param name="root">Transform korzenia hierarchii do modyfikacji.</param>
    /// <param name="visible">Czy renderery mają być widoczne (true) czy ukryte (false).</param>
    private void SetRendererVisible(Transform root, bool visible)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = visible;
        }
    }

    /// <summary>
    /// Tworzy blok geometryczny z prymitywu Unity z nadanym materiałem i kolorem.
    /// Uniwersalna metoda pomocnicza do budowania elementów środowiska.
    /// </summary>
    /// <param name="parent">Transform rodzica dla bloku.</param>
    /// <param name="objectName">Nazwa obiektu bloku.</param>
    /// <param name="primitiveType">Typ prymitywu Unity (sześcian, kula, cylinder itp.).</param>
    /// <param name="localPosition">Lokalna pozycja bloku.</param>
    /// <param name="localScale">Lokalna skala bloku.</param>
    /// <param name="color">Kolor materiału bloku.</param>
    /// <returns>Transform utworzonego bloku.</returns>
    private Transform CreateBlock(
        Transform parent,
        string objectName,
        PrimitiveType primitiveType,
        Vector3 localPosition,
        Vector3 localScale,
        Color color)
    {
        GameObject block = GameObject.CreatePrimitive(primitiveType);
        block.name = objectName;
        block.transform.SetParent(parent);
        block.transform.localPosition = localPosition;
        block.transform.localScale = localScale;

        Renderer renderer = block.GetComponent<Renderer>();
        renderer.material = new Material(GetLitShader());
        renderer.material.color = color;
        return block.transform;
    }

    /// <summary>
    /// Tworzy etykietę tekstową 3D (TextMesh) z komponentem billboard,
    /// która zawsze jest zwrócona przodem do kamery.
    /// </summary>
    /// <param name="parent">Transform rodzica dla etykiety.</param>
    /// <param name="labelText">Tekst wyświetlany na etykiecie.</param>
    /// <param name="localPosition">Lokalna pozycja etykiety.</param>
    /// <remarks>
    /// Etykieta używa komponentu <see cref="BillboardLabel"/> do automatycznego
    /// obracania się w kierunku kamery w każdej klatce. Tekst jest półprzezroczysty (alpha 0.55).
    /// </remarks>
    private void CreateLabel(Transform parent, string labelText, Vector3 localPosition)
    {
        GameObject label = new GameObject(labelText + "_Label");
        label.transform.SetParent(parent);
        label.transform.localPosition = localPosition;

        TextMesh textMesh = label.AddComponent<TextMesh>();
        textMesh.text = labelText;
        textMesh.characterSize = 0.02f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = 24;
        textMesh.color = new Color(1f, 1f, 1f, 0.55f);

        label.AddComponent<BillboardLabel>();
    }
}

/// <summary>
/// Komponent billboard — obraca obiekt tak, aby zawsze był zwrócony przodem do głównej kamery.
/// Używany głównie do etykiet tekstowych 3D nad stacjami kuchennymi.
/// </summary>
/// <remarks>
/// Aktualizacja następuje w LateUpdate, aby zapewnić poprawną orientację
/// po ruchu kamery w danej klatce. Automatycznie wyszukuje główną kamerę
/// przy pierwszym użyciu lub gdy referencja zostanie utracona.
/// </remarks>
public class BillboardLabel : MonoBehaviour
{
    /// <summary>
    /// Referencja do kamery, w kierunku której obiekt jest obracany.
    /// Automatycznie ustawiana na Camera.main, gdy jest null.
    /// </summary>
    private Camera targetCamera;

    /// <summary>
    /// Aktualizuje orientację obiektu w każdej klatce (po Update),
    /// ustawiając jego wektor forward na wektor forward kamery.
    /// </summary>
    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        transform.forward = targetCamera.transform.forward;
    }
}

/// <summary>
/// Zarządza wyświetlaniem serwowanego kebaba na tacy przy okienku wydawania.
/// Kebab jest pokazywany na określony czas po każdym wydaniu zamówienia,
/// a następnie automatycznie ukrywany.
/// </summary>
/// <remarks>
/// Klasa implementuje wzorzec singletona poprzez statyczne pole <see cref="activeDisplay"/>,
/// umożliwiając łatwe wywoływanie z dowolnego miejsca kodu za pomocą
/// metody statycznej <see cref="ShowServedKebab"/>.
/// </remarks>
public class DeliveryTrayDisplay : MonoBehaviour
{
    /// <summary>
    /// Aktywna instancja wyświetlacza tacy — wzorzec singletona.
    /// Umożliwia globalne wywoływanie <see cref="ShowServedKebab"/> bez referencji.
    /// </summary>
    private static DeliveryTrayDisplay activeDisplay;

    /// <summary>
    /// Referencja do obiektu wizualnego kebaba na tacy.
    /// Jego aktywność (SetActive) jest przełączana przy pokazywaniu/ukrywaniu.
    /// </summary>
    [SerializeField] private GameObject servedKebab;

    /// <summary>
    /// Czas trwania widoczności kebaba w sekundach po wywołaniu Show.
    /// </summary>
    [SerializeField] private float visibleDuration = 5f;

    /// <summary>
    /// Znacznik czasu (Time.time), po przekroczeniu którego kebab zostanie ukryty.
    /// </summary>
    private float hideAtTime;

    /// <summary>
    /// Statyczna metoda wywoływana po wydaniu zamówienia — pokazuje kebab na tacy
    /// przy okienku przez czas określony w <see cref="visibleDuration"/>.
    /// </summary>
    public static void ShowServedKebab()
    {
        if (activeDisplay != null)
        {
            activeDisplay.Show();
        }
    }

    /// <summary>
    /// Konfiguruje wyświetlacz tacy z referencją do obiektu kebaba i czasem widoczności.
    /// Ustawia bieżącą instancję jako aktywną i ukrywa kebab na starcie.
    /// </summary>
    /// <param name="servedKebab">Obiekt wizualny kebaba do pokazywania/ukrywania.</param>
    /// <param name="visibleDuration">Czas widoczności kebaba w sekundach.</param>
    public void Configure(GameObject servedKebab, float visibleDuration)
    {
        activeDisplay = this;
        this.servedKebab = servedKebab;
        this.visibleDuration = visibleDuration;
        SetKebabVisible(false);
    }

    /// <summary>
    /// Sprawdza w każdej klatce, czy czas widoczności kebaba upłynął,
    /// i jeśli tak — ukrywa go.
    /// </summary>
    private void Update()
    {
        if (servedKebab == null || !servedKebab.activeSelf || Time.time < hideAtTime)
        {
            return;
        }

        SetKebabVisible(false);
    }

    /// <summary>
    /// Czyści referencję singletona przy niszczeniu obiektu,
    /// aby uniknąć odwołań do zniszczonego komponentu.
    /// </summary>
    private void OnDestroy()
    {
        if (activeDisplay == this)
        {
            activeDisplay = null;
        }
    }

    /// <summary>
    /// Pokazuje kebab na tacy i ustawia timer automatycznego ukrycia.
    /// </summary>
    private void Show()
    {
        hideAtTime = Time.time + visibleDuration;
        SetKebabVisible(true);
    }

    /// <summary>
    /// Ustawia widoczność obiektu wizualnego kebaba.
    /// </summary>
    /// <param name="visible">True aby pokazać kebab, false aby go ukryć.</param>
    private void SetKebabVisible(bool visible)
    {
        if (servedKebab != null)
        {
            servedKebab.SetActive(visible);
        }
    }
}

/// <summary>
/// Komponent animujący model klienta za pomocą systemu Playable API Unity.
/// Ładuje klip animacji idle z zasobów i odtwarza go w pętli.
/// </summary>
/// <remarks>
/// Używa niskiego poziomu Playable API zamiast Animator Controller,
/// co pozwala na dynamiczne ładowanie i odtwarzanie animacji bez potrzeby
/// tworzenia kontrolera animacji w edytorze. Animacja jest ręcznie zapętlana
/// w metodzie Update, resetując czas po osiągnięciu końca klipu.
/// </remarks>
public class CustomerAnimator : MonoBehaviour
{
    /// <summary>
    /// Graf odtwarzania Playable — zarządza łańcuchem odtwarzania animacji.
    /// Musi być jawnie niszczony w OnDestroy, aby uniknąć wycieków pamięci.
    /// </summary>
    private UnityEngine.Playables.PlayableGraph graph;

    /// <summary>
    /// Playable opakowujący klip animacji idle klienta.
    /// Pozwala na kontrolę czasu odtwarzania animacji.
    /// </summary>
    private UnityEngine.Animations.AnimationClipPlayable idlePlayable;

    /// <summary>
    /// Długość klipu animacji idle w sekundach.
    /// Używana do ręcznego zapętlania animacji w Update.
    /// </summary>
    private float clipLength;

    /// <summary>
    /// Inicjalizuje system animacji klienta — ładuje klip idle z Resources,
    /// tworzy graf Playable i rozpoczyna odtwarzanie.
    /// </summary>
    /// <remarks>
    /// Wyszukuje klipy animacji w katalogu "Models/klient_idle" i wybiera
    /// pierwszy klip, który nie jest podglądem (nie zaczyna się od "__preview").
    /// Jeśli na obiekcie nie ma komponentu Animator, zostaje on automatycznie dodany.
    /// </remarks>
    private void Start()
    {
        AnimationClip[] idles = Resources.LoadAll<AnimationClip>("Models/klient_idle");
        if (idles != null && idles.Length > 0)
        {
            AnimationClip idleClip = idles.FirstOrDefault(c => !c.name.StartsWith("__preview")) ?? idles.FirstOrDefault();
            if (idleClip != null)
            {
                clipLength = idleClip.length;
                Animator animator = GetComponent<Animator>();
                if (animator == null) animator = gameObject.AddComponent<Animator>();

                graph = UnityEngine.Playables.PlayableGraph.Create("CustomerAnimGraph");
                graph.SetTimeUpdateMode(UnityEngine.Playables.DirectorUpdateMode.GameTime);

                var output = UnityEngine.Animations.AnimationPlayableOutput.Create(graph, "Animation", animator);
                idlePlayable = UnityEngine.Animations.AnimationClipPlayable.Create(graph, idleClip);
                output.SetSourcePlayable(idlePlayable);

                graph.Play();
            }
        }
    }

    /// <summary>
    /// Ręcznie zapętla animację idle — resetuje czas odtwarzania po osiągnięciu końca klipu.
    /// </summary>
    /// <remarks>
    /// Konieczne, ponieważ Playable API nie zapętla automatycznie klipów animacji
    /// bez dodatkowej konfiguracji. Używa operatora modulo na czasie, aby zachować
    /// płynne przejście między iteracjami animacji.
    /// </remarks>
    private void Update()
    {
        if (graph.IsValid() && idlePlayable.IsValid() && clipLength > 0f)
        {
            if (idlePlayable.GetTime() >= clipLength)
            {
                idlePlayable.SetTime(idlePlayable.GetTime() % clipLength);
            }
        }
    }

    /// <summary>
    /// Niszczy graf Playable przy niszczeniu komponentu, aby zwolnić zasoby natywne.
    /// </summary>
    /// <remarks>
    /// Graf PlayableGraph alokuje pamięć natywną, która nie jest zarządzana przez
    /// garbage collector .NET — musi być jawnie zwolniona wywołaniem Destroy().
    /// </remarks>
    private void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
    }
}
