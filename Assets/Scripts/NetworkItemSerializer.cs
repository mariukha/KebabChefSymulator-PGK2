/// \file NetworkItemSerializer.cs
/// \brief Plik zawierający strukturę NetworkItemState do serializacji przedmiotów kuchennych przez sieć.
/// \details Definiuje serializowalną strukturę reprezentującą stan przedmiotu kuchennego
/// (składnika lub dania) do przesyłania pomiędzy serwerem a klientami.
/// Obsługuje konwersję z/do obiektu KitchenItem oraz serializację binarną
/// z optymalizacją liczby przesyłanych składników.

using Unity.Collections;
using Unity.Netcode;

/// <summary>
/// Struktura reprezentująca sieciowy stan przedmiotu kuchennego (składnika lub dania).
/// </summary>
/// <remarks>
/// Implementuje <see cref="INetworkSerializable"/> do serializacji binarnej przez Netcode.
/// Przechowuje informacje o:
/// <list type="bullet">
///   <item>Nazwie przedmiotu</item>
///   <item>Rodzaju składnika i jego stanie przetworzenia</item>
///   <item>Czy przedmiot jest daniem (posiada zawartość)</item>
///   <item>Czy przedmiot istnieje (w ręku gracza lub na stacji)</item>
///   <item>Do 8 składnikach zawartych w daniu</item>
/// </list>
/// Serializacja jest zoptymalizowana — składniki zawartości (content) są przesyłane
/// tylko do wartości <see cref="contentCount"/>.
/// </remarks>
public struct NetworkItemState : INetworkSerializable
{
    /// <summary>
    /// Nazwa przedmiotu kuchennego (maksymalnie 64 bajty).
    /// </summary>
    public FixedString64Bytes itemName;

    /// <summary>
    /// Rodzaj składnika (np. mięso, warzywo, sos).
    /// </summary>
    public IngredientKind ingredientKind;

    /// <summary>
    /// Stan przetworzenia składnika (np. surowy, grillowany, pokrojony).
    /// </summary>
    public IngredientProcessState state;

    /// <summary>
    /// Czy przedmiot jest daniem (zawiera wiele składników).
    /// </summary>
    /// <remarks>
    /// Gdy <c>true</c>, przedmiot posiada listę zawartości (content),
    /// która jest serializowana wraz z głównym stanem.
    /// </remarks>
    public bool isDish;

    /// <summary>
    /// Czy przedmiot istnieje (jest obecny w ręku gracza lub na stacji).
    /// </summary>
    /// <remarks>
    /// Gdy <c>false</c>, oznacza brak przedmiotu (puste ręce lub pusta stacja).
    /// </remarks>
    public bool exists;

    /// <summary>
    /// Liczba składników zawartych w daniu (0-8).
    /// </summary>
    public int contentCount;

    /// <summary>Rodzaj składnika zawartości w slocie 0.</summary>
    public IngredientKind content0Kind;
    /// <summary>Stan przetworzenia składnika zawartości w slocie 0.</summary>
    public IngredientProcessState content0State;
    /// <summary>Rodzaj składnika zawartości w slocie 1.</summary>
    public IngredientKind content1Kind;
    /// <summary>Stan przetworzenia składnika zawartości w slocie 1.</summary>
    public IngredientProcessState content1State;
    /// <summary>Rodzaj składnika zawartości w slocie 2.</summary>
    public IngredientKind content2Kind;
    /// <summary>Stan przetworzenia składnika zawartości w slocie 2.</summary>
    public IngredientProcessState content2State;
    /// <summary>Rodzaj składnika zawartości w slocie 3.</summary>
    public IngredientKind content3Kind;
    /// <summary>Stan przetworzenia składnika zawartości w slocie 3.</summary>
    public IngredientProcessState content3State;
    /// <summary>Rodzaj składnika zawartości w slocie 4.</summary>
    public IngredientKind content4Kind;
    /// <summary>Stan przetworzenia składnika zawartości w slocie 4.</summary>
    public IngredientProcessState content4State;
    /// <summary>Rodzaj składnika zawartości w slocie 5.</summary>
    public IngredientKind content5Kind;
    /// <summary>Stan przetworzenia składnika zawartości w slocie 5.</summary>
    public IngredientProcessState content5State;
    /// <summary>Rodzaj składnika zawartości w slocie 6.</summary>
    public IngredientKind content6Kind;
    /// <summary>Stan przetworzenia składnika zawartości w slocie 6.</summary>
    public IngredientProcessState content6State;
    /// <summary>Rodzaj składnika zawartości w slocie 7.</summary>
    public IngredientKind content7Kind;
    /// <summary>Stan przetworzenia składnika zawartości w slocie 7.</summary>
    public IngredientProcessState content7State;

