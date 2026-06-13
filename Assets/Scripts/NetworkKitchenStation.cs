/// \file NetworkKitchenStation.cs
/// \brief Plik zawierający klasę NetworkKitchenStation oraz strukturę StationStateSnapshot.
/// \details Definiuje logikę sieciowej synchronizacji stanu stacji kuchennych,
/// w tym wykrywanie zmian stanu, tworzenie migawek (snapshot) i ich aplikowanie
/// na klientach w grze wieloosobowej.

using UnityEngine;

/// <summary>
/// Komponent sieciowy opakowujący lokalną stację kuchenną (<see cref="KitchenStation"/>).
/// </summary>
/// <remarks>
/// Odpowiada za:
/// <list type="bullet">
///   <item>Wykrywanie zmian stanu lokalnej stacji kuchennej (dirty check)</item>
///   <item>Tworzenie migawek stanu do przesyłania przez sieć</item>
///   <item>Aplikowanie migawek odebranych od serwera na kliencie</item>
///   <item>Wykonywanie interakcji na serwerze w imieniu gracza</item>
/// </list>
/// Każda stacja kuchenna na scenie powinna posiadać ten komponent
/// wraz z unikalnym <see cref="StationIndex"/>.
/// </remarks>
public class NetworkKitchenStation : MonoBehaviour
{
    /// <summary>
    /// Referencja do lokalnego komponentu stacji kuchennej.
    /// </summary>
    private KitchenStation localStation;

    /// <summary>
    /// Ostatni znany stan przetwarzania (czy stacja aktualnie przetwarza składnik).
    /// </summary>
    private bool lastIsProcessing;

    /// <summary>
    /// Ostatni znany pozostały czas przetwarzania (w sekundach).
    /// </summary>
    private float lastRemainingProcessTime;

    /// <summary>
    /// Ostatnia znana liczba przygotowanych porcji mięsa.
    /// </summary>
    private int lastPreparedMeatServings;

    /// <summary>
    /// Ostatni znany stan obecności lawasza na stacji.
    /// </summary>
    private bool lastHasLavash;

    /// <summary>
    /// Ostatnia znana liczba składników w zestawie montażowym (assembly).
    /// </summary>
    private int lastAssemblyCount;

    /// <summary>
    /// Ostatni znany stan sieciowy przedmiotu znajdującego się na stacji.
    /// </summary>
    private NetworkItemState lastStationItem;

    /// <summary>
    /// Unikalny indeks stacji kuchennej na scenie.
    /// </summary>
    /// <value>Indeks używany do identyfikacji stacji w komunikacji sieciowej.</value>
    /// <remarks>
    /// Musi być unikalny w obrębie sceny. Służy do dopasowania migawek
    /// do odpowiednich stacji na klientach.
    /// </remarks>
    public int StationIndex { get; set; }

    /// <summary>
    /// Inicjalizuje referencję do lokalnej stacji kuchennej.
    /// </summary>
    private void Start()
    {
        localStation = GetComponent<KitchenStation>();
    }

    /// <summary>
    /// Sprawdza, czy stan stacji kuchennej zmienił się od ostatniej migawki.
    /// </summary>
    /// <returns>
    /// <c>true</c> jeśli którakolwiek z właściwości stacji uległa zmianie;
    /// w przeciwnym razie <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Porównuje aktualny stan stacji z ostatnio zapamiętanymi wartościami:
    /// stan przetwarzania, obecność lawasza, liczbę porcji mięsa,
    /// liczbę składników montażowych, pozostały czas przetwarzania (z tolerancją 0.5s)
    /// oraz stan przedmiotu na stacji.
    /// </remarks>
    public bool IsStateDirty()
    {
        if (localStation == null) return false;

        if (localStation.IsProcessing != lastIsProcessing) return true;
        if (localStation.HasLavash != lastHasLavash) return true;
        if (localStation.PreparedMeatServings != lastPreparedMeatServings) return true;
        if (localStation.AssemblyCount != lastAssemblyCount) return true;

        float currentRemaining = localStation.IsProcessing
            ? Mathf.Max(0f, localStation.ProcessEndTime - Time.time)
            : 0f;
        if (Mathf.Abs(currentRemaining - lastRemainingProcessTime) > 0.5f) return true;

        NetworkItemState currentItem = NetworkItemState.FromKitchenItem(localStation.StationItem);
        if (currentItem.exists != lastStationItem.exists) return true;
        if (currentItem.ingredientKind != lastStationItem.ingredientKind) return true;
        if (currentItem.state != lastStationItem.state) return true;

        return false;
    }

