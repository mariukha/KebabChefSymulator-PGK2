using UnityEngine;

/// <summary>
/// Helper class to create 3D visual representations of kitchen items
/// using the project's GLB models from Resources/Models/.
/// Used for held item display and station item display.
/// </summary>
public static class KitchenItemVisualFactory
{
    private const string ModelPath = "Models/";

    /// <summary>
    /// Returns the model resource name and color for a given ingredient.
    /// </summary>
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
    /// Creates a 3D model visual for a kitchen item.
    /// Returns the root GameObject or null if model not found.
    /// </summary>
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

        GameObject prefab = Resources.Load<GameObject>(ModelPath + modelName);
        if (prefab == null)
        {
            // Fallback: create a colored primitive
            return CreateFallbackVisual(kind, state, isDish, parent, localPosition, targetSize);
        }

        GameObject model = Object.Instantiate(prefab, parent);
        model.name = "ItemVisual_" + kind;
        model.transform.localPosition = localPosition;
        model.transform.localRotation = Quaternion.Euler(localRotation);
        model.transform.localScale = Vector3.one;

        // Scale to target size
        ScaleToSize(model.transform, targetSize);

        // Disable all colliders
        Collider[] colliders = model.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        return model;
    }

    /// <summary>
    /// Creates a simple colored primitive as fallback when model is missing.
    /// </summary>
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
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            rend.material = new Material(shader);
            rend.material.color = GetIngredientColor(kind, state);
        }

        return obj;
    }

    /// <summary>
    /// Returns characteristic color for an ingredient.
    /// </summary>
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
    /// Scales a model so its largest dimension matches targetSize.
    /// </summary>
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
