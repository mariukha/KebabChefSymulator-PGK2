/// \file KitchenItemVisualFactory.cs
/// \brief Plik zawierający statyczną klasę fabryki wizualnych reprezentacji
/// składników i dań kuchennych.
/// \details Klasa KitchenItemVisualFactory odpowiada za tworzenie obiektów 3D
/// reprezentujących składniki w grze. Obsługuje ładowanie modeli z zasobów
/// (Resources), tworzenie zastępczych prymitywów geometrycznych gdy model
/// nie jest dostępny, oraz generowanie rozproszonych wizualizacji składników
/// (np. posiekanych warzyw). Wykorzystuje buforowanie shaderów i materiałów
/// dla optymalizacji wydajności.

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Statyczna klasa fabryki tworząca wizualne reprezentacje 3D składników
/// i dań kuchennych w grze Kebab Chef Symulator.
/// </summary>
/// <remarks>
/// Klasa pełni rolę centralnej fabryki do tworzenia obiektów wizualnych
/// dla systemu kuchni. Obsługuje:
/// - Ładowanie modeli 3D z katalogu Resources/Models/ z buforowaniem,
/// - Tworzenie prymitywów zastępczych (kula, cylinder) gdy model nie jest dostępny,
/// - Generowanie rozproszonych wizualizacji składników (posiekane warzywa, sos),
/// - Buforowanie materiałów i shaderów dla optymalizacji wydajności,
/// - Automatyczne skalowanie modeli do pożądanego rozmiaru,
/// - Integrację z systemem animacji pojawiania się (<see cref="ItemAnimator"/>).
/// </remarks>
public static class KitchenItemVisualFactory
{
    /// <summary>
    /// Ścieżka bazowa do katalogu modeli w zasobach Unity (Resources).
    /// </summary>
    private const string ModelPath = "Models/";

    /// <summary>
    /// Buforowany shader używany do tworzenia materiałów.
    /// Preferuje URP Lit, awaryjnie Standard, ostatecznie Diffuse.
    /// </summary>
    private static Shader cachedShader;

    /// <summary>
    /// Słownik buforujący materiały po kolorze, aby uniknąć tworzenia
    /// duplikatów materiałów o tym samym kolorze.
    /// </summary>
    private static readonly Dictionary<Color, Material> materialCache = new Dictionary<Color, Material>();

    /// <summary>
    /// Słownik buforujący załadowane modele 3D (prefaby) po ścieżce zasobu,
    /// aby uniknąć wielokrotnego ładowania tego samego modelu z Resources.
    /// </summary>
    private static readonly Dictionary<string, GameObject> modelCache = new Dictionary<string, GameObject>();

    /// <summary>
    /// Zwraca nazwę pliku modelu 3D odpowiadającego danemu rodzajowi składnika
    /// i jego stanowi przetworzenia.
    /// </summary>
    /// <param name="kind">Rodzaj składnika (mięso, pomidor, cebula itp.).</param>
    /// <param name="state">Stan przetworzenia składnika (surowy, posiekany, ugotowany).</param>
    /// <param name="isDish">Czy obiekt jest gotowym daniem (kebabem).</param>
    /// <returns>
    /// Nazwa pliku modelu (bez ścieżki i rozszerzenia), lub <c>null</c>
    /// jeśli nie istnieje odpowiedni model dla danego rodzaju składnika.
    /// </returns>
    /// <remarks>
    /// Dla gotowych dań i kebabów zawsze zwraca "kebab_wrap".
    /// Pomidory i cebule mają warianty w zależności od stanu przetworzenia
    /// (cały vs posiekany). Pozostałe składniki mają stały model.
    /// </remarks>
    public static string GetModelName(IngredientKind kind, IngredientProcessState state, bool isDish)
    {
        if (isDish || kind == IngredientKind.Kebab)
        {
            return "kebab_wrap";
        }

        switch (kind)
        {
            case IngredientKind.Meat:
                return "meat_cooked";
            case IngredientKind.Tomato:
                return state == IngredientProcessState.Chopped ? "tomato_chopped" : "tomato_whole";
            case IngredientKind.Onion:
                return state == IngredientProcessState.Chopped ? "onion_chopped" : "onion_whole";
            case IngredientKind.Lettuce:
                return "lettuce_whole";
            case IngredientKind.GarlicSauce:
                return "sauce_bottle";
            case IngredientKind.Lavash:
                return "lavash";
            default:
                return null;
        }
    }

