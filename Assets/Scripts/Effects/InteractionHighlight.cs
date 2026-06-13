/// \file InteractionHighlight.cs
/// \brief Plik zawierający klasę InteractionHighlight — efekt podświetlenia interaktywnych obiektów.
/// \details Dodaje wizualny efekt podświetlenia emisyjnego i pulsacji skali do obiektów,
/// na które gracz patrzy i z którymi może wejść w interakcję.

using UnityEngine;

/// <summary>
/// Dodaje wizualny efekt podświetlenia do interaktywnych obiektów, gdy gracz na nie patrzy.
/// Wykorzystuje wzmocnienie koloru emisji na rendererach obiektu (bez potrzeby niestandardowych shaderów).
/// Tworzy także subtelną pulsację skali podświetlonego obiektu.
/// </summary>
/// <remarks>
/// Klasa implementuje wzorzec Singleton. Podświetlenie jest realizowane przez ustawienie
/// koloru emisji w <see cref="MaterialPropertyBlock"/>, co nie modyfikuje współdzielonych materiałów.
/// Pulsacja skali dodaje dodatkowy wizualny sygnał, że obiekt jest interaktywny.
/// Metoda <see cref="SetTarget"/> powinna być wywoływana co klatkę z systemu interakcji gracza.
/// </remarks>
public class InteractionHighlight : MonoBehaviour
{
    /// <summary>
    /// Statyczna instancja Singletona klasy <see cref="InteractionHighlight"/>.
    /// Umożliwia globalny dostęp do systemu podświetlania z dowolnego miejsca w kodzie.
    /// </summary>
    public static InteractionHighlight Instance { get; private set; }

    /// <summary>
    /// Aktualnie podświetlony obiekt gry (cel interakcji).
    /// Null, gdy żaden obiekt nie jest podświetlony.
    /// </summary>
    private GameObject currentTarget;

    /// <summary>
    /// Tablica rendererów aktualnie podświetlonego obiektu i jego dzieci.
    /// Używana do stosowania i usuwania efektu emisji.
    /// </summary>
    private Renderer[] currentRenderers;

    /// <summary>
    /// Tablica oryginalnych kolorów emisji rendererów przed zastosowaniem podświetlenia.
    /// Przechowywana w celu przywrócenia oryginalnych wartości przy usuwaniu podświetlenia.
    /// </summary>
    private Color[] originalEmissions;

    /// <summary>
    /// Flaga określająca, czy efekt podświetlenia jest aktualnie aktywny.
    /// </summary>
    private bool isHighlighting;

    /// <summary>
    /// Akumulator fazy pulsacji skali obiektu (rośnie z prędkością <see cref="PulseSpeed"/>).
    /// </summary>
    private float pulsePhase;

    /// <summary>
    /// Tablica oryginalnych skal lokalnych rendererów przed zastosowaniem pulsacji.
    /// Przechowywana w celu przywrócenia oryginalnych wartości przy usuwaniu podświetlenia.
    /// </summary>
    private Vector3[] originalScales;

    /// <summary>
    /// Kolor emisji stosowany na podświetlonym obiekcie.
    /// Ciepły, brązowo-złoty odcień nawiązujący do estetyki kuchni kebabowej.
    /// </summary>
    private static readonly Color HighlightEmission = new Color(0.35f, 0.28f, 0.12f);

    /// <summary>
    /// Amplituda pulsacji skali podświetlonego obiektu (mnożnik).
    /// Wartość 0.012 oznacza oscylację skali o ±1.2% wokół oryginalnej wartości.
    /// </summary>
    private const float PulseAmplitude = 0.012f;

    /// <summary>
    /// Szybkość pulsacji skali podświetlonego obiektu (w radianach na sekundę).
    /// </summary>
    private const float PulseSpeed = 4f;

    /// <summary>
    /// Statyczna, leniwie inicjalizowana instancja <see cref="MaterialPropertyBlock"/>.
    /// Pole bazowe dla właściwości <see cref="propBlock"/>.
    /// </summary>
    private static MaterialPropertyBlock _propBlock;

