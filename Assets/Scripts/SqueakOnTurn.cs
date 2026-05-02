using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SqueakOnTurn : MonoBehaviour
{
    [Header("Squeak")]
    [Tooltip("Clip to play when the visuals flip horizontally.")]
    public AudioClip squeakClip;

    [Tooltip("Minimum seconds between squeaks to avoid rapid repeats.")]
    public float cooldown = 0.12f;

    AudioSource _audio;
    float _prevScaleSign = 1f;
    float _lastPlayTime = -999f;

    void Awake()
    {
        _audio = GetComponent<AudioSource>();
        _audio.playOnAwake = false;
        // use PlayOneShot to avoid interfering with any preconfigured clip
        _audio.clip = null;

        // initialize previous sign from the current localScale.x on this transform
        _prevScaleSign = Mathf.Sign(transform.localScale.x != 0f ? transform.localScale.x : 1f);
    }

    void Update()
    {
        if (squeakClip == null) return;

        float sign = Mathf.Sign(transform.localScale.x != 0f ? transform.localScale.x : 1f);
        if (!Mathf.Approximately(sign, _prevScaleSign))
        {
            // flipped horizontally
            if (Time.time - _lastPlayTime >= cooldown)
            {
                _audio.PlayOneShot(squeakClip);
                _lastPlayTime = Time.time;
            }
            _prevScaleSign = sign;
        }
    }

    // Optional public trigger so other systems (AI/player controllers) can force a squeak.
    public void TriggerSqueak()
    {
        if (squeakClip == null) return;
        if (Time.time - _lastPlayTime < cooldown) return;
        _audio.PlayOneShot(squeakClip);
        _lastPlayTime = Time.time;
    }
}
