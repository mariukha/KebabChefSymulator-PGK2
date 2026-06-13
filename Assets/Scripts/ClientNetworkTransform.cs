/// \file ClientNetworkTransform.cs
/// \brief Plik zawierający implementację sieciowej transformacji sterowanej przez klienta.
/// \details Rozszerza klasę NetworkTransform z Unity Netcode, umożliwiając klientowi
///          bezpośrednie sterowanie własną pozycją, rotacją i skalą bez autoryzacji serwera.

using Unity.Netcode.Components;

/// <summary>
/// Komponent sieciowej transformacji sterowanej po stronie klienta.
/// Nadpisuje domyślne zachowanie <see cref="NetworkTransform"/>, wyłączając
/// autorytatywność serwera, dzięki czemu każdy klient samodzielnie
/// kontroluje i synchronizuje swoją transformację w sieci.
/// </summary>
/// <remarks>
/// W standardowym modelu Unity Netcode, <see cref="NetworkTransform"/> jest
/// autorytatywny po stronie serwera — serwer decyduje o pozycji obiektów.
/// Ta klasa odwraca ten model, umożliwiając klientowi przesyłanie własnych
/// danych transformacji do pozostałych graczy. Jest to typowe rozwiązanie
/// stosowane dla obiektów gracza w grach multiplayer, gdzie responsywność
/// sterowania jest priorytetem.
/// </remarks>
/// <seealso cref="NetworkTransform"/>
public class ClientNetworkTransform : NetworkTransform
{
    /// <summary>
    /// Określa, czy transformacja jest autorytatywna po stronie serwera.
    /// </summary>
    /// <returns>
    /// Zawsze zwraca <c>false</c>, co oznacza, że klient jest źródłem
    /// prawdy dla danych transformacji tego obiektu sieciowego.
    /// </returns>
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
