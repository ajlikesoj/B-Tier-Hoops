using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Samples human movement during play and POSTs batches to the LangGraph sidecar (learning_service).
/// Pulls the latest profile periodically so <see cref="AIController"/> can adapt defense and reactions.
/// </summary>
public class OpponentLearningService : MonoBehaviour
{
    [Header("Service")]
    [Tooltip("Python service base URL (run: python -m uvicorn app:app --host 127.0.0.1 --port 8765 from learning_service/).")]
    public string baseUrl = "http://127.0.0.1:8765";

    [Tooltip("Disable to keep vanilla tier-only AI (no HTTP).")]
    public bool learningEnabled = true;

    [Header("Sampling")]
    public float sampleInterval = 0.2f;
    public float pushInterval = 4f;
    public float pullInterval = 7f;

    GameManager _gm;
    ShootingController _playerShooter;
    readonly List<TelemetrySample> _buffer = new List<TelemetrySample>(256);
    float _nextSample;
    float _nextPush;
    float _nextPull;
    Coroutine _running;
    Transform _lastAi;

    [Serializable]
    class TelemetryBatch
    {
        public TelemetrySample[] samples;
    }

    [Serializable]
    class TelemetryPostResponse
    {
        public bool ok;
        public OpponentLearningProfile profile;
    }

    void Awake()
    {
        _gm = GetComponent<GameManager>();
        if (_gm == null) _gm = GameManager.Instance;
    }

    void OnEnable()
    {
        if (_running == null && learningEnabled)
            _running = StartCoroutine(RunLoop());
    }

    void OnDisable()
    {
        if (_running != null)
        {
            StopCoroutine(_running);
            _running = null;
        }
    }

    IEnumerator RunLoop()
    {
        while (_gm == null)
        {
            _gm = GameManager.Instance;
            yield return null;
        }

        while (true)
        {
            while (_gm.player == null || _gm.ball == null)
                yield return null;

            if (_nextPush <= 0f)
            {
                _nextSample = Time.unscaledTime;
                _nextPush = Time.unscaledTime + pushInterval;
                _nextPull = Time.unscaledTime + pullInterval;
            }

            if (_gm.ai != _lastAi)
            {
                _lastAi = _gm.ai;
                if (_lastAi != null && learningEnabled)
                    yield return PullProfile();
            }

            if (_playerShooter == null && _gm.player != null)
                _playerShooter = _gm.player.GetComponent<ShootingController>();

            if (learningEnabled && _gm.State == GameManager.GameState.Playing && _gm.BallInPlay)
            {
                if (Time.unscaledTime >= _nextSample)
                {
                    _nextSample = Time.unscaledTime + sampleInterval;
                    CaptureSample();
                }

                if (Time.unscaledTime >= _nextPush && _buffer.Count > 0)
                {
                    _nextPush = Time.unscaledTime + pushInterval;
                    yield return PostTelemetry();
                }

                if (Time.unscaledTime >= _nextPull)
                {
                    _nextPull = Time.unscaledTime + pullInterval;
                    yield return PullProfile();
                }
            }

            yield return null;
        }
    }

    void CaptureSample()
    {
        var player = _gm.player;
        var ball = _gm.ball;
        if (player == null || ball == null) return;

        var rb = player.GetComponent<Rigidbody2D>();
        float vx = rb != null ? rb.linearVelocity.x : 0f;
        float vy = rb != null ? rb.linearVelocity.y : 0f;

        int owner = 0;
        if (ball.holder != null)
        {
            if (ball.holder == player) owner = 1;
            else if (_gm.ai != null && ball.holder == _gm.ai) owner = 2;
            else owner = 2;
        }

        bool charging = _playerShooter != null && _playerShooter.isCharging;

        var s = new TelemetrySample
        {
            t = Time.unscaledTime,
            px = player.position.x,
            py = player.position.y,
            vx = vx,
            vy = vy,
            ballOwner = owner,
            playerCharging = charging
        };
        _buffer.Add(s);
    }

    IEnumerator PostTelemetry()
    {
        if (_buffer.Count == 0) yield break;

        var batch = new TelemetryBatch { samples = _buffer.ToArray() };
        _buffer.Clear();

        string json = JsonUtility.ToJson(batch);
        string url = baseUrl.TrimEnd('/') + "/telemetry";

        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 4;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                yield break;

            try
            {
                var resp = JsonUtility.FromJson<TelemetryPostResponse>(req.downloadHandler.text);
                if (resp != null && resp.profile != null)
                    ApplyToAi(resp.profile);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BTierHoops] Learning parse error: {e.Message}");
            }
        }
    }

    IEnumerator PullProfile()
    {
        string url = baseUrl.TrimEnd('/') + "/profile";
        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 4;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
                yield break;

            try
            {
                var profile = JsonUtility.FromJson<OpponentLearningProfile>(req.downloadHandler.text);
                if (profile != null)
                    ApplyToAi(profile);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BTierHoops] Learning profile parse: {e.Message}");
            }
        }
    }

    void ApplyToAi(OpponentLearningProfile profile)
    {
        if (_gm == null || _gm.ai == null) return;
        var ai = _gm.ai.GetComponent<AIController>();
        if (ai == null) return;
        ai.SetOpponentLearningProfile(profile);
    }
}
