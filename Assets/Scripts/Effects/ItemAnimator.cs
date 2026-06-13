using UnityEngine;

/// <summary>
/// Animates item visuals: spawn bounce, pickup fly-to-hand, drop shrink, and assembly arc.
/// Uses coroutines for smooth, non-blocking animations.
/// </summary>
public class ItemAnimator : MonoBehaviour
{
    public static ItemAnimator Instance { get; private set; }

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
    /// Scale from 0 to 1 with overshoot bounce. Call when a new visual appears.
    /// </summary>
    public void AnimateSpawn(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        StartCoroutine(SpawnCoroutine(target));
    }

    /// <summary>
    /// Scale from 1 to 0 quickly. Call when item is removed.
    /// </summary>
    public void AnimateDespawn(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        StartCoroutine(DespawnCoroutine(target));
    }

    /// <summary>
    /// Quick pop effect (scale up briefly then back). Call on state change.
    /// </summary>
    public void AnimatePop(GameObject target, float intensity = 1.2f)
    {
        if (target == null)
        {
            return;
        }

        StartCoroutine(PopCoroutine(target, intensity));
    }

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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
