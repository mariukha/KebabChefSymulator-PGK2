/// \file ItemAnimator.cs
/// \brief Plik zawierający klasę ItemAnimator — animator wizualny przedmiotów w grze.
/// \details Animuje wizualne elementy przedmiotów: efekt pojawienia się (bounce),
/// efekt znikania (shrink), oraz efekt pulsacji (pop) przy zmianie stanu.
/// Wykorzystuje korutyny do płynnych, nieblokujących animacji.

using UnityEngine;

/// <summary>
/// Animuje wizualne elementy przedmiotów: efekt pojawienia się z odbiciem (bounce),
/// efekt znikania ze zmniejszaniem (shrink) oraz efekt pulsacji (pop).
/// Wykorzystuje korutyny do płynnych, nieblokujących animacji.
/// </summary>
/// <remarks>
/// Klasa implementuje wzorzec Singleton. Wszystkie animacje operują na skali lokalnej
/// (localScale) obiektu docelowego. Korutyny sprawdzają w każdej klatce, czy obiekt
/// docelowy nadal istnieje, dzięki czemu są bezpieczne nawet gdy obiekt zostanie
/// zniszczony w trakcie trwania animacji.
/// </remarks>
public class ItemAnimator : MonoBehaviour
{
    /// <summary>
    /// Statyczna instancja Singletona klasy <see cref="ItemAnimator"/>.
    /// Umożliwia globalny dostęp do animatora przedmiotów z dowolnego miejsca w kodzie.
    /// </summary>
    public static ItemAnimator Instance { get; private set; }

    /// <summary>
    /// Metoda inicjalizacyjna Unity wywoływana przy tworzeniu obiektu.
    /// Implementuje wzorzec Singleton — ustawia instancję lub niszczy duplikat.
    /// </summary>
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Animuje pojawienie się przedmiotu — skaluje go od 0 do 1 z efektem odbicia (overshoot bounce).
    /// Wywoływać, gdy nowy wizualny element przedmiotu pojawia się na scenie.
    /// </summary>
    /// <param name="target">Obiekt gry do animowania. Jeśli null, metoda nic nie robi.</param>
    public void AnimateSpawn(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        StartCoroutine(SpawnCoroutine(target));
    }

    /// <summary>
    /// Animuje znikanie przedmiotu — skaluje go szybko od 1 do 0.
    /// Wywoływać, gdy przedmiot jest usuwany ze sceny.
    /// </summary>
    /// <param name="target">Obiekt gry do animowania. Jeśli null, metoda nic nie robi.</param>
    public void AnimateDespawn(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        StartCoroutine(DespawnCoroutine(target));
    }

    /// <summary>
    /// Animuje efekt pulsacji (pop) — krótkie powiększenie obiektu i powrót do oryginalnej skali.
    /// Wywoływać przy zmianie stanu przedmiotu (np. zmiana etapu przetwarzania).
    /// </summary>
    /// <param name="target">Obiekt gry do animowania. Jeśli null, metoda nic nie robi.</param>
    /// <param name="intensity">Mnożnik maksymalnej skali pulsacji (domyślnie 1.2 = 120% oryginalnej skali).</param>
    public void AnimatePop(GameObject target, float intensity = 1.2f)
    {
        if (target == null)
        {
            return;
        }

        StartCoroutine(PopCoroutine(target, intensity));
    }

    /// <summary>
    /// Korutyna animacji pojawienia się przedmiotu z efektem odbicia.
    /// Animacja trwa 0.35s i składa się z trzech faz:
    /// 1. Szybkie powiększenie do 115% (0-60% czasu)
    /// 2. Lekkie cofnięcie do 95% (60-80% czasu)
    /// 3. Stabilizacja do 100% (80-100% czasu)
    /// </summary>
    /// <param name="target">Obiekt gry, którego skala jest animowana.</param>
    /// <returns>Enumerator korutyny.</returns>
    private System.Collections.IEnumerator SpawnCoroutine(GameObject target)
    {
        if (target == null)
        {
            yield break;
        }

        Vector3 finalScale = target.transform.localScale;
        float duration = 0.35f;
        float elapsed = 0f;

        target.transform.localScale = Vector3.zero;

        while (elapsed < duration)
        {
            if (target == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float bounce;
            if (t < 0.6f)
            {

                float sub = t / 0.6f;
                bounce = sub * sub * 1.15f;
            }
            else if (t < 0.8f)
            {

                float sub = (t - 0.6f) / 0.2f;
                bounce = Mathf.Lerp(1.15f, 0.95f, sub);
            }
            else
            {

                float sub = (t - 0.8f) / 0.2f;
                bounce = Mathf.Lerp(0.95f, 1f, sub);
            }

            target.transform.localScale = finalScale * bounce;
            yield return null;
        }

        if (target != null)
        {
            target.transform.localScale = finalScale;
        }
    }

    /// <summary>
    /// Korutyna animacji znikania przedmiotu.
    /// Animacja trwa 0.15s — szybkie zmniejszenie skali do zera z krzywą wygaszania kwadratowego (ease-out).
    /// </summary>
    /// <param name="target">Obiekt gry, którego skala jest animowana.</param>
    /// <returns>Enumerator korutyny.</returns>
    private System.Collections.IEnumerator DespawnCoroutine(GameObject target)
    {
        if (target == null)
        {
            yield break;
        }

        Vector3 startScale = target.transform.localScale;
        float duration = 0.15f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (target == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = 1f - (t * t);
            target.transform.localScale = startScale * ease;
            yield return null;
        }

        if (target != null)
        {
            target.transform.localScale = Vector3.zero;
        }
    }

    /// <summary>
    /// Korutyna animacji pulsacji (pop) przedmiotu.
    /// Animacja trwa 0.2s — skala rośnie sinusoidalnie do wartości szczytowej,
    /// a następnie płynnie wraca do oryginalnej skali.
    /// </summary>
    /// <param name="target">Obiekt gry, którego skala jest animowana.</param>
    /// <param name="intensity">Mnożnik maksymalnej skali pulsacji.</param>
    /// <returns>Enumerator korutyny.</returns>
    private System.Collections.IEnumerator PopCoroutine(GameObject target, float intensity)
    {
        if (target == null)
        {
            yield break;
        }

        Vector3 originalScale = target.transform.localScale;
        Vector3 peakScale = originalScale * intensity;
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (target == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float curve = Mathf.Sin(t * Mathf.PI);
            target.transform.localScale = Vector3.Lerp(originalScale, peakScale, curve);
            yield return null;
        }

        if (target != null)
        {
            target.transform.localScale = originalScale;
        }
    }

    /// <summary>
    /// Metoda Unity wywoływana przy niszczeniu obiektu.
    /// Czyści statyczną referencję Singletona, aby uniknąć wiszących wskaźników.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
