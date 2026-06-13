using System.Collections.Generic;
using UnityEngine;

public static class KitchenItemVisualFactory
{
    private const string ModelPath = "Models/";

    private static Shader cachedShader;
    private static readonly Dictionary<Color, Material> materialCache = new Dictionary<Color, Material>();
    private static readonly Dictionary<string, GameObject> modelCache = new Dictionary<string, GameObject>();

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

    public static Material GetCachedMaterial(Color color)
    {
        if (!materialCache.TryGetValue(color, out var mat) || mat == null)
        {
            mat = new Material(GetCachedShader()) { color = color };
            materialCache[color] = mat;
        }
        return mat;
    }

    private static GameObject LoadCachedModel(string path)
    {
        if (!modelCache.TryGetValue(path, out var model))
        {
            model = Resources.Load<GameObject>(path);
            modelCache[path] = model;
        }
        return model;
    }

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
