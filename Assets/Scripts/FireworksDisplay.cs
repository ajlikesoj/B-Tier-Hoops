using System.Collections;
using UnityEngine;

/// <summary>
/// Spawns colorful particle-burst fireworks in the sky behind the win panel and triggers the
/// firecracker audio at a tier-scaled volume. The bigger the opponent you beat, the bigger
/// (and louder) the celebration: F=nothing, S=full show.
/// </summary>
public class FireworksDisplay : MonoBehaviour
{
    [Header("Spawn area (world coords, upper sky)")]
    public Vector2 spawnAreaCenter = new Vector2(0f, 4f);
    public Vector2 spawnAreaSize   = new Vector2(20f, 3.5f);
    public float spawnZ = 4.0f;        // in front of clouds (z=4.7) but behind characters

    [Header("Per-tier celebration scale (indexed by AIController.Tier: F, D, C, B, A, S)")]
    public int[] burstsPerTier      = new int[] { 0, 3, 6, 12, 22, 40 };
    [Range(0f, 1f)] public float[] audioVolumePerTier = new float[] { 0f, 0.30f, 0.50f, 0.70f, 0.85f, 1.00f };

    [Header("Per-burst look")]
    public int particlesPerBurst = 12;
    public float particleSpeed   = 3.2f;
    public float particleLife    = 0.95f;
    public float particleSize    = 0.18f;
    public float gravity         = 1.4f;
    [Tooltip("Max time the whole celebration is spread across (seconds). Bigger crowds → denser show in same window.")]
    public float celebrationWindow = 4f;

    static readonly Color[] FireworkColors = new[]
    {
        new Color(1.00f, 0.30f, 0.30f),
        new Color(0.30f, 1.00f, 0.45f),
        new Color(0.30f, 0.65f, 1.00f),
        new Color(1.00f, 0.85f, 0.30f),
        new Color(0.85f, 0.45f, 1.00f),
        new Color(1.00f, 0.60f, 0.20f),
        new Color(0.40f, 1.00f, 1.00f),
        new Color(1.00f, 1.00f, 1.00f),
    };

    /// <summary>Trigger the celebration for the given tier the player just beat.</summary>
    public void Celebrate(AIController.Tier tier)
    {
        int idx = (int)tier;
        int burstCount = (burstsPerTier != null && idx < burstsPerTier.Length) ? burstsPerTier[idx] : 0;
        float vol = (audioVolumePerTier != null && idx < audioVolumePerTier.Length) ? audioVolumePerTier[idx] : 0f;
        if (burstCount <= 0 && vol <= 0f) return;  // F-tier — no celebration at all

        // Audio (scaled). SoundHandler holds the matchWinClip — auto-loaded from firecrackers.aiff
        if (vol > 0f && SoundHandler.Instance != null && SoundHandler.Instance.matchWinClip != null)
        {
            SoundHandler.Instance.PlayClip(SoundHandler.Instance.matchWinClip, vol);
        }

        // Visual show
        if (burstCount > 0) StartCoroutine(RunShow(burstCount));
    }

    IEnumerator RunShow(int burstCount)
    {
        // Spread bursts across celebrationWindow with light random jitter — denser shows feel busier
        float avgInterval = celebrationWindow / Mathf.Max(1, burstCount);
        for (int i = 0; i < burstCount; i++)
        {
            SpawnBurst();
            float wait = avgInterval * Random.Range(0.4f, 1.6f);
            yield return new WaitForSeconds(wait);
        }
    }

    void SpawnBurst()
    {
        Vector3 origin = new Vector3(
            spawnAreaCenter.x + Random.Range(-spawnAreaSize.x * 0.5f, spawnAreaSize.x * 0.5f),
            spawnAreaCenter.y + Random.Range(-spawnAreaSize.y * 0.5f, spawnAreaSize.y * 0.5f),
            spawnZ);
        Color color = FireworkColors[Random.Range(0, FireworkColors.Length)];

        // White flash at the burst origin
        StartCoroutine(AnimateFlash(origin));

        // Radial particles
        int n = particlesPerBurst;
        for (int i = 0; i < n; i++)
        {
            float angle = (i / (float)n) * Mathf.PI * 2f + Random.Range(-0.05f, 0.05f);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float speed = particleSpeed * Random.Range(0.85f, 1.15f);
            StartCoroutine(AnimateParticle(origin, dir, color, speed));
        }
    }

    IEnumerator AnimateFlash(Vector3 origin)
    {
        var go = new GameObject("Flash");
        go.transform.SetParent(transform);
        go.transform.position = origin;
        go.transform.localScale = Vector3.one * 0.6f;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CharacterFactory.CircleSprite;
        sr.color = Color.white;
        sr.sortingOrder = -10;

        float t = 0f;
        const float life = 0.18f;
        while (t < life)
        {
            t += Time.deltaTime;
            float p = t / life;
            sr.color = new Color(1f, 1f, 1f, 1f - p);
            go.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.6f, p);
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator AnimateParticle(Vector3 start, Vector2 dir, Color color, float speed)
    {
        var go = new GameObject("Particle");
        go.transform.SetParent(transform);
        go.transform.position = start;
        go.transform.localScale = Vector3.one * particleSize;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CharacterFactory.CircleSprite;
        sr.color = color;
        sr.sortingOrder = -10;

        float t = 0f;
        while (t < particleLife)
        {
            t += Time.deltaTime;
            float p = t / particleLife;
            float ease = 1f - Mathf.Pow(1f - p, 2f);                  // ease-out outward
            float dist = ease * speed * particleLife;
            float drop = -gravity * p * p;                            // gentle gravity drop
            go.transform.position = start + new Vector3(dir.x * dist, dir.y * dist + drop, 0f);
            sr.color = new Color(color.r, color.g, color.b, 1f - p);
            yield return null;
        }
        Destroy(go);
    }
}
