/// \file ShopManager.cs
/// \brief Plik zawierający system sklepu z ulepszeniami w grze Kebab Chef Symulator.
/// \details Definiuje typ wyliczeniowy UpgradeType, klasy danych ulepszeń (UpgradeDefinition,
/// ShopSaveData, UpgradeLevelEntry) oraz główną klasę ShopManager odpowiedzialną za
/// zarządzanie systemem ulepszeń, ich zakupem, śledzeniem postępu oraz serializacją stanu.

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Typ wyliczeniowy definiujący dostępne rodzaje ulepszeń w sklepie.
/// </summary>
/// <remarks>
/// Każdy typ ulepszenia odpowiada innemu aspektowi rozgrywki,
/// który gracz może usprawnić za pomocą zakupionych ulepszeń.
/// </remarks>
public enum UpgradeType
{
    /// <summary>
    /// Ulepszenie szybkości grillowania mięsa na donerze.
    /// Zmniejsza czas potrzebny na ściągnięcie mięsa z rożna.
    /// </summary>
    GrillSpeed,

    /// <summary>
    /// Ulepszenie szybkości krojenia warzyw na desce do krojenia.
    /// Zmniejsza czas potrzebny na pokrojenie składników.
    /// </summary>
    CuttingSpeed,

    /// <summary>
    /// Ulepszenie bonusu do nagrody za zamówienia.
    /// Zwiększa procentowo kwotę otrzymywaną za każde ukończone zamówienie.
    /// </summary>
    RewardBonus,

    /// <summary>
    /// Ulepszenie czasu na realizację zamówień.
    /// Dodaje dodatkowe sekundy na wykonanie każdego zamówienia.
    /// </summary>
    OrderTime,

    /// <summary>
    /// Ulepszenie wielkości porcji mięsa.
    /// Zwiększa liczbę porcji mięsa uzyskiwanych z jednego ścięcia.
    /// </summary>
    MeatBatchSize
}

/// <summary>
/// Klasa definiująca pojedyncze ulepszenie dostępne w sklepie.
/// </summary>
/// <remarks>
/// Przechowuje wszystkie informacje potrzebne do wyświetlenia ulepszenia w interfejsie użytkownika,
/// obliczenia kosztu na danym poziomie oraz opisu efektu ulepszenia.
/// Klasa jest serializowalna, co umożliwia konfigurację z poziomu inspektora Unity.
/// </remarks>
[Serializable]
public class UpgradeDefinition
{
    /// <summary>
    /// Typ ulepszenia określający, jaki aspekt rozgrywki jest ulepszany.
    /// </summary>
    public UpgradeType type;

    /// <summary>
    /// Nazwa wyświetlana ulepszenia widoczna w interfejsie sklepu.
    /// </summary>
    public string displayName;

    /// <summary>
    /// Tekstowy opis działania ulepszenia prezentowany graczowi.
    /// </summary>
    public string description;

    /// <summary>
    /// Znak lub symbol ikony reprezentującej ulepszenie w interfejsie.
    /// </summary>
    public string icon;

    /// <summary>
    /// Maksymalny poziom, do którego można ulepszyć dany element.
    /// </summary>
    public int maxLevel;

    /// <summary>
    /// Bazowy koszt pierwszego poziomu ulepszenia.
    /// </summary>
    public float baseCost;

    /// <summary>
    /// Współczynnik skalowania kosztu dla kolejnych poziomów.
    /// </summary>
    /// <remarks>
    /// Koszt kolejnego poziomu obliczany jest jako: bazowy koszt * (współczynnik ^ poziom).
    /// Wyższa wartość oznacza szybszy wzrost ceny ulepszeń.
    /// </remarks>
    public float costScaling;

    /// <summary>
    /// Kolor akcentu używany do wizualnego wyróżnienia ulepszenia w interfejsie sklepu.
    /// </summary>
    public Color accentColor;