    /// <summary>
    /// Serializuje lub deserializuje dane stanu przedmiotu przez bufor sieciowy.
    /// </summary>
    /// <typeparam name="T">Typ implementujący <see cref="IReaderWriter"/>.</typeparam>
    /// <param name="serializer">Serializer bufora sieciowego.</param>
    /// <remarks>
    /// Optymalizuje rozmiar pakietu — sloty zawartości są serializowane
    /// tylko do wartości <see cref="contentCount"/>, co minimalizuje przepustowość sieciową.
    /// </remarks>
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref itemName);
        serializer.SerializeValue(ref ingredientKind);
        serializer.SerializeValue(ref state);
        serializer.SerializeValue(ref isDish);
        serializer.SerializeValue(ref exists);
        serializer.SerializeValue(ref contentCount);

        if (contentCount > 0) { serializer.SerializeValue(ref content0Kind); serializer.SerializeValue(ref content0State); }
        if (contentCount > 1) { serializer.SerializeValue(ref content1Kind); serializer.SerializeValue(ref content1State); }
        if (contentCount > 2) { serializer.SerializeValue(ref content2Kind); serializer.SerializeValue(ref content2State); }
        if (contentCount > 3) { serializer.SerializeValue(ref content3Kind); serializer.SerializeValue(ref content3State); }
        if (contentCount > 4) { serializer.SerializeValue(ref content4Kind); serializer.SerializeValue(ref content4State); }
        if (contentCount > 5) { serializer.SerializeValue(ref content5Kind); serializer.SerializeValue(ref content5State); }
        if (contentCount > 6) { serializer.SerializeValue(ref content6Kind); serializer.SerializeValue(ref content6State); }
        if (contentCount > 7) { serializer.SerializeValue(ref content7Kind); serializer.SerializeValue(ref content7State); }
    }

    /// <summary>
    /// Tworzy pusty stan przedmiotu oznaczający brak przedmiotu.
    /// </summary>
    /// <returns>Nowy <see cref="NetworkItemState"/> z <see cref="exists"/> ustawionym na <c>false</c>.</returns>
    public static NetworkItemState Empty()
    {
        return new NetworkItemState { exists = false };
    }

    /// <summary>
    /// Konwertuje obiekt <see cref="KitchenItem"/> na sieciowy stan <see cref="NetworkItemState"/>.
    /// </summary>
    /// <param name="item">Przedmiot kuchenny do konwersji. Może być <c>null</c>.</param>
    /// <returns>
    /// Stan sieciowy odpowiadający podanemu przedmiotowi
    /// lub <see cref="Empty"/> jeśli przedmiot jest <c>null</c>.
    /// </returns>
    /// <remarks>
    /// Dla dań (<see cref="KitchenItem.isDish"/> == <c>true</c>) kopiuje do 8 składników
    /// z listy zawartości przedmiotu.
    /// </remarks>
    public static NetworkItemState FromKitchenItem(KitchenItem item)
    {
        if (item == null)
        {
            return Empty();
        }

        NetworkItemState netState = new NetworkItemState
        {
            itemName = new FixedString64Bytes(item.itemName ?? "Skladnik"),
            ingredientKind = item.ingredientKind,
            state = item.state,
            isDish = item.isDish,
            exists = true,
            contentCount = 0
        };

        if (item.contents != null && item.isDish)
        {
            netState.contentCount = UnityEngine.Mathf.Min(item.contents.Count, 8);
            for (int i = 0; i < netState.contentCount; i++)
            {
                SetContent(ref netState, i, item.contents[i].ingredientKind, item.contents[i].state);
            }
        }

        return netState;
    }

    /// <summary>
    /// Konwertuje sieciowy stan z powrotem na obiekt <see cref="KitchenItem"/>.
    /// </summary>
    /// <returns>
    /// Nowy obiekt <see cref="KitchenItem"/> z odpowiednimi właściwościami
    /// lub <c>null</c> jeśli <see cref="exists"/> jest <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Odtwarza listę zawartości dania na podstawie zapisanych slotów zawartości.
    /// </remarks>
    public KitchenItem ToKitchenItem()
    {
        if (!exists)
        {
            return null;
        }

        KitchenItem item = new KitchenItem
        {
            itemName = itemName.ToString(),
            ingredientKind = ingredientKind,
            state = state,
            isDish = isDish
        };

        for (int i = 0; i < contentCount; i++)
        {
            GetContent(this, i, out IngredientKind cKind, out IngredientProcessState cState);
            item.contents.Add(new PreparedIngredientData(cKind, cState));
        }

        return item;
    }

    /// <summary>
    /// Ustawia rodzaj i stan przetworzenia składnika zawartości w określonym slocie.
    /// </summary>
    /// <param name="s">Referencja do stanu sieciowego do modyfikacji.</param>
    /// <param name="index">Indeks slotu zawartości (0-7).</param>
    /// <param name="kind">Rodzaj składnika.</param>
    /// <param name="pState">Stan przetworzenia składnika.</param>
    /// <remarks>
    /// Indeksy spoza zakresu 0-7 są ignorowane.
    /// </remarks>
    private static void SetContent(ref NetworkItemState s, int index, IngredientKind kind, IngredientProcessState pState)
    {
        switch (index)
        {
            case 0: s.content0Kind = kind; s.content0State = pState; break;
            case 1: s.content1Kind = kind; s.content1State = pState; break;
            case 2: s.content2Kind = kind; s.content2State = pState; break;
            case 3: s.content3Kind = kind; s.content3State = pState; break;
            case 4: s.content4Kind = kind; s.content4State = pState; break;
            case 5: s.content5Kind = kind; s.content5State = pState; break;
            case 6: s.content6Kind = kind; s.content6State = pState; break;
            case 7: s.content7Kind = kind; s.content7State = pState; break;
        }
    }

    /// <summary>
    /// Pobiera rodzaj i stan przetworzenia składnika zawartości z określonego slotu.
    /// </summary>
    /// <param name="s">Stan sieciowy, z którego odczytywana jest zawartość.</param>
    /// <param name="index">Indeks slotu zawartości (0-7).</param>
    /// <param name="kind">Wynikowy rodzaj składnika.</param>
    /// <param name="pState">Wynikowy stan przetworzenia składnika.</param>
    /// <remarks>
    /// Dla indeksów spoza zakresu 0-7 zwraca wartości domyślne:
    /// <see cref="IngredientKind.Meat"/> i <see cref="IngredientProcessState.Raw"/>.
    /// </remarks>
    private static void GetContent(NetworkItemState s, int index, out IngredientKind kind, out IngredientProcessState pState)
    {
        switch (index)
        {
            case 0: kind = s.content0Kind; pState = s.content0State; break;
            case 1: kind = s.content1Kind; pState = s.content1State; break;
            case 2: kind = s.content2Kind; pState = s.content2State; break;
            case 3: kind = s.content3Kind; pState = s.content3State; break;
            case 4: kind = s.content4Kind; pState = s.content4State; break;
            case 5: kind = s.content5Kind; pState = s.content5State; break;
            case 6: kind = s.content6Kind; pState = s.content6State; break;
            case 7: kind = s.content7Kind; pState = s.content7State; break;
            default: kind = IngredientKind.Meat; pState = IngredientProcessState.Raw; break;
        }
    }
}