    /// <summary>
    /// Tworzy wizualną reprezentację 3D składnika lub dania jako obiekt potomny
    /// podanego rodzica.
    /// </summary>
    /// <param name="kind">Rodzaj składnika do zwizualizowania.</param>
    /// <param name="state">Stan przetworzenia składnika.</param>
    /// <param name="isDish">Czy obiekt jest gotowym daniem.</param>
    /// <param name="parent">Transformata rodzica, do którego zostanie dołączony model.</param>
    /// <param name="localPosition">Lokalna pozycja modelu względem rodzica.</param>
    /// <param name="localRotation">Lokalna rotacja modelu w stopniach (Euler).</param>
    /// <param name="targetSize">Docelowy rozmiar obiektu (największy wymiar zostanie dopasowany).</param>
    /// <returns>
    /// Utworzony obiekt wizualny, lub <c>null</c> jeśli nie można określić modelu.
    /// Jeśli prefab nie zostanie znaleziony, tworzona jest zastępcza wizualizacja
    /// za pomocą <see cref="CreateFallbackVisual"/>.
    /// </returns>
    /// <remarks>
    /// Model jest skalowany za pomocą <see cref="ScaleToSize"/> do podanego rozmiaru.
    /// Wszystkie kollidery w modelu i jego dzieciach są wyłączane.
    /// Jeśli dostępny jest <see cref="ItemAnimator"/>, uruchamiana jest animacja pojawiania.
    /// </remarks>
    public static GameObject CreateItemVisual(
        IngredientKind kind,
        IngredientProcessState state,
        bool isDish,
        Transform parent,
        Vector3 localPosition,
        Vector3 localRotation,
        float targetSize)
    {
        string modelName = GetModelName(kind, state, isDish);
        if (modelName == null) return null;

        GameObject prefab = LoadCachedModel(ModelPath + modelName);
        if (prefab == null)
        {

            return CreateFallbackVisual(kind, state, isDish, parent, localPosition, targetSize);
        }

        GameObject model = Object.Instantiate(prefab, parent);
        model.name = "ItemVisual_" + kind;
        model.transform.localPosition = localPosition;
        model.transform.localRotation = Quaternion.Euler(localRotation);
        model.transform.localScale = Vector3.one;

        ScaleToSize(model.transform, targetSize);

        Collider[] colliders = model.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        if (ItemAnimator.Instance != null)
        {
            ItemAnimator.Instance.AnimateSpawn(model);
        }

        return model;
    }

    /// <summary>
    /// Pobiera buforowany shader do tworzenia materiałów.
    /// Próbuje kolejno: URP Lit, Standard, Diffuse.
    /// </summary>
    /// <returns>Znaleziony shader, lub <c>null</c> jeśli żaden nie jest dostępny.</returns>
    private static Shader GetCachedShader()
    {
        if (cachedShader == null)
        {
            cachedShader = Shader.Find("Universal Render Pipeline/Lit");
            if (cachedShader == null) cachedShader = Shader.Find("Standard");
            if (cachedShader == null) cachedShader = Shader.Find("Diffuse");
        }
        return cachedShader;
    }

    /// <summary>
    /// Pobiera lub tworzy buforowany materiał o podanym kolorze.
    /// Jeśli materiał o danym kolorze już istnieje w cache, zwraca go;
    /// w przeciwnym razie tworzy nowy.
    /// </summary>
    /// <param name="color">Kolor materiału do pobrania lub utworzenia.</param>
    /// <returns>Materiał o podanym kolorze z odpowiednim shaderem.</returns>
    public static Material GetCachedMaterial(Color color)
    {
        if (!materialCache.TryGetValue(color, out var mat) || mat == null)
        {
            mat = new Material(GetCachedShader()) { color = color };
            materialCache[color] = mat;
        }
        return mat;
    }

