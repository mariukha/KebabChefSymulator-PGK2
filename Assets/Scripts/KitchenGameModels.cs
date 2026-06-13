/// \file KitchenGameModels.cs
/// \brief Plik zawierający modele danych i typy wyliczeniowe systemu kuchni gry kebab.
/// \details Definiuje kluczowe struktury danych używane w całym systemie kuchni:
///          typy składników, stany przetworzenia, typy stacji kuchennych,
///          klasy reprezentujące wymagania składników, przygotowane składniki,
///          przedmioty kuchenne oraz klasy pomocnicze do walidacji zamówień
///          i nazewnictwa składników w języku polskim.

using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Typ wyliczeniowy określający rodzaj składnika w systemie kuchni.
/// Każda wartość odpowiada konkretnemu produktowi spożywczemu
/// wykorzystywanemu do przygotowania kebaba.
/// </summary>
public enum IngredientKind
{
    /// <summary>
    /// Mięso — główny składnik białkowy kebaba.
    /// </summary>
    Meat,

    /// <summary>
    /// Pomidor — składnik warzywny dodawany jako dodatek.
    /// </summary>
    Tomato,

    /// <summary>
    /// Cebula — składnik warzywny nadający smak.
    /// </summary>
    Onion,

    /// <summary>
    /// Sałata — składnik warzywny dodawany jako świeży dodatek.
    /// </summary>
    Lettuce,

    /// <summary>
    /// Sos czosnkowy — sos dodawany jako przyprawienie.
    /// </summary>
    GarlicSauce,

    /// <summary>
    /// Lawasz — cienki chleb płaski stanowiący bazę (owijkę) kebaba.
    /// </summary>
    Lavash,

    /// <summary>
    /// Kebab — gotowa potrawa złożona z wielu składników.
    /// </summary>
    Kebab
}

/// <summary>
/// Typ wyliczeniowy określający stan przetworzenia składnika.
/// Reprezentuje etapy przygotowania, przez które przechodzi składnik
/// na drodze od surowego produktu do gotowej potrawy.
/// </summary>
public enum IngredientProcessState
{
    /// <summary>
    /// Surowy — składnik w stanie nieprzetworzonym, bezpośrednio ze źródła.
    /// </summary>
    Raw,

    /// <summary>
    /// Pokrojony — składnik po przetworzeniu na desce do krojenia.
    /// </summary>
    Chopped,

    /// <summary>
    /// Upieczony — składnik po obróbce termicznej na grillu.
    /// </summary>
    Cooked,

    /// <summary>
    /// Złożony — składnik wchodzący w skład gotowej potrawy.
    /// </summary>
    Assembled
}

/// <summary>
/// Typ wyliczeniowy określający rodzaj stacji kuchennej.
/// Każda stacja pełni określoną rolę w procesie przygotowania potrawy.
/// </summary>
public enum KitchenStationType
{
    /// <summary>
    /// Źródło składników — stacja, z której gracz pobiera surowe składniki.
    /// </summary>
    IngredientSource,

    /// <summary>
    /// Deska do krojenia — stacja do krojenia składników (zmiana stanu na <see cref="IngredientProcessState.Chopped"/>).
    /// </summary>
    CuttingBoard,

    /// <summary>
    /// Grill — stacja do pieczenia składników (zmiana stanu na <see cref="IngredientProcessState.Cooked"/>).
    /// </summary>
    Grill,

    /// <summary>
    /// Stacja montażu — miejsce składania gotowej potrawy z przygotowanych składników.
    /// </summary>
    Assembly,

    /// <summary>
    /// Stacja wydawania — punkt dostarczania gotowej potrawy klientowi.
    /// </summary>
    Delivery
}

/// <summary>
/// Klasa reprezentująca wymaganie dotyczące pojedynczego składnika w zamówieniu.
/// Określa, jaki rodzaj składnika jest potrzebny, w jakim stanie przetworzenia
/// oraz w jakiej ilości.
/// </summary>
/// <remarks>
/// Klasa jest serializowalna, co umożliwia edycję wymagań zamówienia
/// bezpośrednio w Inspektorze Unity. Wymagania składników są porównywane
/// z zawartością gotowej potrawy przez klasę <see cref="KitchenOrderValidator"/>
/// w celu weryfikacji poprawności realizacji zamówienia.
/// </remarks>
/// <seealso cref="Order"/>
/// <seealso cref="KitchenOrderValidator"/>
[Serializable]
public class IngredientRequirement
{
    /// <summary>
    /// Rodzaj wymaganego składnika.
    /// </summary>
    public IngredientKind ingredientKind = IngredientKind.Meat;

