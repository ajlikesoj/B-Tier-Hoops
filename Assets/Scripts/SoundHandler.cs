using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SoundHandler : MonoBehaviour
{
    public static SoundHandler Instance { get; private set; }

    // One-shot sound source (effects)
    private AudioSource _sfxSource;
    // Background crowd loop source
    private AudioSource _crowdSource;
    // General purpose looping source for things like charge-up
    private AudioSource _loopSource;

    [Header("Background")]
    [Tooltip("Looping crowd audio clip (will play on Awake if PlayCrowdOnStart is true)")]
    public AudioClip crowdClip;
    [Tooltip("Volume for crowd loop (0-1)")]
    [Range(0f, 1f)] public float crowdVolume = 0.6f;
    [Tooltip("If true, start playing crowd loop automatically on Awake. Default false — CrowdManager decides whether to play and at what volume based on tier attendance, and MainMenu keeps it silent.")]
    public bool playCrowdOnStart = false;

    [Header("Click & Celebration Clips (auto-loaded by OnValidate)")]
    [Tooltip("Short click — UI buttons (menu tiles, name submit, return-to-menu, reset progress).")]
    public AudioClip buttonClip;
    [Range(0f, 1f)] public float buttonVolume = 0.7f;
    [Tooltip("Heavier impact — slam dunks (player + AI).")]
    public AudioClip dunkBoomClip;
    [Range(0f, 1f)] public float dunkBoomVolume = 0.9f;
    [Tooltip("Quick celebratory burst on every made basket.")]
    public AudioClip scoreCrackerClip;
    [Range(0f, 1f)] public float scoreCrackerVolume = 0.6f;
    [Tooltip("Long firecracker celebration — played when the player wins a match.")]
    public AudioClip matchWinClip;
    [Range(0f, 1f)] public float matchWinVolume = 0.85f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _sfxSource = GetComponent<AudioSource>();
        if (_sfxSource == null)
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
        }
        _sfxSource.playOnAwake = false;
        _sfxSource.spatialBlend = 0f; // 2D audio for UI/effects

        // separate source for crowd loop so it can loop independently
        _crowdSource = gameObject.AddComponent<AudioSource>();
        _crowdSource.playOnAwake = false;
        _crowdSource.loop = true;
        _crowdSource.spatialBlend = 0f;
        _crowdSource.volume = crowdVolume;

        // loop source for effects that need sustained playback (charge-up, etc.)
        _loopSource = gameObject.AddComponent<AudioSource>();
        _loopSource.playOnAwake = false;
        _loopSource.loop = true;
        _loopSource.spatialBlend = 0f;

        Debug.Log("[BTierHoops] SoundHandler initialized");

        if (playCrowdOnStart && crowdClip != null)
        {
            PlayCrowd(true);
        }
    }

    public void PlayClip(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
        {
            Debug.LogError("[BTierHoops] PlayClip called with null clip!");
            return;
        }

        if (_sfxSource != null)
        {
            _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }
    }

    /// <summary>Play a looping clip (e.g. charge-up). Stops any previous loop.</summary>
    public void PlayLoop(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        if (_loopSource == null) return;
        _loopSource.clip = clip;
        _loopSource.volume = Mathf.Clamp01(volume);
        if (!_loopSource.isPlaying) _loopSource.Play();
    }

    /// <summary>Stop the current loop-playing source.</summary>
    public void StopLoop()
    {
        if (_loopSource == null) return;
        if (_loopSource.isPlaying) _loopSource.Stop();
    }

    /// <summary>UI button click — call from any onClick handler.</summary>
    public void PlayButton() { if (buttonClip != null) PlayClip(buttonClip, buttonVolume); }

    /// <summary>Heavier impact for slam dunks.</summary>
    public void PlayDunkBoom() { if (dunkBoomClip != null) PlayClip(dunkBoomClip, dunkBoomVolume); }

    /// <summary>Quick crackle on every made basket (in addition to the swish).</summary>
    public void PlayScoreCelebration() { if (scoreCrackerClip != null) PlayClip(scoreCrackerClip, scoreCrackerVolume); }

    /// <summary>Long firecracker burst when the player wins a match.</summary>
    public void PlayMatchWin() { if (matchWinClip != null) PlayClip(matchWinClip, matchWinVolume); }

    /// <summary>Start or stop the looping crowd audio. If clip is null it uses the configured crowdClip.</summary>
    public void PlayCrowd(bool play, AudioClip clip = null, float volume = -1f)
    {
        if (_crowdSource == null) return;
        if (!play)
        {
            if (_crowdSource.isPlaying) _crowdSource.Stop();
            return;
        }
        AudioClip use = clip != null ? clip : crowdClip;
        if (use == null) return;
        _crowdSource.clip = use;
        if (volume >= 0f) _crowdSource.volume = Mathf.Clamp01(volume);
        else _crowdSource.volume = crowdVolume;
        if (!_crowdSource.isPlaying) _crowdSource.Play();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (crowdClip == null)        crowdClip        = TryFindClip("crowdgood", "crowd");
        if (buttonClip == null)       buttonClip       = TryFindClip("button");
        if (dunkBoomClip == null)     dunkBoomClip     = TryFindClip("boom");
        if (scoreCrackerClip == null) scoreCrackerClip = TryFindClip("shortburstcracker", "shortburst");
        if (matchWinClip == null)     matchWinClip     = TryFindClip("firecrackers", "firecracker");
    }

    static AudioClip TryFindClip(params string[] names)
    {
        foreach (var n in names)
        {
            string[] results = AssetDatabase.FindAssets($"{n} t:AudioClip");
            if (results.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(results[0]);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null) return clip;
            }
        }
        return null;
    }
#endif
}