    /// <summary>
    /// Tworzy migawkę aktualnego stanu stacji kuchennej i zapamiętuje go jako ostatni znany.
    /// </summary>
    /// <returns>
    /// Struktura <see cref="StationStateSnapshot"/> zawierająca pełny stan stacji
    /// lub wartość domyślna, jeśli lokalna stacja nie jest dostępna.
    /// </returns>
    /// <remarks>
    /// Aktualizuje wewnętrzne zmienne śledzące ostatni stan (dirty tracking),
    /// a następnie deleguje zapis składników montażowych do <see cref="KitchenStation.WriteAssemblyToSnapshot"/>.
    /// </remarks>
    public StationStateSnapshot CaptureSnapshot()
    {
        if (localStation == null) return default;

        lastIsProcessing = localStation.IsProcessing;
        lastRemainingProcessTime = localStation.IsProcessing
            ? Mathf.Max(0f, localStation.ProcessEndTime - Time.time)
            : 0f;
        lastPreparedMeatServings = localStation.PreparedMeatServings;
        lastHasLavash = localStation.HasLavash;
        lastAssemblyCount = localStation.AssemblyCount;
        lastStationItem = NetworkItemState.FromKitchenItem(localStation.StationItem);

        StationStateSnapshot snapshot = new StationStateSnapshot
        {
            stationIndex = StationIndex,
            isProcessing = lastIsProcessing,
            remainingProcessTime = lastRemainingProcessTime,
            preparedMeatServings = lastPreparedMeatServings,
            hasLavash = lastHasLavash,
            assemblyCount = lastAssemblyCount,
            stationItem = lastStationItem
        };

        localStation.WriteAssemblyToSnapshot(ref snapshot);

        return snapshot;
    }

    /// <summary>
    /// Aplikuje migawkę stanu stacji otrzymaną od serwera na lokalnej stacji kuchennej.
    /// </summary>
    /// <param name="snapshot">Migawka stanu do zastosowania.</param>
    /// <remarks>
    /// Automatycznie inicjalizuje referencję do lokalnej stacji, jeśli nie jest jeszcze dostępna.
    /// Deleguje synchronizację do <see cref="KitchenStation.SyncNetworkState"/>.
    /// </remarks>
    public void ApplySnapshot(StationStateSnapshot snapshot)
    {
        if (localStation == null)
        {
            localStation = GetComponent<KitchenStation>();
        }

        if (localStation != null)
        {
            localStation.SyncNetworkState(snapshot);
        }
    }

    /// <summary>
    /// Wykonuje interakcję na stacji kuchennej na serwerze w imieniu danego gracza.
    /// </summary>
    /// <param name="interaction">Komponent interakcji gracza wykonującego akcję.</param>
    /// <remarks>
    /// Deleguje wywołanie do <see cref="KitchenStation.Interact"/>.
    /// Wywoływana wyłącznie na serwerze przez <see cref="NetworkPlayer.InteractWithStationServerRpc"/>.
    /// </remarks>
    public void ServerInteract(PlayerInteraction interaction)
    {
        if (localStation != null)
        {
            localStation.Interact(interaction);
        }
    }
}