    /// <summary>
    /// Tablica wartości efektów dla każdego poziomu ulepszenia.
    /// </summary>
    /// <remarks>
    /// Indeks tablicy odpowiada poziomowi ulepszenia (0 = brak ulepszenia, 1 = pierwszy poziom itd.).
    /// Interpretacja wartości zależy od typu ulepszenia (np. mnożnik czasu, bonus procentowy, liczba porcji).
    /// </remarks>
    public float[] effectValues;

    /// <summary>
    /// Oblicza koszt ulepszenia dla podanego następnego poziomu.
    /// </summary>
    /// <param name="nextLevel">Numer poziomu, na który gracz chce ulepszyć (0 oznacza pierwszy zakup).</param>
    /// <returns>
    /// Zaokrąglona kwota kosztu ulepszenia. Dla poziomu 0 lub mniejszego zwraca <see cref="baseCost"/>.
    /// </returns>
    /// <remarks>
    /// Koszt jest obliczany wykładniczo: baseCost * costScaling^nextLevel, a wynik jest zaokrąglany
    /// do najbliższej liczby całkowitej dla czytelności w interfejsie.
    /// </remarks>
    public float GetCostForLevel(int nextLevel)
    {
        if (nextLevel <= 0)
        {
            return baseCost;
        }

        return Mathf.Round(baseCost * Mathf.Pow(costScaling, nextLevel));
    }

    /// <summary>
    /// Generuje tekstowy opis efektu ulepszenia na następnym poziomie.
    /// </summary>
    /// <param name="currentLevel">Aktualny poziom ulepszenia.</param>
    /// <returns>
    /// Czytelny opis efektu ulepszenia (np. "-20% czasu", "+10% nagrody", "4 porcji")
    /// lub "MAX" jeśli ulepszenie osiągnęło maksymalny poziom.
    /// Zwraca pusty ciąg jeśli brak zdefiniowanych wartości efektów.
    /// </returns>
    /// <remarks>
    /// Format opisu zależy od typu ulepszenia:
    /// - GrillSpeed / CuttingSpeed: procent redukcji czasu
    /// - RewardBonus: procent bonusu do nagrody
    /// - OrderTime: dodatkowe sekundy
    /// - MeatBatchSize: liczba porcji
    /// </remarks>
    public string GetEffectDescription(int currentLevel)
    {
        if (effectValues == null || effectValues.Length == 0)
        {
            return string.Empty;
        }

        if (currentLevel >= maxLevel)
        {
            return "MAX";
        }

        int nextLevel = Mathf.Clamp(currentLevel + 1, 0, effectValues.Length - 1);

        switch (type)
        {
            case UpgradeType.GrillSpeed:
            case UpgradeType.CuttingSpeed:
                int percentFaster = Mathf.RoundToInt((1f - effectValues[nextLevel]) * 100f);
                return "-" + percentFaster + "% czasu";
            case UpgradeType.RewardBonus:
                int percentBonus = Mathf.RoundToInt(effectValues[nextLevel] * 100f);
                return "+" + percentBonus + "% nagrody";
            case UpgradeType.OrderTime:
                return "+" + effectValues[nextLevel] + " s";
            case UpgradeType.MeatBatchSize:
                return effectValues[nextLevel] + " porcji";
            default:
                return effectValues[nextLevel].ToString("F1");
        }
    }
}

/// <summary>
/// Klasa danych zapisu stanu sklepu z ulepszeniami.
/// </summary>
/// <remarks>
/// Serializowana struktura przechowująca poziomy wszystkich ulepszeń
/// oraz łączną liczbę zakupionych ulepszeń. Używana przez system zapisu/odczytu gry.
/// </remarks>
/// <seealso cref="SaveManager"/>
/// <seealso cref="ShopManager.CaptureState"/>
[Serializable]
public class ShopSaveData
{
    /// <summary>
    /// Lista wpisów zawierających typ i poziom każdego ulepszenia.
    /// </summary>
    public List<UpgradeLevelEntry> upgradeLevels = new List<UpgradeLevelEntry>();

