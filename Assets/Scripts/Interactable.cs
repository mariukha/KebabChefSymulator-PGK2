/// \file Interactable.cs
/// \brief Plik zawierający bazową klasę obiektów interaktywnych w grze.
/// \details Definiuje komponent MonoBehaviour umożliwiający graczowi interakcję
///          z obiektami na scenie za pomocą systemu podpowiedzi i zdarzeń Unity.

using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Bazowa klasa dla wszystkich obiektów interaktywnych w grze.
/// Zapewnia mechanizm wyświetlania komunikatu podpowiedzi gracza
/// oraz wywoływania akcji po interakcji. Klasy pochodne mogą
/// nadpisywać metody wirtualne, aby dostarczyć niestandardowe zachowanie.
/// </summary>
/// <remarks>
/// Klasa korzysta z systemu <see cref="UnityEvent"/>, co pozwala
/// na konfigurację reakcji na interakcję bezpośrednio z poziomu Inspektora Unity
/// bez konieczności pisania dodatkowego kodu.
/// Metody <see cref="GetPrompt"/> i <see cref="Interact"/> są wirtualne,
/// umożliwiając specjalizację w klasach pochodnych (np. stacje kuchenne, źródła składników).
/// </remarks>
public class Interactable : MonoBehaviour
{
    /// <summary>
    /// Komunikat podpowiedzi wyświetlany graczowi, gdy jest w zasięgu interakcji.
    /// Domyślna wartość to "Interact".
    /// </summary>
    [SerializeField] private string promptMessage = "Interact";

    /// <summary>
    /// Zdarzenie Unity wywoływane podczas interakcji gracza z obiektem.
    /// Umożliwia podpięcie dowolnych akcji z poziomu Inspektora Unity.
    /// </summary>
    [SerializeField] private UnityEvent onInteract;

    /// <summary>
    /// Właściwość dostępowa do komunikatu podpowiedzi interakcji.
    /// Pozwala na odczyt i zapis tekstu wyświetlanego graczowi
    /// w momencie, gdy obiekt jest w zasięgu interakcji.
    /// </summary>
    /// <value>Tekst komunikatu podpowiedzi interakcji.</value>
    public string PromptMessage
    {
        get { return promptMessage; }
        set { promptMessage = value; }
    }

    /// <summary>
    /// Zwraca komunikat podpowiedzi dla danego gracza.
    /// Metoda wirtualna — klasy pochodne mogą ją nadpisać,
    /// aby dostarczyć kontekstowy komunikat zależny od stanu gry lub gracza.
    /// </summary>
    /// <param name="player">Referencja do komponentu interakcji gracza, który żąda podpowiedzi.</param>
    /// <returns>Tekst komunikatu podpowiedzi do wyświetlenia w interfejsie użytkownika.</returns>
    public virtual string GetPrompt(PlayerInteraction player)
    {
        return promptMessage;
    }

    /// <summary>
    /// Wykonuje akcję interakcji z obiektem.
    /// Domyślna implementacja wywołuje zdarzenie <see cref="onInteract"/>.
    /// Metoda wirtualna — klasy pochodne mogą ją nadpisać, aby dodać
    /// własną logikę interakcji (np. podnoszenie składników, obsługa stacji).
    /// </summary>
    /// <param name="player">Referencja do komponentu interakcji gracza wykonującego akcję. Może być <c>null</c>.</param>
    public virtual void Interact(PlayerInteraction player)
    {
        onInteract?.Invoke();
    }

    /// <summary>
    /// Metoda pomocnicza wywołująca interakcję bez podania referencji do gracza.
    /// Przydatna do wywoływania interakcji z poziomu zdarzeń Unity
    /// lub skryptów, które nie posiadają referencji do obiektu gracza.
    /// </summary>
    /// <remarks>
    /// Wywołuje metodę <see cref="Interact"/> z parametrem <c>null</c>
    /// zamiast konkretnego obiektu gracza.
    /// </remarks>
    public void BaseInteract()
    {
        Interact(null);
    }
}