/// <summary>
/// Struktura przechowująca migawkę stanu stacji kuchennej do przesyłania przez sieć.
/// </summary>
/// <remarks>
/// Implementuje <see cref="Unity.Netcode.INetworkSerializable"/> do serializacji binarnej.
/// Zawiera informacje o stanie przetwarzania, przedmiocie na stacji,
/// obecności lawasza, przygotowanych porcjach mięsa oraz do 8 składnikach
/// w zestawie montażowym (assembly).
/// Serializacja składników montażowych jest optymalizowana — przesyłane są
/// tylko te sloty, które są w użyciu (na podstawie <see cref="assemblyCount"/>).
/// </remarks>
public struct StationStateSnapshot : Unity.Netcode.INetworkSerializable
{
    /// <summary>
    /// Indeks stacji kuchennej, do której należy ta migawka.
    /// </summary>
    public int stationIndex;

    /// <summary>
    /// Czy stacja aktualnie przetwarza składnik.
    /// </summary>
    public bool isProcessing;

    /// <summary>
    /// Pozostały czas przetwarzania (w sekundach).
    /// </summary>
    public float remainingProcessTime;

    /// <summary>
    /// Liczba przygotowanych porcji mięsa na stacji.
    /// </summary>
    public int preparedMeatServings;

    /// <summary>
    /// Czy na stacji znajduje się lawasz.
    /// </summary>
    public bool hasLavash;

    /// <summary>
    /// Liczba składników w zestawie montażowym (assembly).
    /// </summary>
    public int assemblyCount;

    /// <summary>
    /// Stan sieciowy przedmiotu znajdującego się na stacji.
    /// </summary>
    public NetworkItemState stationItem;

    /// <summary>Rodzaj składnika w slocie montażowym 0.</summary>
    public IngredientKind assembly0Kind;
    /// <summary>Stan przetworzenia składnika w slocie montażowym 0.</summary>
    public IngredientProcessState assembly0State;
    /// <summary>Rodzaj składnika w slocie montażowym 1.</summary>
    public IngredientKind assembly1Kind;
    /// <summary>Stan przetworzenia składnika w slocie montażowym 1.</summary>
    public IngredientProcessState assembly1State;
    /// <summary>Rodzaj składnika w slocie montażowym 2.</summary>
    public IngredientKind assembly2Kind;
    /// <summary>Stan przetworzenia składnika w slocie montażowym 2.</summary>
    public IngredientProcessState assembly2State;
    /// <summary>Rodzaj składnika w slocie montażowym 3.</summary>
    public IngredientKind assembly3Kind;
    /// <summary>Stan przetworzenia składnika w slocie montażowym 3.</summary>
    public IngredientProcessState assembly3State;
    /// <summary>Rodzaj składnika w slocie montażowym 4.</summary>
    public IngredientKind assembly4Kind;
    /// <summary>Stan przetworzenia składnika w slocie montażowym 4.</summary>
    public IngredientProcessState assembly4State;
    /// <summary>Rodzaj składnika w slocie montażowym 5.</summary>
    public IngredientKind assembly5Kind;
    /// <summary>Stan przetworzenia składnika w slocie montażowym 5.</summary>
    public IngredientProcessState assembly5State;
    /// <summary>Rodzaj składnika w slocie montażowym 6.</summary>
    public IngredientKind assembly6Kind;
    /// <summary>Stan przetworzenia składnika w slocie montażowym 6.</summary>
    public IngredientProcessState assembly6State;
    /// <summary>Rodzaj składnika w slocie montażowym 7.</summary>
    public IngredientKind assembly7Kind;
    /// <summary>Stan przetworzenia składnika w slocie montażowym 7.</summary>
    public IngredientProcessState assembly7State;

    /// <summary>
    /// Ustawia rodzaj i stan przetworzenia składnika w określonym slocie montażowym.
    /// </summary>
    /// <param name="index">Indeks slotu (0-7).</param>
    /// <param name="kind">Rodzaj składnika do ustawienia.</param>
    /// <param name="state">Stan przetworzenia składnika do ustawienia.</param>
    /// <remarks>
    /// Indeksy spoza zakresu 0-7 są ignorowane.
    /// </remarks>
    public void SetAssemblySlot(int index, IngredientKind kind, IngredientProcessState state)
    {
        switch (index)
        {
            case 0: assembly0Kind = kind; assembly0State = state; break;
            case 1: assembly1Kind = kind; assembly1State = state; break;
            case 2: assembly2Kind = kind; assembly2State = state; break;
            case 3: assembly3Kind = kind; assembly3State = state; break;
            case 4: assembly4Kind = kind; assembly4State = state; break;
            case 5: assembly5Kind = kind; assembly5State = state; break;
            case 6: assembly6Kind = kind; assembly6State = state; break;
            case 7: assembly7Kind = kind; assembly7State = state; break;
        }
    }

