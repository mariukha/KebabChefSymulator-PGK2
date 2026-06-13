using UnityEngine;

/// <summary>
/// Adds a visual highlight effect to interactable objects when the player looks at them.
/// Uses emission color boost on the object's renderers (no custom shaders needed).
/// Also creates a subtle scale pulse on the highlighted object.
/// </summary>
public class InteractionHighlight : MonoBehaviour
{
    public static InteractionHighlight Instance { get; private set; }

    private GameObject currentTarget;
    private Renderer[] currentRenderers;
    private Color[] originalEmissions;
    private bool isHighlighting;

    private float pulsePhase;
    private Vector3[] originalScales;

    private static readonly Color HighlightEmission = new Color(0.35f, 0.28f, 0.12f);
    private const float PulseAmplitude = 0.012f;
    private const float PulseSpeed = 4f;

    private static MaterialPropertyBlock _propBlock;
    private static MaterialPropertyBlock propBlock
    {
        get
        {
            if (_propBlock == null)
                _propBlock = new MaterialPropertyBlock();
            return _propBlock;
        }
    }

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
    /// Call every frame from PlayerInteraction with the current target (or null).
    /// </summary>
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

    private void OnDestroy()
    {
        ClearHighlight();
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