    /// <summary>
    /// Wymagany stan przetworzenia składnika (np. surowy, pokrojony, upieczony).
    /// </summary>
    public IngredientProcessState requiredState = IngredientProcessState.Raw;

    /// <summary>
    /// Wymagana ilość tego składnika w zamówieniu. Domyślnie 1.
    /// </summary>
    public int quantity = 1;

    /// <summary>
    /// Konstruktor domyślny. Tworzy wymaganie z wartościami domyślnymi.
    /// </summary>
    public IngredientRequirement()
    {
    }

    /// <summary>
    /// Konstruktor parametryczny tworzący wymaganie z określonym rodzajem,
    /// stanem przetworzenia i opcjonalną ilością składnika.
    /// </summary>
    /// <param name="ingredientKind">Rodzaj wymaganego składnika.</param>
    /// <param name="requiredState">Wymagany stan przetworzenia składnika.</param>
    /// <param name="quantity">Ilość wymaganego składnika (domyślnie 1).</param>
    public IngredientRequirement(
        IngredientKind ingredientKind,
        IngredientProcessState requiredState,
        int quantity = 1)
    {
        this.ingredientKind = ingredientKind;
        this.requiredState = requiredState;
        this.quantity = quantity;
    }

    /// <summary>
    /// Tworzy czytelny dla człowieka opis wymagania składnika.
    /// Dla ilości większej niż 1 dodaje prefiks z ilością (np. "2x Pomidor").
    /// </summary>
    /// <returns>Sformatowany tekst opisujący wymaganie składnika.</returns>
    public string ToDisplayString()
    {
        string formattedName = KitchenNaming.FormatIngredient(ingredientKind, requiredState);

        if (quantity <= 1)
        {
            return formattedName;
        }

        return quantity + "x " + formattedName;
    }
}

/// <summary>
/// Klasa reprezentująca dane przygotowanego składnika.
/// Przechowuje informację o rodzaju składnika oraz jego aktualnym stanie przetworzenia.
/// Używana jako element listy zawartości gotowej potrawy (<see cref="KitchenItem.contents"/>).
/// </summary>
/// <remarks>
/// Klasa jest serializowalna, co umożliwia jej zapis i przesyłanie przez sieć.
/// W przeciwieństwie do <see cref="IngredientRequirement"/>, ta klasa nie przechowuje
/// ilości — każda instancja reprezentuje dokładnie jeden egzemplarz składnika.
/// </remarks>
[Serializable]
public class PreparedIngredientData
{
    /// <summary>
    /// Rodzaj przygotowanego składnika.
    /// </summary>
    public IngredientKind ingredientKind = IngredientKind.Meat;

    /// <summary>
    /// Aktualny stan przetworzenia składnika (np. surowy, pokrojony, upieczony).
    /// </summary>
    public IngredientProcessState state = IngredientProcessState.Raw;

    /// <summary>
    /// Konstruktor domyślny. Tworzy dane składnika z wartościami domyślnymi.
    /// </summary>
    public PreparedIngredientData()
    {
    }

    /// <summary>
    /// Konstruktor parametryczny tworzący dane przygotowanego składnika
    /// z określonym rodzajem i stanem przetworzenia.
    /// </summary>
    /// <param name="ingredientKind">Rodzaj składnika.</param>
    /// <param name="state">Stan przetworzenia składnika.</param>
    public PreparedIngredientData(IngredientKind ingredientKind, IngredientProcessState state)
    {
        this.ingredientKind = ingredientKind;
        this.state = state;
    }

    /// <summary>
    /// Tworzy czytelny dla człowieka opis przygotowanego składnika.
    /// Wykorzystuje klasę <see cref="KitchenNaming"/> do formatowania nazwy.
    /// </summary>
    /// <returns>Sformatowany tekst z nazwą składnika i jego stanem przetworzenia.</returns>
    public string ToDisplayString()
    {
        return KitchenNaming.FormatIngredient(ingredientKind, state);
    }
}

/// <summary>
/// Klasa reprezentująca przedmiot kuchenny — pojedynczy składnik lub gotową potrawę.
/// Może przechowywać listę zawartych składników w przypadku potrawy złożonej (np. kebab).
/// Jest centralnym obiektem danych przechodzącym przez stacje kuchenne.
/// </summary>
/// <remarks>
/// Pole <see cref="isDish"/> rozróżnia między pojedynczym składnikiem a gotową potrawą.
/// Gotowa potrawa zawiera listę <see cref="contents"/> z przygotowanymi składnikami,
/// która jest porównywana z wymaganiami zamówienia przez <see cref="KitchenOrderValidator"/>.
/// </remarks>
/// <seealso cref="PreparedIngredientData"/>
/// <seealso cref="IngredientData"/>
[Serializable]
public class KitchenItem
{
    /// <summary>
    /// Nazwa przedmiotu kuchennego wyświetlana w interfejsie użytkownika.
    /// </summary>
    public string itemName;

