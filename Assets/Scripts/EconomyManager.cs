/// \file EconomyManager.cs
/// \brief Plik zawierający klasę zarządzającą systemem ekonomii w grze Kebab Chef Symulator.
/// \details Definiuje klasę EconomyManager, która implementuje wzorzec Singleton
/// i odpowiada za śledzenie salda gracza, przetwarzanie transakcji finansowych
/// (zarabianie i wydawanie pieniędzy) oraz powiadamianie innych systemów
/// o zmianach stanu finansowego.

using UnityEngine;

/// <summary>
/// Klasa zarządzająca systemem ekonomii w grze.
/// </summary>
/// <remarks>
/// Implementuje wzorzec Singleton, zapewniając jedną globalną instancję menedżera ekonomii.
/// Odpowiada za śledzenie aktualnego salda gracza, łącznych zarobków i wydatków.
/// Powiadamia inne systemy o zmianach salda za pomocą zdarzenia <see cref="OnBalanceChanged"/>.
/// Współpracuje z <see cref="SaveManager"/> w celu oznaczania stanu gry jako wymagającego zapisu
/// po każdej transakcji finansowej.
/// </remarks>
public class EconomyManager : MonoBehaviour
{
    /// <summary>
    /// Statyczna instancja Singleton klasy <see cref="EconomyManager"/>.
    /// </summary>
    /// <value>Jedyna instancja menedżera ekonomii dostępna globalnie.</value>
    public static EconomyManager Instance { get; private set; }

    /// <summary>
    /// Zdarzenie wywoływane przy każdej zmianie salda gracza.
    /// </summary>
    /// <remarks>
    /// Przekazuje nową wartość salda jako parametr typu <see cref="float"/>.
    /// Subskrybenci mogą wykorzystać to zdarzenie do aktualizacji interfejsu użytkownika
    /// lub innych systemów zależnych od stanu finansowego gracza.
    /// </remarks>
    public event System.Action<float> OnBalanceChanged;

    /// <summary>
    /// Początkowa kwota pieniędzy, z jaką gracz rozpoczyna grę.
    /// </summary>
    /// <remarks>
    /// Wartość konfigurowana z poziomu inspektora Unity.
    /// Domyślnie ustawiona na 100 jednostek walutowych.
    /// </remarks>
    [SerializeField] private float startingMoney = 100f;

    /// <summary>
    /// Aktualne saldo gracza w grze.
    /// </summary>
    private float balance;

    /// <summary>
    /// Łączna kwota pieniędzy zarobionych przez gracza od początku gry.
    /// </summary>
    private float totalEarned;

    /// <summary>
    /// Łączna kwota pieniędzy wydanych przez gracza od początku gry.
    /// </summary>
    [SerializeField] private float totalSpent;

    /// <summary>
    /// Pobiera aktualne saldo gracza.
    /// </summary>
    /// <value>Bieżąca wartość salda jako liczba zmiennoprzecinkowa.</value>
    public float CurrentBalance => balance;

    /// <summary>
    /// Pobiera łączną kwotę zarobionych pieniędzy.
    /// </summary>
    /// <value>Suma wszystkich zarobków gracza od początku gry.</value>
    public float TotalEarned => totalEarned;

    /// <summary>
    /// Pobiera łączną kwotę wydanych pieniędzy.
    /// </summary>
    /// <value>Suma wszystkich wydatków gracza od początku gry.</value>
    public float TotalSpent => totalSpent;

    /// <summary>
    /// Inicjalizuje instancję Singleton przy starcie obiektu.
    /// </summary>
    /// <remarks>
    /// Jeśli instancja nie istnieje, ustawia ją na bieżący obiekt i inicjalizuje saldo
    /// wartością <see cref="startingMoney"/>. Jeśli instancja już istnieje,
    /// niszczy duplikat obiektu, zapewniając unikalność Singletona.
    /// </remarks>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            balance = startingMoney;
            return;
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Dodaje określoną kwotę pieniędzy do salda gracza.
    /// </summary>
    /// <param name="amount">Kwota do dodania. Musi być wartością dodatnią, w przeciwnym razie operacja jest ignorowana.</param>
    /// <remarks>
    /// Po dodaniu kwoty aktualizuje łączne zarobki, wywołuje zdarzenie <see cref="OnBalanceChanged"/>
    /// i oznacza stan gry jako wymagający zapisu w <see cref="SaveManager"/>.
    /// </remarks>
    public void AddMoney(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        balance += amount;
        totalEarned += amount;
        OnBalanceChanged?.Invoke(balance);
        if (SaveManager.Instance != null) SaveManager.Instance.MarkDirty();
    }