    /// <summary>
    /// Współdzielony blok właściwości materiału używany do modyfikacji emisji rendererów
    /// bez tworzenia nowych instancji materiałów. Inicjalizowany leniwie przy pierwszym dostępie.
    /// </summary>
    private static MaterialPropertyBlock propBlock
    {
        get
        {
            if (_propBlock == null)
                _propBlock = new MaterialPropertyBlock();
            return _propBlock;
        }
    }

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
    /// Ustawia cel podświetlenia interakcji.
    /// Wywoływać co klatkę z klasy PlayerInteraction z aktualnym celem (lub null gdy brak celu).
    /// Jeśli cel zmienia się, poprzednie podświetlenie jest usuwane i nakładane na nowy obiekt.
    /// </summary>
    /// <param name="target">Obiekt gry do podświetlenia lub null, aby usunąć podświetlenie.</param>
    public void SetTarget(GameObject target)
    {
        if (target == currentTarget)
        {
            return;
        }

        ClearHighlight();

        currentTarget = target;

        if (currentTarget != null)
        {
            ApplyHighlight();
        }
    }

    /// <summary>
    /// Metoda Unity wywoływana co klatkę.
    /// Aktualizuje pulsację skali podświetlonego obiektu — sinusoidalne oscylowanie
    /// rozmiaru rendererów wokół ich oryginalnych skal.
    /// </summary>
    private void Update()
    {
        if (!isHighlighting || currentRenderers == null)
        {
            return;
        }

        pulsePhase += Time.deltaTime * PulseSpeed;
        float pulse = 1f + Mathf.Sin(pulsePhase) * PulseAmplitude;

        for (int i = 0; i < currentRenderers.Length; i++)
        {
            if (currentRenderers[i] == null)
            {
                continue;
            }

            if (originalScales != null && i < originalScales.Length)
            {
                currentRenderers[i].transform.localScale = originalScales[i] * pulse;
            }
        }
    }

    /// <summary>
    /// Stosuje efekt podświetlenia na aktualnym celu interakcji.
    /// Pobiera wszystkie renderery z obiektu i jego dzieci, zapamiętuje oryginalne wartości emisji i skal,
    /// a następnie ustawia kolor emisji podświetlenia i aktywuje słowo kluczowe "_EMISSION" w materiałach.
    /// </summary>
    private void ApplyHighlight()
    {
        currentRenderers = currentTarget.GetComponentsInChildren<Renderer>();
        if (currentRenderers.Length == 0)
        {
            return;
        }

        originalEmissions = new Color[currentRenderers.Length];
        originalScales = new Vector3[currentRenderers.Length];

        for (int i = 0; i < currentRenderers.Length; i++)
        {
            Renderer rend = currentRenderers[i];
            if (rend == null)
            {
                continue;
            }

            originalScales[i] = rend.transform.localScale;

            rend.GetPropertyBlock(propBlock);
            if (rend.sharedMaterial != null && rend.sharedMaterial.HasProperty("_EmissionColor"))
            {
                originalEmissions[i] = rend.sharedMaterial.GetColor("_EmissionColor");
            }
            else
            {
                originalEmissions[i] = Color.black;
            }

            propBlock.SetColor("_EmissionColor", HighlightEmission);
            propBlock.SetColor("_BaseColor", rend.sharedMaterial != null && rend.sharedMaterial.HasProperty("_BaseColor")
                ? rend.sharedMaterial.GetColor("_BaseColor")
                : Color.white);
            rend.SetPropertyBlock(propBlock);

            if (rend.sharedMaterial != null)
            {
                rend.sharedMaterial.EnableKeyword("_EMISSION");
            }
        }

        isHighlighting = true;
        pulsePhase = 0f;
    }

    /// <summary>
    /// Usuwa efekt podświetlenia z aktualnego celu interakcji.
    /// Przywraca oryginalne kolory emisji i skale rendererów, a następnie
    /// czyści wszystkie referencje do podświetlonego obiektu.
    /// </summary>
    private void ClearHighlight()
    {
        if (!isHighlighting || currentRenderers == null)
        {
            return;
        }

        for (int i = 0; i < currentRenderers.Length; i++)
        {
            Renderer rend = currentRenderers[i];
            if (rend == null)
            {
                continue;
            }

            rend.GetPropertyBlock(propBlock);
            propBlock.SetColor("_EmissionColor", originalEmissions[i]);
            rend.SetPropertyBlock(propBlock);

            if (originalScales != null && i < originalScales.Length)
            {
                rend.transform.localScale = originalScales[i];
            }
        }

        currentRenderers = null;
        originalEmissions = null;
        originalScales = null;
        currentTarget = null;
        isHighlighting = false;
    }

    /// <summary>
    /// Metoda Unity wywoływana przy niszczeniu obiektu.
    /// Czyści podświetlenie (jeśli aktywne) i resetuje statyczną referencję Singletona.
    /// </summary>
    private void OnDestroy()
    {
        ClearHighlight();
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