    /// <summary>
    /// Rodzaj składnika, z którego wywodzi się ten przedmiot.
    /// Dla potraw złożonych określa główny typ (np. Kebab).
    /// </summary>
    public IngredientKind ingredientKind = IngredientKind.Meat;

    /// <summary>
    /// Aktualny stan przetworzenia przedmiotu kuchennego.
    /// </summary>
    public IngredientProcessState state = IngredientProcessState.Raw;

    /// <summary>
    /// Flaga określająca, czy przedmiot jest gotową potrawą (daniem).
    /// Wartość <c>true</c> oznacza potrawę złożoną z wielu składników,
    /// <c>false</c> — pojedynczy składnik.
    /// </summary>
    public bool isDish;

    /// <summary>
    /// Szacowana wartość przedmiotu w walucie gry.
    /// Dla gotowych potraw może być sumą wartości poszczególnych składników.
    /// </summary>
    public float estimatedValue;

    /// <summary>
    /// Lista przygotowanych składników wchodzących w skład potrawy.
    /// Wypełniana tylko dla potraw (<see cref="isDish"/> = <c>true</c>).
    /// </summary>
    public List<PreparedIngredientData> contents = new List<PreparedIngredientData>();

    /// <summary>
    /// Tworzy nowy przedmiot kuchenny na podstawie danych składnika z assetu <see cref="IngredientData"/>.
    /// Metoda fabryczna konwertująca dane ScriptableObject na obiekt runtime.
    /// </summary>
    /// <param name="data">
    /// Dane składnika (asset ScriptableObject). Jeśli <c>null</c>,
    /// tworzony jest przedmiot z wartościami domyślnymi.
    /// </param>
    /// <returns>Nowa instancja <see cref="KitchenItem"/> zainicjalizowana danymi ze składnika.</returns>
    public static KitchenItem FromIngredient(IngredientData data)
    {
        return new KitchenItem
        {
            itemName = data != null ? data.DisplayName : "Skladnik",
            ingredientKind = data != null ? data.typSkladnika : IngredientKind.Tomato,
            state = data != null ? data.stanPoczatkowy : IngredientProcessState.Raw,
            estimatedValue = data != null ? data.wartoscSprzedazy : 0f
        };
    }

    /// <summary>
    /// Tworzy głęboką kopię przedmiotu kuchennego, włącznie z listą zawartych składników.
    /// Nowy obiekt jest całkowicie niezależny od oryginału.
    /// </summary>
    /// <returns>Nowa instancja <see cref="KitchenItem"/> będąca pełną kopią bieżącego przedmiotu.</returns>
    public KitchenItem Clone()
    {
        KitchenItem copy = new KitchenItem
        {
            itemName = itemName,
            ingredientKind = ingredientKind,
            state = state,
            isDish = isDish,
            estimatedValue = estimatedValue
        };

        foreach (PreparedIngredientData ingredient in contents)
        {
            copy.contents.Add(new PreparedIngredientData(ingredient.ingredientKind, ingredient.state));
        }

        return copy;
    }

    /// <summary>
    /// Buduje tekstowe podsumowanie przedmiotu kuchennego.
    /// Dla pojedynczego składnika zwraca jego sformatowaną nazwę z uwzględnieniem stanu.
    /// Dla potrawy złożonej zwraca nazwę przedmiotu wraz z listą wszystkich składników.
    /// </summary>
    /// <returns>
    /// Czytelny dla człowieka opis przedmiotu, np. "Kebab: Mieso (upieczony), Pomidor, Cebula (pokrojony)".
    /// </returns>
    public string BuildSummary()
    {
        if (!isDish)
        {
            return KitchenNaming.FormatIngredient(ingredientKind, state);
        }

        StringBuilder builder = new StringBuilder();
        builder.Append(itemName);
        builder.Append(": ");

        for (int i = 0; i < contents.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(contents[i].ToDisplayString());
        }

        return builder.ToString();
    }
}