    /// <summary>
    /// Łączna liczba ulepszeń zakupionych przez gracza od początku gry.
    /// </summary>
    public int totalUpgradesPurchased;
}

/// <summary>
/// Klasa reprezentująca pojedynczy wpis poziomu ulepszenia w danych zapisu.
/// </summary>
/// <remarks>
/// Przechowuje typ ulepszenia jako ciąg tekstowy (nazwa enum) oraz jego aktualny poziom.
/// Format tekstowy typu ulepszenia zapewnia kompatybilność z serializacją JSON.
/// </remarks>
[Serializable]
public class UpgradeLevelEntry
{
    /// <summary>
    /// Nazwa typu ulepszenia jako ciąg tekstowy (wartość <see cref="UpgradeType"/> skonwertowana na string).
    /// </summary>
    public string upgradeType;

    /// <summary>
    /// Aktualny poziom danego ulepszenia.
    /// </summary>
    public int level;
}

/// <summary>
/// Główna klasa zarządzająca systemem sklepu z ulepszeniami.
/// </summary>
/// <remarks>
/// Implementuje wzorzec Singleton. Odpowiada za:
/// - Rejestrację i przechowywanie definicji dostępnych ulepszeń
/// - Śledzenie aktualnych poziomów każdego ulepszenia
/// - Obsługę zakupu ulepszeń z weryfikacją salda gracza
/// - Dostarczanie wartości efektów ulepszeń innym systemom gry (grillowanie, krojenie, nagrody, czas zamówień, wielkość porcji)
/// - Serializację i deserializację stanu sklepu dla systemu zapisu gry
///
/// Współpracuje z <see cref="EconomyManager"/> (system finansów) oraz <see cref="SaveManager"/> (system zapisu).
/// </remarks>
public class ShopManager : MonoBehaviour
{
    /// <summary>
    /// Statyczna instancja Singleton klasy <see cref="ShopManager"/>.
    /// </summary>
    /// <value>Jedyna instancja menedżera sklepu dostępna globalnie.</value>
    public static ShopManager Instance { get; private set; }

    /// <summary>
    /// Słownik przechowujący definicje ulepszeń indeksowane typem ulepszenia.
    /// </summary>
    /// <remarks>
    /// Inicjalizowany w metodzie <see cref="InitializeDefinitions"/> podczas startu aplikacji.
    /// Umożliwia szybki dostęp do definicji ulepszenia po jego typie.
    /// </remarks>
    private readonly Dictionary<UpgradeType, UpgradeDefinition> definitions =
        new Dictionary<UpgradeType, UpgradeDefinition>();

    /// <summary>
    /// Aktualny poziom ulepszenia szybkości grillowania.
    /// </summary>
    private int grillSpeedLevel;

    /// <summary>
    /// Aktualny poziom ulepszenia szybkości krojenia.
    /// </summary>
    private int cuttingSpeedLevel;

    /// <summary>
    /// Aktualny poziom ulepszenia bonusu do nagrody.
    /// </summary>
    private int rewardBonusLevel;

    /// <summary>
    /// Aktualny poziom ulepszenia czasu na zamówienie.
    /// </summary>
    private int orderTimeLevel;

    /// <summary>
    /// Aktualny poziom ulepszenia wielkości porcji mięsa.
    /// </summary>
    private int meatBatchSizeLevel;

    /// <summary>
    /// Łączna liczba ulepszeń zakupionych przez gracza.
    /// </summary>
    /// <remarks>
    /// Wartość widoczna w inspektorze Unity w celach debugowania.
    /// </remarks>
    [SerializeField] private int totalUpgradesPurchased;

    /// <summary>
    /// Pobiera łączną liczbę zakupionych ulepszeń.
    /// </summary>
    /// <value>Całkowita liczba transakcji zakupu ulepszeń od początku gry.</value>
    public int TotalUpgradesPurchased => totalUpgradesPurchased;

