using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Camera effects suite: head bob while walking, screen shake on events,
/// colored screen flash overlay, and landing bob.
/// Attach to the player camera object.
/// </summary>
public class CameraEffects : MonoBehaviour
{
    public static CameraEffects Instance { get; private set; }

    [Header("Head Bob")]
    public float bobFrequency = 8f;
    public float bobAmplitudeY = 0.035f;
    public float bobAmplitudeX = 0.015f;

    [Header("Screen Shake")]
    private float shakeIntensity;
    private float shakeDuration;
    private float shakeTimer;

    [Header("Screen Flash")]
    private Image flashOverlay;
    private float flashTimer;
    private float flashDuration;
    private Color flashColor;

    [Header("Landing Bob")]
    private bool wasGrounded = true;
    private float landingBobTimer;

    private Vector3 baseLocalPosition;
    private float bobTimer;
    private CharacterController trackedController;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        baseLocalPosition = transform.localPosition;
    }

    private void Start()
    {
        CreateFlashOverlay();
    }

    /// <summary>
    /// Call from SimplePlayerController to feed movement data.
    /// </summary>
    public void SetTrackedController(CharacterController controller)
    {
        trackedController = controller;
    }

    /// <summary>
    /// Trigger screen shake (e.g. on delivery, chop).
    /// </summary>
    public void ShakeCamera(float intensity, float duration)
    {
        shakeIntensity = intensity;
        shakeDuration = duration;
        shakeTimer = duration;
    }

    /// <summary>
    /// Flash the screen with a color overlay (green = success, red = fail).
    /// </summary>
    public void FlashScreen(Color color, float duration = 0.3f)
    {
        flashColor = color;
        flashDuration = duration;
        flashTimer = duration;
    }

    private void LateUpdate()
    {
        Vector3 offset = Vector3.zero;

        offset += CalculateHeadBob();
        offset += CalculateShake();
        offset += CalculateLandingBob();

        transform.localPosition = baseLocalPosition + offset;

        UpdateFlashOverlay();
    }

    private Vector3 CalculateHeadBob()
    {
        if (trackedController == null)
        {
            return Vector3.zero;
        }

        Vector3 velocity = trackedController.velocity;
        float horizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;

        if (horizontalSpeed < 0.5f)
        {

            bobTimer = Mathf.Lerp(bobTimer, 0f, Time.deltaTime * 4f);
            return Vector3.zero;
        }

        bobTimer += Time.deltaTime * bobFrequency;

        float bobY = Mathf.Sin(bobTimer) * bobAmplitudeY;
        float bobX = Mathf.Sin(bobTimer * 0.5f) * bobAmplitudeX;

        return new Vector3(bobX, bobY, 0f);
    }

    private Vector3 CalculateShake()
    {
        if (shakeTimer <= 0f)
        {
            return Vector3.zero;
        }

        shakeTimer -= Time.deltaTime;
        float progress = shakeTimer / shakeDuration;
        float decayingIntensity = shakeIntensity * progress;

        return new Vector3(
            Random.Range(-1f, 1f) * decayingIntensity,
            Random.Range(-1f, 1f) * decayingIntensity,
            0f);
    }

    private Vector3 CalculateLandingBob()
    {
        if (trackedController != null)
        {
            bool grounded = trackedController.isGrounded;
            if (grounded && !wasGrounded)
            {
                landingBobTimer = 0.2f;
            }

            wasGrounded = grounded;
        }

        if (landingBobTimer <= 0f)
        {
            return Vector3.zero;
        }

        landingBobTimer -= Time.deltaTime;
        float t = landingBobTimer / 0.2f;
        float dip = Mathf.Sin(t * Mathf.PI) * 0.06f;
        return new Vector3(0f, -dip, 0f);
    }

    private void CreateFlashOverlay()
    {

        Canvas canvas = null;
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Canvas c in allCanvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvas = c;
                break;
            }
        }

        if (canvas == null)
        {
            return;
        }

        GameObject flashObj = new GameObject("ScreenFlash");
        flashObj.transform.SetParent(canvas.transform, false);

        flashOverlay = flashObj.AddComponent<Image>();
        flashOverlay.color = new Color(0f, 0f, 0f, 0f);
        flashOverlay.raycastTarget = false;

        RectTransform rect = flashObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void UpdateFlashOverlay()
    {
        if (flashOverlay == null || flashTimer <= 0f)
        {
            return;
        }

        flashTimer -= Time.deltaTime;
        float alpha = Mathf.Clamp01(flashTimer / flashDuration) * 0.25f;
        flashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);

        if (flashTimer <= 0f)
        {
            flashOverlay.color = new Color(0f, 0f, 0f, 0f);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