/// <summary>
/// Klasa statyczna odpowiedzialna za walidację zgodności przygotowanej potrawy z zamówieniem klienta.
/// Porównuje zawartość dostarczonego przedmiotu kuchennego z wymaganiami zamówienia,
/// sprawdzając zarówno brakujące, jak i nadmiarowe składniki.
/// </summary>
/// <remarks>
/// Walidacja opiera się na porównaniu map zliczających składniki według klucza
/// złożonego z rodzaju składnika i jego stanu przetworzenia.
/// Zamówienie jest uznane za poprawnie zrealizowane tylko wtedy, gdy
/// dostarczona potrawa zawiera dokładnie te składniki (w odpowiednim stanie
/// i ilości), które zostały zamówione — bez nadmiarów i braków.
/// </remarks>
/// <seealso cref="Order"/>
/// <seealso cref="KitchenItem"/>
public static class KitchenOrderValidator
{
    /// <summary>
    /// Sprawdza, czy dostarczony przedmiot kuchenny spełnia wymagania zamówienia.
    /// Weryfikuje, czy przedmiot jest gotową potrawą, czy zawiera wszystkie
    /// wymagane składniki w odpowiednim stanie i ilości oraz czy nie zawiera
    /// nadmiarowych składników.
    /// </summary>
    /// <param name="order">Zamówienie do sprawdzenia. Jeśli <c>null</c>, walidacja kończy się niepowodzeniem.</param>
    /// <param name="item">
    /// Dostarczony przedmiot kuchenny. Musi mieć ustawioną flagę <see cref="KitchenItem.isDish"/>
    /// na <c>true</c>, aby walidacja mogła zakończyć się sukcesem.
    /// </param>
    /// <param name="failureReason">
    /// Parametr wyjściowy zawierający opis powodu niepowodzenia walidacji.
    /// Pusty ciąg znaków, jeśli walidacja zakończyła się sukcesem.
    /// </param>
    /// <returns>
    /// <c>true</c>, jeśli potrawa spełnia wszystkie wymagania zamówienia;
    /// <c>false</c> w przeciwnym wypadku.
    /// </returns>
    public static bool MatchesOrder(Order order, KitchenItem item, out string failureReason)
    {
        if (order == null)
        {
            failureReason = "Brak aktywnego zamowienia.";
            return false;
        }

        if (item == null || !item.isDish)
        {
            failureReason = "Klient oczekuje gotowego kebaba.";
            return false;
        }

        Dictionary<string, int> deliveredMap = BuildIngredientCountMap(item.contents);
        Dictionary<string, int> requiredMap = BuildRequirementCountMap(order.wymaganeSkladniki);

        foreach (KeyValuePair<string, int> requirement in requiredMap)
        {
            int deliveredAmount;
            deliveredMap.TryGetValue(requirement.Key, out deliveredAmount);
            if (deliveredAmount < requirement.Value)
            {
                failureReason = "Brakuje skladnika lub ma zly stan przygotowania.";
                return false;
            }
        }

        foreach (KeyValuePair<string, int> delivered in deliveredMap)
        {
            int requiredAmount;
            requiredMap.TryGetValue(delivered.Key, out requiredAmount);
            if (delivered.Value > requiredAmount)
            {
                failureReason = "Kebab zawiera nadmiarowe skladniki.";
                return false;
            }
        }

        failureReason = string.Empty;
        return true;
    }

    /// <summary>
    /// Buduje mapę zliczającą przygotowane składniki według klucza (rodzaj + stan).
    /// Służy do porównania dostarczonych składników z wymaganiami zamówienia.
    /// </summary>
    /// <param name="ingredients">Lista przygotowanych składników do zliczenia.</param>
    /// <returns>
    /// Słownik, w którym kluczem jest połączenie rodzaju i stanu składnika (np. "Meat|Cooked"),
    /// a wartością — liczba wystąpień.
    /// </returns>
    private static Dictionary<string, int> BuildIngredientCountMap(List<PreparedIngredientData> ingredients)
    {
        Dictionary<string, int> map = new Dictionary<string, int>();
        foreach (PreparedIngredientData ingredient in ingredients)
        {
            string key = GetKey(ingredient.ingredientKind, ingredient.state);
            if (!map.ContainsKey(key))
            {
                map[key] = 0;
            }

            map[key]++;
        }

        return map;
    }

    /// <summary>
    /// Buduje mapę zliczającą wymagane składniki zamówienia według klucza (rodzaj + stan).
    /// Uwzględnia ilość (<see cref="IngredientRequirement.quantity"/>) każdego wymagania.
    /// </summary>
    /// <param name="requirements">Lista wymagań składnikowych zamówienia.</param>
    /// <returns>
    /// Słownik, w którym kluczem jest połączenie rodzaju i stanu składnika (np. "Tomato|Chopped"),
    /// a wartością — łączna wymagana ilość.
    /// </returns>
    private static Dictionary<string, int> BuildRequirementCountMap(List<IngredientRequirement> requirements)
    {
        Dictionary<string, int> map = new Dictionary<string, int>();
        foreach (IngredientRequirement requirement in requirements)
        {
            string key = GetKey(requirement.ingredientKind, requirement.requiredState);
            if (!map.ContainsKey(key))
            {
                map[key] = 0;
            }

            map[key] += requirement.quantity;
        }

        return map;
    }

