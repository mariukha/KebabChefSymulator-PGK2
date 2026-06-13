/// \file IngredientData.cs
/// \brief Plik zawierający definicję klasy ScriptableObject przechowującej dane składnika.
/// \details Definiuje asset danych składnika używany w systemie kuchni gry kebab.
///          Umożliwia tworzenie nowych składników z poziomu menu Unity (KebabGame/Skladnik).

using UnityEngine;

/// <summary>
/// Klasa ScriptableObject reprezentująca dane pojedynczego składnika w grze.
/// Przechowuje informacje o nazwie, cenie, ikonie, modelu 3D oraz parametrach
/// gameplayowych składnika, takich jak typ, stan początkowy i wartość sprzedaży.
/// </summary>
/// <remarks>
/// Nowe instancje można tworzyć z poziomu menu Unity:
/// <c>Assets → Create → KebabGame → Skladnik</c>.
/// Każdy składnik posiada zarówno dane prezentacyjne (ikona, model 3D),
/// jak i dane logiki gry (typ, stan, wartość).
/// </remarks>
[CreateAssetMenu(fileName = "NowySkladnik", menuName = "KebabGame/Skladnik")]
public class IngredientData : ScriptableObject
{
    /// <summary>
    /// Nazwa składnika wyświetlana w interfejsie użytkownika.
    /// </summary>
    [Header("Istniejace pola assetu")]
    public string nazwaSkladnika;

    /// <summary>
    /// Cena zakupu składnika w walucie gry.
    /// Określa, ile gracz musi zapłacić za nabycie tego składnika.
    /// </summary>
    public float cenaZakupu;

    /// <summary>
    /// Ikona sprite'a składnika używana w interfejsie użytkownika.
    /// Wyświetlana m.in. w menu zamówień i panelu ekwipunku.
    /// </summary>
    public Sprite ikona;

    /// <summary>
    /// Prefab modelu 3D składnika używany do wizualizacji w scenie gry.
    /// Instancjonowany na stacjach kuchennych i w trakcie przygotowywania potraw.
    /// </summary>
    public GameObject model3D;

    /// <summary>
    /// Typ (rodzaj) składnika określający jego kategorię w systemie kuchni.
    /// </summary>
    /// <seealso cref="IngredientKind"/>
    [Header("Rozszerzone dane gameplayowe")]
    public IngredientKind typSkladnika = IngredientKind.Tomato;

    /// <summary>
    /// Początkowy stan przetworzenia składnika po jego pobraniu ze źródła.
    /// Określa, w jakim stanie składnik zaczyna swoją „drogę" w kuchni.
    /// </summary>
    /// <seealso cref="IngredientProcessState"/>
    public IngredientProcessState stanPoczatkowy = IngredientProcessState.Raw;

    /// <summary>
    /// Wartość sprzedaży składnika w walucie gry.
    /// Wpływa na końcową wartość potrawy zawierającej ten składnik.
    /// </summary>
    public float wartoscSprzedazy = 5f;

    /// <summary>
    /// Kolor debugowania używany do wizualizacji składnika w trybie deweloperskim.
    /// Domyślnie ustawiony na biały.
    /// </summary>
    public Color kolorDebug = Color.white;

    /// <summary>
    /// Zwraca nazwę wyświetlaną składnika.
    /// Jeśli pole <see cref="nazwaSkladnika"/> jest puste lub zawiera same białe znaki,
    /// zwracana jest nazwa obiektu ScriptableObject (właściwość <c>name</c>).
    /// </summary>
    /// <value>Nazwa składnika przeznaczona do wyświetlenia w interfejsie użytkownika.</value>
    public string DisplayName
    {
        get
        {
            return string.IsNullOrWhiteSpace(nazwaSkladnika) ? name : nazwaSkladnika;
        }
    }
}