    /// <summary>
    /// Próbuje wydać określoną kwotę pieniędzy z salda gracza.
    /// </summary>
    /// <param name="amount">Kwota do wydania. Musi być wartością dodatnią.</param>
    /// <returns>
    /// <c>true</c> jeśli transakcja się powiodła (wystarczające saldo);
    /// <c>false</c> jeśli kwota jest niedodatnia lub saldo jest niewystarczające.
    /// </returns>
    /// <remarks>
    /// Weryfikuje, czy gracz posiada wystarczające środki przed dokonaniem transakcji.
    /// W przypadku powodzenia aktualizuje łączne wydatki, wywołuje zdarzenie <see cref="OnBalanceChanged"/>
    /// i oznacza stan gry jako wymagający zapisu.
    /// </remarks>
    public bool SpendMoney(float amount)
    {
        if (amount <= 0f)
        {
            return false;
        }

        if (balance < amount)
        {
            Debug.Log("Brak wystarczajacej ilosci pieniedzy.");
            return false;
        }

        balance -= amount;
        totalSpent += amount;
        OnBalanceChanged?.Invoke(balance);
        if (SaveManager.Instance != null) SaveManager.Instance.MarkDirty();
        return true;
    }

    /// <summary>
    /// Ustawia saldo i łączne zarobki na podstawie danych z sieci.
    /// </summary>
    /// <param name="newBalance">Nowa wartość salda otrzymana z serwera.</param>
    /// <param name="newTotalEarned">Nowa wartość łącznych zarobków otrzymana z serwera.</param>
    /// <remarks>
    /// Metoda wykorzystywana w trybie wieloosobowym do synchronizacji stanu ekonomii
    /// pomiędzy serwerem a klientami. Po aktualizacji wywołuje zdarzenie <see cref="OnBalanceChanged"/>.
    /// </remarks>
    public void SetBalanceFromNetwork(float newBalance, float newTotalEarned)
    {
        balance = newBalance;
        totalEarned = newTotalEarned;
        OnBalanceChanged?.Invoke(balance);
    }

    /// <summary>
    /// Przechwytuje bieżący stan ekonomii do struktury danych zapisu.
    /// </summary>
    /// <returns>
    /// Obiekt <see cref="EconomySaveData"/> zawierający aktualne saldo,
    /// łączne zarobki oraz łączne wydatki.
    /// </returns>
    /// <remarks>
    /// Używana przez <see cref="SaveManager"/> podczas zapisywania stanu gry do pliku.
    /// </remarks>
    public EconomySaveData CaptureState()
    {
        return new EconomySaveData
        {
            currentBalance = balance,
            totalEarned = totalEarned,
            totalSpent = totalSpent
        };
    }

    /// <summary>
    /// Przywraca stan ekonomii z wcześniej zapisanych danych.
    /// </summary>
    /// <param name="saveData">
    /// Obiekt <see cref="EconomySaveData"/> zawierający zapisany stan ekonomii.
    /// Jeśli <c>null</c>, metoda nie wykonuje żadnej operacji.
    /// </param>
    /// <remarks>
    /// Używana przez <see cref="SaveManager"/> podczas wczytywania stanu gry z pliku.
    /// Wszystkie wartości są zabezpieczone przed wartościami ujemnymi za pomocą <see cref="Mathf.Max"/>.
    /// Po przywróceniu stanu wywołuje zdarzenie <see cref="OnBalanceChanged"/>.
    /// </remarks>
    public void RestoreState(EconomySaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        balance = Mathf.Max(0f, saveData.currentBalance);
        totalEarned = Mathf.Max(0f, saveData.totalEarned);
        totalSpent = Mathf.Max(0f, saveData.totalSpent);
        OnBalanceChanged?.Invoke(balance);
    }

    /// <summary>
    /// Czyści referencję Singletona przy niszczeniu obiektu.
    /// </summary>
    /// <remarks>
    /// Zapobiega pozostawaniu nieaktualnych referencji po zniszczeniu obiektu menedżera.
    /// </remarks>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