    /// <summary>
    /// Generuje unikalny klucz tekstowy na podstawie rodzaju i stanu przetworzenia składnika.
    /// Klucz ma format "RodzajSkladnika|StanPrzetworzenia" i służy do indeksowania
    /// map zliczających w procesie walidacji zamówienia.
    /// </summary>
    /// <param name="kind">Rodzaj składnika.</param>
    /// <param name="state">Stan przetworzenia składnika.</param>
    /// <returns>Klucz tekstowy w formacie "kind|state".</returns>
    private static string GetKey(IngredientKind kind, IngredientProcessState state)
    {
        return kind + "|" + state;
    }
}

/// <summary>
/// Klasa statyczna zapewniająca polskie nazwy dla składników i ich stanów przetworzenia.
/// Używana w całym systemie kuchni do generowania czytelnych dla gracza
/// opisów składników w interfejsie użytkownika.
/// </summary>
/// <remarks>
/// Wszystkie etykiety są zakodowane na stałe w języku polskim,
/// co jest zgodne z polskojęzyczną wersją gry.
/// Klasa jest wykorzystywana przez <see cref="IngredientRequirement.ToDisplayString"/>,
/// <see cref="PreparedIngredientData.ToDisplayString"/> oraz <see cref="KitchenItem.BuildSummary"/>.
/// </remarks>
public static class KitchenNaming
{
    /// <summary>
    /// Zwraca polską etykietę nazwy składnika na podstawie jego rodzaju.
    /// </summary>
    /// <param name="kind">Rodzaj składnika do przetłumaczenia.</param>
    /// <returns>Polska nazwa składnika (np. "Mieso", "Pomidor", "Cebula").</returns>
    public static string GetIngredientLabel(IngredientKind kind)
    {
        switch (kind)
        {
            case IngredientKind.Meat:
                return "Mieso";
            case IngredientKind.Tomato:
                return "Pomidor";
            case IngredientKind.Onion:
                return "Cebula";
            case IngredientKind.Lettuce:
                return "Salata";
            case IngredientKind.GarlicSauce:
                return "Sos czosnkowy";
            case IngredientKind.Lavash:
                return "Lawasz";
            case IngredientKind.Kebab:
                return "Kebab";
            default:
                return kind.ToString();
        }
    }

    /// <summary>
    /// Formatuje pełną nazwę składnika z uwzględnieniem jego stanu przetworzenia.
    /// Dla składników surowych (z wyjątkiem mięsa) zwraca samą nazwę bez stanu.
    /// Dla pozostałych przypadków dodaje stan w nawiasie, np. "Mieso (upieczony)".
    /// </summary>
    /// <param name="kind">Rodzaj składnika.</param>
    /// <param name="state">Stan przetworzenia składnika.</param>
    /// <returns>
    /// Sformatowana nazwa składnika, opcjonalnie z informacją o stanie przetworzenia w nawiasie.
    /// </returns>
    /// <remarks>
    /// Mięso surowe zawsze wyświetla stan ("surowy"), ponieważ stan mięsa
    /// jest istotną informacją — w przeciwieństwie do warzyw, które domyślnie
    /// są surowe i nie wymagają takiego oznaczenia.
    /// </remarks>
    public static string FormatIngredient(IngredientKind kind, IngredientProcessState state)
    {
        string ingredientName = GetIngredientLabel(kind);

        if (state == IngredientProcessState.Raw && kind != IngredientKind.Meat)
        {
            return ingredientName;
        }

        return ingredientName + " (" + GetProcessLabel(state) + ")";
    }

    /// <summary>
    /// Zwraca polską etykietę stanu przetworzenia składnika.
    /// </summary>
    /// <param name="state">Stan przetworzenia do przetłumaczenia.</param>
    /// <returns>Polska nazwa stanu przetworzenia (np. "surowy", "pokrojony", "upieczony", "zlozony").</returns>
    public static string GetProcessLabel(IngredientProcessState state)
    {
        switch (state)
        {
            case IngredientProcessState.Raw:
                return "surowy";
            case IngredientProcessState.Chopped:
                return "pokrojony";
            case IngredientProcessState.Cooked:
                return "upieczony";
            case IngredientProcessState.Assembled:
                return "zlozony";
            default:
                return state.ToString();
        }
    }
}