    /// <summary>
    /// Zdarzenie wywoływane po pomyślnym zakupie ulepszenia.
    /// </summary>
    /// <remarks>
    /// Przekazuje typ zakupionego ulepszenia oraz nowy poziom.
    /// Subskrybenci mogą wykorzystać to zdarzenie do aktualizacji interfejsu
    /// lub zastosowania efektów ulepszenia w rozgrywce.
    /// </remarks>
    public event Action<UpgradeType, int> OnUpgradePurchased;

    /// <summary>
    /// Inicjalizuje instancję Singleton oraz definicje ulepszeń.
    /// </summary>
    /// <remarks>
    /// Jeśli instancja nie istnieje, ustawia ją i wywołuje <see cref="InitializeDefinitions"/>.
    /// Jeśli instancja już istnieje, niszczy duplikat obiektu.
    /// </remarks>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeDefinitions();
            return;
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Obsługuje zmianę poziomu ulepszenia, wywołując zdarzenie <see cref="OnUpgradePurchased"/>.
    /// </summary>
    /// <param name="type">Typ ulepszenia, którego poziom został zmieniony.</param>
    /// <param name="newLevel">Nowy poziom ulepszenia po zmianie.</param>
    private void HandleLevelChanged(UpgradeType type, int newLevel)
    {
        OnUpgradePurchased?.Invoke(type, newLevel);
    }

    /// <summary>
    /// Inicjalizuje wszystkie definicje ulepszeń dostępnych w sklepie.
    /// </summary>
    /// <remarks>
    /// Rejestruje pięć typów ulepszeń z predefiniowanymi parametrami:
    /// - Szybszy Doner (GrillSpeed): 4 poziomy, redukcja czasu grillowania
    /// - Szybsze Krojenie (CuttingSpeed): 4 poziomy, redukcja czasu krojenia
    /// - Lepsza Reputacja (RewardBonus): 5 poziomów, bonus do nagrody
    /// - Więcej Czasu (OrderTime): 4 poziomy, dodatkowy czas na zamówienia
    /// - Większa Porcja (MeatBatchSize): 3 poziomy, więcej porcji mięsa
    /// </remarks>
    private void InitializeDefinitions()
    {
        RegisterDefinition(new UpgradeDefinition
        {
            type = UpgradeType.GrillSpeed,
            displayName = "Szybszy Doner",
            description = "Przyspiesza scinanie miesa z donera.",
            icon = "G",
            maxLevel = 4,
            baseCost = 45f,
            costScaling = 1.9f,
            accentColor = new Color(0.92f, 0.55f, 0.18f),
            effectValues = new float[] { 1.0f, 0.82f, 0.66f, 0.52f, 0.40f }
        });

        RegisterDefinition(new UpgradeDefinition
        {
            type = UpgradeType.CuttingSpeed,
            displayName = "Szybsze Krojenie",
            description = "Przyspiesza krojenie warzyw na desce.",
            icon = "K",
            maxLevel = 4,
            baseCost = 35f,
            costScaling = 1.85f,
            accentColor = new Color(0.25f, 0.78f, 0.55f),
            effectValues = new float[] { 1.0f, 0.80f, 0.64f, 0.50f, 0.38f }
        });

        RegisterDefinition(new UpgradeDefinition
        {
            type = UpgradeType.RewardBonus,
            displayName = "Lepsza Reputacja",
            description = "Zwieksza nagrode za kazde zamowienie.",
            icon = "$",
            maxLevel = 5,
            baseCost = 60f,
            costScaling = 2.0f,
            accentColor = new Color(1f, 0.82f, 0.28f),
            effectValues = new float[] { 0f, 0.10f, 0.22f, 0.36f, 0.52f, 0.70f }
        });

        RegisterDefinition(new UpgradeDefinition
        {
            type = UpgradeType.OrderTime,
            displayName = "Wiecej Czasu",
            description = "Dodaje czas na realizacje zamowien.",
            icon = "T",
            maxLevel = 4,
            baseCost = 40f,
            costScaling = 1.8f,
            accentColor = new Color(0.40f, 0.68f, 0.95f),
            effectValues = new float[] { 0f, 12f, 25f, 40f, 60f }
        });

        RegisterDefinition(new UpgradeDefinition
        {
            type = UpgradeType.MeatBatchSize,
            displayName = "Wieksza Porcja",
            description = "Wiecej porcji miesa z jednego sciecia.",
            icon = "M",
            maxLevel = 3,
            baseCost = 55f,
            costScaling = 2.1f,
            accentColor = new Color(0.82f, 0.32f, 0.22f),
            effectValues = new float[] { 3f, 4f, 6f, 8f }
        });
    }