    /// <summary>
    /// Ładuje model 3D z Resources i buforuje go w słowniku.
    /// Przy kolejnych wywołaniach z tą samą ścieżką zwraca buforowany wynik.
    /// </summary>
    /// <param name="path">Ścieżka zasobu modelu (względem katalogu Resources).</param>
    /// <returns>Załadowany prefab modelu, lub <c>null</c> jeśli zasób nie istnieje.</returns>
    private static GameObject LoadCachedModel(string path)
    {
        if (!modelCache.TryGetValue(path, out var model))
        {
            model = Resources.Load<GameObject>(path);
            modelCache[path] = model;
        }
        return model;
    }

    /// <summary>
    /// Tworzy zastępczą wizualizację składnika używając prymitywów geometrycznych
    /// (kula lub cylinder), gdy model 3D nie jest dostępny w zasobach.
    /// </summary>
    /// <param name="kind">Rodzaj składnika.</param>
    /// <param name="state">Stan przetworzenia składnika.</param>
    /// <param name="isDish">Czy obiekt jest gotowym daniem (kebabem) — jeśli tak, używany jest cylinder.</param>
    /// <param name="parent">Transformata rodzica.</param>
    /// <param name="localPosition">Lokalna pozycja obiektu względem rodzica.</param>
    /// <param name="targetSize">Docelowy rozmiar obiektu.</param>
    /// <returns>Utworzony obiekt zastępczy z odpowiednim materiałem i kolorem.</returns>
    /// <remarks>
    /// Dania i kebaby reprezentowane są jako cylindry (0.4 × 0.7 × 0.4 targetSize),
    /// pozostałe składniki jako kule. Kolider jest wyłączany.
    /// Kolor jest automatycznie dobierany na podstawie rodzaju składnika
    /// za pomocą <see cref="GetIngredientColor"/>.
    /// </remarks>
    private static GameObject CreateFallbackVisual(
        IngredientKind kind,
        IngredientProcessState state,
        bool isDish,
        Transform parent,
        Vector3 localPosition,
        float targetSize)
    {
        GameObject obj;
        if (isDish || kind == IngredientKind.Kebab)
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.transform.localScale = new Vector3(targetSize * 0.4f, targetSize * 0.7f, targetSize * 0.4f);
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.transform.localScale = Vector3.one * targetSize;
        }

        obj.name = "ItemVisual_" + kind + "_fallback";
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = Quaternion.identity;