    /// <summary>
    /// Pobiera rodzaj i stan przetworzenia składnika z określonego slotu montażowego.
    /// </summary>
    /// <param name="index">Indeks slotu (0-7).</param>
    /// <param name="kind">Wynikowy rodzaj składnika.</param>
    /// <param name="state">Wynikowy stan przetworzenia składnika.</param>
    /// <remarks>
    /// Dla indeksów spoza zakresu 0-7 zwraca wartości domyślne:
    /// <see cref="IngredientKind.Meat"/> i <see cref="IngredientProcessState.Raw"/>.
    /// </remarks>
    public void GetAssemblySlot(int index, out IngredientKind kind, out IngredientProcessState state)
    {
        switch (index)
        {
            case 0: kind = assembly0Kind; state = assembly0State; break;
            case 1: kind = assembly1Kind; state = assembly1State; break;
            case 2: kind = assembly2Kind; state = assembly2State; break;
            case 3: kind = assembly3Kind; state = assembly3State; break;
            case 4: kind = assembly4Kind; state = assembly4State; break;
            case 5: kind = assembly5Kind; state = assembly5State; break;
            case 6: kind = assembly6Kind; state = assembly6State; break;
            case 7: kind = assembly7Kind; state = assembly7State; break;
            default: kind = IngredientKind.Meat; state = IngredientProcessState.Raw; break;
        }
    }

    /// <summary>
    /// Serializuje lub deserializuje dane migawki przez bufor sieciowy.
    /// </summary>
    /// <typeparam name="T">Typ implementujący <see cref="Unity.Netcode.IReaderWriter"/>.</typeparam>
    /// <param name="serializer">Serializer bufora sieciowego.</param>
    /// <remarks>
    /// Optymalizuje transfer danych — sloty montażowe są serializowane
    /// tylko do wartości <see cref="assemblyCount"/> (maksymalnie 8),
    /// co zmniejsza rozmiar pakietu sieciowego.
    /// </remarks>
    public void NetworkSerialize<T>(Unity.Netcode.BufferSerializer<T> serializer) where T : Unity.Netcode.IReaderWriter
    {
        serializer.SerializeValue(ref stationIndex);
        serializer.SerializeValue(ref isProcessing);
        serializer.SerializeValue(ref remainingProcessTime);
        serializer.SerializeValue(ref preparedMeatServings);
        serializer.SerializeValue(ref hasLavash);
        serializer.SerializeValue(ref assemblyCount);
        serializer.SerializeValue(ref stationItem);

        int clampedCount = Mathf.Min(assemblyCount, 8);
        if (clampedCount > 0) { serializer.SerializeValue(ref assembly0Kind); serializer.SerializeValue(ref assembly0State); }
        if (clampedCount > 1) { serializer.SerializeValue(ref assembly1Kind); serializer.SerializeValue(ref assembly1State); }
        if (clampedCount > 2) { serializer.SerializeValue(ref assembly2Kind); serializer.SerializeValue(ref assembly2State); }
        if (clampedCount > 3) { serializer.SerializeValue(ref assembly3Kind); serializer.SerializeValue(ref assembly3State); }
        if (clampedCount > 4) { serializer.SerializeValue(ref assembly4Kind); serializer.SerializeValue(ref assembly4State); }
        if (clampedCount > 5) { serializer.SerializeValue(ref assembly5Kind); serializer.SerializeValue(ref assembly5State); }
        if (clampedCount > 6) { serializer.SerializeValue(ref assembly6Kind); serializer.SerializeValue(ref assembly6State); }
        if (clampedCount > 7) { serializer.SerializeValue(ref assembly7Kind); serializer.SerializeValue(ref assembly7State); }
    }
}