    /// <summary>
    /// Rejestruje definicję ulepszenia w słowniku definicji.
    /// </summary>
    /// <param name="definition">Obiekt <see cref="UpgradeDefinition"/> do zarejestrowania.</param>
    /// <remarks>
    /// Jeśli definicja o tym samym typie już istnieje, zostanie nadpisana.
    /// </remarks>
    private void RegisterDefinition(UpgradeDefinition definition)
    {
        definitions[definition.type] = definition;
    }

    /// <summary>
    /// Pobiera listę wszystkich definicji ulepszeń w ustalonej kolejności.
    /// </summary>
    /// <returns>
    /// Lista obiektów <see cref="UpgradeDefinition"/> zawierająca wszystkie dostępne ulepszenia
    /// w kolejności: GrillSpeed, CuttingSpeed, RewardBonus, OrderTime, MeatBatchSize.
    /// </returns>
    /// <remarks>
    /// Kolejność elementów na liście determinuje kolejność wyświetlania w interfejsie sklepu.
    /// </remarks>
    public List<UpgradeDefinition> GetAllDefinitions()
    {
        List<UpgradeDefinition> list = new List<UpgradeDefinition>();
        list.Add(GetDefinition(UpgradeType.GrillSpeed));
        list.Add(GetDefinition(UpgradeType.CuttingSpeed));
        list.Add(GetDefinition(UpgradeType.RewardBonus));
        list.Add(GetDefinition(UpgradeType.OrderTime));
        list.Add(GetDefinition(UpgradeType.MeatBatchSize));
        return list;
    }

    /// <summary>
    /// Pobiera definicję ulepszenia na podstawie jego typu.
    /// </summary>
    /// <param name="type">Typ ulepszenia do wyszukania.</param>
    /// <returns>
    /// Obiekt <see cref="UpgradeDefinition"/> odpowiadający podanemu typowi
    /// lub <c>null</c> jeśli definicja nie została znaleziona.
    /// </returns>
    public UpgradeDefinition GetDefinition(UpgradeType type)
    {
        UpgradeDefinition definition;
        definitions.TryGetValue(type, out definition);
        return definition;
    }