        Collider col = obj.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.sharedMaterial = GetCachedMaterial(GetIngredientColor(kind, state));
        }

        if (ItemAnimator.Instance != null)
        {
            ItemAnimator.Instance.AnimateSpawn(obj);
        }

        return obj;
    }

    /// <summary>
    /// Tworzy wizualizację rozproszonych kawałków składnika (np. posiekane warzywa
    /// lub krople sosu), losowo rozmieszczonych w obrębie kontenera.
    /// </summary>
    /// <param name="kind">Rodzaj składnika do zwizualizowania.</param>
    /// <param name="state">Stan przetworzenia składnika.</param>
    /// <param name="parent">Transformata rodzica.</param>
    /// <param name="localPosition">Lokalna pozycja kontenera względem rodzica.</param>
    /// <param name="count">Liczba kawałków do wygenerowania.</param>
    /// <param name="spread">Maksymalny zasięg rozproszenia kawałków na osiach X i Z.</param>
    /// <param name="pieceSize">Bazowy rozmiar pojedynczego kawałka.</param>
    /// <returns>Kontener GameObject zawierający wszystkie wygenerowane kawałki.</returns>
    /// <remarks>
    /// Sos czosnkowy (<see cref="IngredientKind.GarlicSauce"/>) jest reprezentowany
    /// jako spłaszczone kule, pozostałe składniki jako spłaszczone kostki.
    /// Każdy kawałek ma losową pozycję, rotację i lekki offset na osi Y.
    /// Kollidery kawałków są usuwane. Używany jest współdzielony materiał
    /// z cache dla optymalizacji.
    /// </remarks>
    public static GameObject CreateScatteredVisual(
        IngredientKind kind,
        IngredientProcessState state,
        Transform parent,
        Vector3 localPosition,
        int count,
        float spread,
        float pieceSize)
    {
        GameObject container = new GameObject("Scattered_" + kind);
        container.transform.SetParent(parent, false);
        container.transform.localPosition = localPosition;
        container.transform.localRotation = Quaternion.identity;

        Color col = GetIngredientColor(kind, state);
        Material mat = GetCachedMaterial(col);

        for (int i = 0; i < count; i++)
        {
            GameObject piece;
            if (kind == IngredientKind.GarlicSauce)
            {
                piece = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                piece.transform.localScale = new Vector3(pieceSize, pieceSize * 0.3f, pieceSize);
            }
            else
            {
                piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.transform.localScale = new Vector3(pieceSize, pieceSize * 0.2f, pieceSize * 1.5f);
            }

            Collider collider = piece.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);

            float rx = Random.Range(-spread, spread);
            float rz = Random.Range(-spread, spread);
            float ry = Random.Range(0f, 0.015f);

            piece.transform.SetParent(container.transform, false);
            piece.transform.localPosition = new Vector3(rx, ry, rz);

            float rotY = Random.Range(0f, 360f);
            float rotX = Random.Range(-20f, 20f);
            float rotZ = Random.Range(-20f, 20f);
            piece.transform.localRotation = Quaternion.Euler(rotX, rotY, rotZ);

            Renderer r = piece.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
        }

        if (ItemAnimator.Instance != null)
        {
            ItemAnimator.Instance.AnimateSpawn(container);
        }

        return container;
    }

    /// <summary>
    /// Zwraca kolor reprezentujący dany rodzaj składnika i jego stan przetworzenia.
    /// Używany do kolorowania prymitywów zastępczych i rozproszonych wizualizacji.
    /// </summary>
    /// <param name="kind">Rodzaj składnika.</param>
    /// <param name="state">Stan przetworzenia składnika.</param>
    /// <returns>
    /// Kolor charakterystyczny dla danego składnika:
    /// - Mięso: brązowy (ugotowane) lub czerwonawy (surowe),
    /// - Pomidor: czerwony,
    /// - Cebula: kremowy,
    /// - Sałata: zielony,
    /// - Sos czosnkowy: jasny beżowy,
    /// - Lawasz: ciepły beżowy,
    /// - Kebab: brązowo-złoty,
    /// - Domyślny: szary.
    /// </returns>
    public static Color GetIngredientColor(IngredientKind kind, IngredientProcessState state)
    {
        switch (kind)
        {
            case IngredientKind.Meat:
                return state == IngredientProcessState.Cooked
                    ? new Color(0.55f, 0.30f, 0.15f)
                    : new Color(0.65f, 0.25f, 0.18f);
            case IngredientKind.Tomato:
                return new Color(0.86f, 0.2f, 0.2f);
            case IngredientKind.Onion:
                return new Color(0.93f, 0.9f, 0.75f);
            case IngredientKind.Lettuce:
                return new Color(0.35f, 0.7f, 0.25f);
            case IngredientKind.GarlicSauce:
                return new Color(0.95f, 0.95f, 0.85f);
            case IngredientKind.Lavash:
                return new Color(0.86f, 0.74f, 0.5f);
            case IngredientKind.Kebab:
                return new Color(0.76f, 0.57f, 0.35f);
            default:
                return new Color(0.7f, 0.7f, 0.7f);
        }
    }

    /// <summary>
    /// Skaluje model 3D tak, aby jego największy wymiar odpowiadał podanemu rozmiarowi docelowemu.
    /// </summary>
    /// <param name="modelRoot">Transformata korzenia modelu do przeskalowania.</param>
    /// <param name="targetSize">Docelowy rozmiar największego wymiaru modelu.</param>
    /// <remarks>
    /// Oblicza otoczkę (bounding box) ze wszystkich rendererów w modelu i jego dzieciach,
    /// wyznacza największy wymiar, a następnie skaluje cały model jednorodnie.
    /// Pomija modele o wymiarze mniejszym niż 0.001, aby uniknąć dzielenia przez zero.
    /// </remarks>
    private static void ScaleToSize(Transform modelRoot, float targetSize)
    {
        Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (maxDimension <= 0.001f) return;

        float scaleFactor = targetSize / maxDimension;
        modelRoot.localScale *= scaleFactor;
    }
}
