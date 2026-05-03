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
    [Tooltip("If true, start playing crowd loop automatically on Awake")]
    public bool playCrowdOnStart = true;

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
        // Try to auto-assign crowd clip if missing and an asset named crowdgood exists
        if (crowdClip == null)
        {
            string[] results = AssetDatabase.FindAssets("crowdgood t:AudioClip");
            if (results.Length == 0)
                results = AssetDatabase.FindAssets("crowd t:AudioClip");
            if (results.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(results[0]);
                crowdClip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
        }
    }
#endif
}