    /// <summary>
    /// Pobiera aktualny poziom wskazanego ulepszenia.
    /// </summary>
    /// <param name="type">Typ ulepszenia, którego poziom ma zostać zwrócony.</param>
    /// <returns>
    /// Aktualny poziom ulepszenia jako liczba całkowita.
    /// Zwraca 0 dla nieznanych typów ulepszeń.
    /// </returns>
    public int GetUpgradeLevel(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.GrillSpeed: return grillSpeedLevel;
            case UpgradeType.CuttingSpeed: return cuttingSpeedLevel;
            case UpgradeType.RewardBonus: return rewardBonusLevel;
            case UpgradeType.OrderTime: return orderTimeLevel;
            case UpgradeType.MeatBatchSize: return meatBatchSizeLevel;
            default: return 0;
        }
    }

    /// <summary>
    /// Ustawia poziom wskazanego ulepszenia na podaną wartość.
    /// </summary>
    /// <param name="type">Typ ulepszenia, którego poziom ma zostać zmieniony.</param>
    /// <param name="level">Nowa wartość poziomu do ustawienia.</param>
    /// <remarks>
    /// Metoda nie weryfikuje, czy podany poziom mieści się w dozwolonym zakresie.
    /// Walidacja powinna być przeprowadzona przez kod wywołujący.
    /// </remarks>
    public void SetUpgradeLevel(UpgradeType type, int level)
    {
        switch (type)
        {
            case UpgradeType.GrillSpeed: grillSpeedLevel = level; break;
            case UpgradeType.CuttingSpeed: cuttingSpeedLevel = level; break;
            case UpgradeType.RewardBonus: rewardBonusLevel = level; break;
            case UpgradeType.OrderTime: orderTimeLevel = level; break;
            case UpgradeType.MeatBatchSize: meatBatchSizeLevel = level; break;
        }
    }

    /// <summary>
    /// Sprawdza, czy wskazane ulepszenie osiągnęło maksymalny poziom.
    /// </summary>
    /// <param name="type">Typ ulepszenia do sprawdzenia.</param>
    /// <returns>
    /// <c>true</c> jeśli ulepszenie jest na maksymalnym poziomie lub definicja nie istnieje;
    /// <c>false</c> w przeciwnym razie.
    /// </returns>
    public bool IsMaxLevel(UpgradeType type)
    {
        UpgradeDefinition definition = GetDefinition(type);
        if (definition == null)
        {
            return true;
        }

        return GetUpgradeLevel(type) >= definition.maxLevel;
    }

    /// <summary>
    /// Oblicza koszt następnego poziomu wskazanego ulepszenia.
    /// </summary>
    /// <param name="type">Typ ulepszenia, dla którego obliczany jest koszt.</param>
    /// <returns>
    /// Koszt kolejnego poziomu ulepszenia. Zwraca <see cref="float.MaxValue"/> jeśli definicja nie istnieje,
    /// lub 0 jeśli ulepszenie osiągnęło już maksymalny poziom.
    /// </returns>
    public float GetNextUpgradeCost(UpgradeType type)
    {
        UpgradeDefinition definition = GetDefinition(type);
        if (definition == null)
        {
            return float.MaxValue;
        }

        int currentLevel = GetUpgradeLevel(type);
        if (currentLevel >= definition.maxLevel)
        {
            return 0f;
        }

        return definition.GetCostForLevel(currentLevel);
    }

    /// <summary>
    /// Sprawdza, czy gracz może sobie pozwolić na zakup następnego poziomu ulepszenia.
    /// </summary>
    /// <param name="type">Typ ulepszenia do sprawdzenia.</param>
    /// <returns>
    /// <c>true</c> jeśli ulepszenie nie jest na maksymalnym poziomie i gracz posiada wystarczające środki;
    /// <c>false</c> w przeciwnym razie.
    /// </returns>
    /// <remarks>
    /// Wymaga dostępności instancji <see cref="EconomyManager"/>. Zwraca <c>false</c> jeśli
    /// menedżer ekonomii nie jest dostępny.
    /// </remarks>
    public bool CanAffordUpgrade(UpgradeType type)
    {
        if (IsMaxLevel(type))
        {
            return false;
        }

        if (EconomyManager.Instance == null)
        {
            return false;
        }

        return EconomyManager.Instance.CurrentBalance >= GetNextUpgradeCost(type);
    }

    /// <summary>
    /// Próbuje zakupić następny poziom wskazanego ulepszenia.
    /// </summary>
    /// <param name="type">Typ ulepszenia do zakupienia.</param>
    /// <returns>
    /// <c>true</c> jeśli zakup się powiódł;
    /// <c>false</c> jeśli ulepszenie jest na maksymalnym poziomie, brak menedżera ekonomii
    /// lub niewystarczające saldo.
    /// </returns>
    /// <remarks>
    /// Metoda wykonuje pełny proces zakupu:
    /// 1. Sprawdza czy ulepszenie nie osiągnęło maksymalnego poziomu
    /// 2. Weryfikuje dostępność <see cref="EconomyManager"/>
    /// 3. Próbuje pobrać koszt z salda gracza
    /// 4. Zwiększa poziom ulepszenia
    /// 5. Inkrementuje licznik zakupionych ulepszeń
    /// 6. Loguje informację o zakupie
    /// 7. Wywołuje zdarzenie zmiany poziomu
    /// 8. Zapisuje stan gry przez <see cref="SaveManager"/>
    /// </remarks>
    public bool TryPurchaseUpgrade(UpgradeType type)
    {
        if (IsMaxLevel(type))
        {
            return false;
        }

        if (EconomyManager.Instance == null)
        {
            return false;
        }

        float cost = GetNextUpgradeCost(type);
        if (!EconomyManager.Instance.SpendMoney(cost))
        {
            return false;
        }

        int newLevel = GetUpgradeLevel(type) + 1;
        SetUpgradeLevel(type, newLevel);

        totalUpgradesPurchased++;

        Debug.Log("Zakupiono ulepszenie: " + type + " -> poziom " + newLevel + " za " + cost + " zl.");

        HandleLevelChanged(type, newLevel);
        SaveManager.Instance?.SaveGame();

        return true;
    }

    /// <summary>
    /// Pobiera mnożnik szybkości przetwarzania dla wskazanego typu stacji kuchennej.
    /// </summary>
    /// <param name="stationType">Typ stacji kuchennej (Grill lub CuttingBoard).</param>
    /// <returns>
    /// Mnożnik szybkości przetwarzania (wartość &lt; 1.0 oznacza szybsze przetwarzanie).
    /// Zwraca 1.0 dla nieobsługiwanych typów stacji (brak modyfikacji szybkości).
    /// </returns>
    /// <remarks>
    /// Mapuje typ stacji kuchennej na odpowiedni typ ulepszenia:
    /// - <c>Grill</c> → <see cref="UpgradeType.GrillSpeed"/>
    /// - <c>CuttingBoard</c> → <see cref="UpgradeType.CuttingSpeed"/>
    /// </remarks>
    public float GetProcessingSpeedMultiplier(KitchenStationType stationType)
    {
        UpgradeType upgradeType;
        if (stationType == KitchenStationType.Grill)
        {
            upgradeType = UpgradeType.GrillSpeed;
        }
        else if (stationType == KitchenStationType.CuttingBoard)
        {
            upgradeType = UpgradeType.CuttingSpeed;
        }
        else
        {
            return 1f;
        }

        return GetEffectValue(upgradeType, 1f);
    }

    /// <summary>
    /// Pobiera mnożnik nagrody za zamówienia uwzględniający ulepszenie reputacji.
    /// </summary>
    /// <returns>
    /// Mnożnik nagrody (1.0 = brak bonusu, 1.5 = +50% nagrody itd.).
    /// Wartość bazowa 1.0 jest powiększana o wartość efektu ulepszenia RewardBonus.
    /// </returns>
    public float GetRewardMultiplier()
    {
        return 1f + GetEffectValue(UpgradeType.RewardBonus, 0f);
    }

    /// <summary>
    /// Pobiera bonus czasu dodawanego do limitu zamówień.
    /// </summary>
    /// <returns>
    /// Dodatkowy czas w sekundach przyznawany na realizację zamówień.
    /// Zwraca 0 jeśli ulepszenie nie zostało zakupione.
    /// </returns>
    public float GetOrderTimeBonus()
    {
        return GetEffectValue(UpgradeType.OrderTime, 0f);
    }

    /// <summary>
    /// Pobiera aktualną wielkość porcji mięsa uzyskiwanej z jednego ścięcia.
    /// </summary>
    /// <returns>
    /// Liczba porcji mięsa jako liczba całkowita.
    /// Domyślnie 3 porcje bez ulepszeń.
    /// </returns>
    public int GetMeatBatchSize()
    {
        return Mathf.RoundToInt(GetEffectValue(UpgradeType.MeatBatchSize, 3f));
    }

    /// <summary>
    /// Pobiera wartość efektu ulepszenia na aktualnym poziomie.
    /// </summary>
    /// <param name="type">Typ ulepszenia, którego wartość efektu ma zostać pobrana.</param>
    /// <param name="defaultValue">Wartość domyślna zwracana w przypadku braku definicji lub nieprawidłowego poziomu.</param>
    /// <returns>
    /// Wartość efektu na aktualnym poziomie ulepszenia lub <paramref name="defaultValue"/>
    /// jeśli definicja nie istnieje, tablica wartości jest pusta, lub poziom jest poza zakresem.
    /// </returns>
    private float GetEffectValue(UpgradeType type, float defaultValue)
    {
        UpgradeDefinition definition = GetDefinition(type);
        if (definition == null || definition.effectValues == null)
        {
            return defaultValue;
        }

        int level = GetUpgradeLevel(type);
        if (level < 0 || level >= definition.effectValues.Length)
        {
            return defaultValue;
        }

        return definition.effectValues[level];
    }

    /// <summary>
    /// Przechwytuje bieżący stan sklepu do struktury danych zapisu.
    /// </summary>
    /// <returns>
    /// Obiekt <see cref="ShopSaveData"/> zawierający poziomy wszystkich ulepszeń
    /// oraz łączną liczbę zakupionych ulepszeń.
    /// </returns>
    /// <remarks>
    /// Zapisuje tylko ulepszenia o poziomie większym od 0, pomijając niezakupione.
    /// Używana przez <see cref="SaveManager"/> podczas zapisywania stanu gry.
    /// </remarks>
    public ShopSaveData CaptureState()
    {
        ShopSaveData data = new ShopSaveData();
        data.totalUpgradesPurchased = totalUpgradesPurchased;

        foreach (UpgradeType type in Enum.GetValues(typeof(UpgradeType)))
        {
            int level = GetUpgradeLevel(type);
            if (level <= 0)
            {
                continue;
            }

            data.upgradeLevels.Add(new UpgradeLevelEntry
            {
                upgradeType = type.ToString(),
                level = level
            });
        }

        return data;
    }

    /// <summary>
    /// Przywraca stan sklepu z wcześniej zapisanych danych.
    /// </summary>
    /// <param name="data">
    /// Obiekt <see cref="ShopSaveData"/> zawierający zapisany stan sklepu.
    /// Jeśli <c>null</c>, metoda nie wykonuje żadnej operacji.
    /// </param>
    /// <remarks>
    /// Parsuje nazwy typów ulepszeń z formatu tekstowego z powrotem na wartości enum.
    /// Nieznane typy ulepszeń są pomijane (obsługa kompatybilności wstecznej).
    /// Poziomy ulepszeń są ograniczane do zakresu [0, maxLevel] odpowiedniej definicji.
    /// Używana przez <see cref="SaveManager"/> podczas wczytywania stanu gry.
    /// </remarks>
    public void RestoreState(ShopSaveData data)
    {
        if (data == null)
        {
            return;
        }

        totalUpgradesPurchased = Mathf.Max(0, data.totalUpgradesPurchased);

        foreach (UpgradeLevelEntry entry in data.upgradeLevels)
        {
            UpgradeType parsedType;
            try
            {
                parsedType = (UpgradeType)Enum.Parse(typeof(UpgradeType), entry.upgradeType);
            }
            catch
            {
                continue;
            }

            UpgradeDefinition definition = GetDefinition(parsedType);
            int maxAllowed = definition != null ? definition.maxLevel : 10;
            int restoredLevel = Mathf.Clamp(entry.level, 0, maxAllowed);
            SetUpgradeLevel(parsedType, restoredLevel);
        }
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
