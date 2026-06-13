/// \file Order.cs
/// \brief Plik zawierający definicję klasy zamówienia klienta.
/// \details Klasa Order reprezentuje pojedyncze zamówienie składane przez klienta
///          w symulatorze kebaba. Przechowuje informacje o wymaganych składnikach,
///          nazwie zamówienia, limicie czasowym oraz nagrodzie pieniężnej.

using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Klasa reprezentująca zamówienie klienta w grze.
/// Zawiera informacje o wymaganych składnikach, nazwie zamówienia,
/// danych klienta, limicie czasu na realizację oraz nagrodzie pieniężnej.
/// </summary>
/// <remarks>
/// Zamówienie jest serializowalne, co umożliwia jego przechowywanie
/// w edytorze Unity oraz przesyłanie przez sieć. Lista wymaganych składników
/// (<see cref="wymaganeSkladniki"/>) definiuje, jakie składniki i w jakim stanie
/// przetworzenia muszą znaleźć się w gotowej potrawie, aby zamówienie
/// zostało uznane za zrealizowane.
/// </remarks>
/// <seealso cref="IngredientRequirement"/>
/// <seealso cref="KitchenOrderValidator"/>
[Serializable]
public class Order
{
    /// <summary>
    /// Unikalny identyfikator zamówienia.
    /// Używany do rozróżniania zamówień w systemie zarządzania.
    /// </summary>
    public string orderId = "classic-kebab";

    /// <summary>
    /// Nazwa klienta składającego zamówienie.
    /// Wyświetlana w interfejsie użytkownika przy opisie zamówienia.
    /// </summary>
    public string nazwaKlienta = "Klient";

    /// <summary>
    /// Nazwa zamówienia (np. "Klasyczny kebab").
    /// Określa typ potrawy zamówionej przez klienta.
    /// </summary>
    public string nazwaZamowienia = "Klasyczny kebab";

    /// <summary>
    /// Lista wymaganych składników zamówienia.
    /// Każdy element określa rodzaj składnika, wymagany stan przetworzenia
    /// oraz ilość. Zamówienie jest uznane za zrealizowane, gdy dostarczona
    /// potrawa zawiera dokładnie te składniki.
    /// </summary>
    /// <seealso cref="IngredientRequirement"/>
    public List<IngredientRequirement> wymaganeSkladniki = new List<IngredientRequirement>();

    /// <summary>
    /// Maksymalny czas na realizację zamówienia, wyrażony w sekundach.
    /// Po upływie tego czasu zamówienie może zostać anulowane lub gracz traci punkty.
    /// </summary>
    public float czasNaRealizacje = 90f;

    /// <summary>
    /// Nagroda pieniężna przyznawana graczowi za poprawne zrealizowanie zamówienia.
    /// Wyrażona w walucie gry.
    /// </summary>
    public float nagrodaPieniezna = 30f;

    /// <summary>
    /// Tworzy głęboką kopię zamówienia wraz ze wszystkimi wymaganymi składnikami.
    /// Nowy obiekt jest niezależny od oryginału — modyfikacje kopii
    /// nie wpływają na oryginalne zamówienie.
    /// </summary>
    /// <returns>Nowa instancja <see cref="Order"/> będąca pełną kopią bieżącego zamówienia.</returns>
    public Order Clone()
    {
        Order copy = new Order
        {
            orderId = orderId,
            nazwaKlienta = nazwaKlienta,
            nazwaZamowienia = nazwaZamowienia,
            czasNaRealizacje = czasNaRealizacje,
            nagrodaPieniezna = nagrodaPieniezna
        };

        foreach (IngredientRequirement requirement in wymaganeSkladniki)
        {
            copy.wymaganeSkladniki.Add(new IngredientRequirement(
                requirement.ingredientKind,
                requirement.requiredState,
                requirement.quantity));
        }

        return copy;
    }

    /// <summary>
    /// Buduje czytelny dla człowieka opis zamówienia w języku polskim.
    /// Format: "{nazwaZamowienia} dla {nazwaKlienta}: {lista składników}".
    /// </summary>
    /// <returns>Sformatowany tekst opisu zamówienia zawierający nazwę, klienta i listę składników.</returns>
    /// <example>
    /// Przykładowy wynik: "Klasyczny kebab dla Jan: Mieso (upieczony), Pomidor, Cebula (pokrojony)"
    /// </example>
    public string BuildDescription()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(nazwaZamowienia);
        builder.Append(" dla ");
        builder.Append(nazwaKlienta);
        builder.Append(": ");

        for (int i = 0; i < wymaganeSkladniki.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(wymaganeSkladniki[i].ToDisplayString());
        }

        return builder.ToString();
    }
}
