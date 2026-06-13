/// \file DumpHierarchy.cs
/// \brief Plik zawierający narzędzie debugowania do zrzutu hierarchii obiektów sceny.
/// \details Komponent diagnostyczny dostępny wyłącznie w edytorze Unity.
///          Po upływie 5 sekund od uruchomienia sceny zapisuje pełną hierarchię
///          obiektów gry do pliku tekstowego w katalogu persistentDataPath.

using UnityEngine;

/// <summary>
/// Narzędzie debugowania do zrzutu hierarchii obiektów na scenie Unity.
/// Wyłączone w kompilacjach produkcyjnych — aktywne wyłącznie w edytorze Unity.
/// </summary>
/// <remarks>
/// Po upływie 5 sekund od uruchomienia gry, komponent przeszukuje wszystkie obiekty
/// na scenie (włącznie z nieaktywnymi) i zapisuje ich hierarchię do pliku
/// <c>hierarchy.txt</c> w katalogu <see cref="Application.persistentDataPath"/>.
/// Zrzut jest wykonywany jednokrotnie — po zapisie komponent nie podejmuje
/// dalszych działań. Aby ponownie włączyć tę funkcjonalność, należy aktywować
/// symbol kompilacji <c>UNITY_EDITOR</c> (aktywny domyślnie w edytorze).
/// </remarks>
public class DumpHierarchy : MonoBehaviour
{
#if UNITY_EDITOR
    /// <summary>
    /// Flaga określająca, czy zrzut hierarchii został już wykonany.
    /// Zapobiega wielokrotnemu zapisowi pliku w trakcie działania gry.
    /// </summary>
    private bool dumped = false;

    /// <summary>
    /// Metoda wywoływana co klatkę przez silnik Unity.
    /// Sprawdza, czy upłynęło 5 sekund od uruchomienia gry, a następnie
    /// wykonuje jednorazowy zrzut pełnej hierarchii obiektów sceny do pliku tekstowego.
    /// </summary>
    /// <remarks>
    /// Zrzut obejmuje wszystkie obiekty gry, w tym nieaktywne (oznaczone sufiksem "(I)").
    /// Plik wynikowy jest zapisywany w <see cref="Application.persistentDataPath"/>
    /// pod nazwą <c>hierarchy.txt</c>. W przypadku błędu zapisu
    /// wyświetlane jest ostrzeżenie w konsoli Unity.
    /// </remarks>
    void Update()
    {
        if (dumped) return;
        if (Time.time > 5f)
        {
            var sb = new System.Text.StringBuilder(4096);
            foreach (var o in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (o.transform.parent == null) Dump(sb, o, 0);
            }

            try
            {
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(Application.persistentDataPath, "hierarchy.txt"),
                    sb.ToString());
                Debug.Log("[DumpHierarchy] Hierarchy written.");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[DumpHierarchy] Write failed: " + e.Message);
            }

            dumped = true;
        }
    }

    /// <summary>
    /// Rekurencyjnie buduje tekstową reprezentację hierarchii obiektów gry.
    /// Każdy poziom zagnieżdżenia jest wizualizowany za pomocą wcięcia (2 spacje na poziom).
    /// Obiekty nieaktywne są oznaczane sufiksem "(I)".
    /// </summary>
    /// <param name="sb">Bufor <see cref="System.Text.StringBuilder"/> do budowania tekstu wyjściowego.</param>
    /// <param name="o">Bieżący obiekt gry (<see cref="GameObject"/>) do przetworzenia.</param>
    /// <param name="level">Aktualny poziom zagnieżdżenia w hierarchii (0 = korzeń).</param>
    void Dump(System.Text.StringBuilder sb, GameObject o, int level)
    {
        sb.Append(' ', level * 2);
        sb.Append(o.name);
        if (!o.activeSelf) sb.Append(" (I)");
        sb.AppendLine();
        foreach (Transform c in o.transform) Dump(sb, c.gameObject, level + 1);
    }
#endif
}
