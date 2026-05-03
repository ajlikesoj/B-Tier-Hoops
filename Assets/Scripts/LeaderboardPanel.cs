using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime UI on the main menu: opens a panel that GETs /leaderboard from learning_service
/// and lists wins, samples, and inferred playstyle per stored user profile.
/// </summary>
public class LeaderboardPanel : MonoBehaviour
{
    [SerializeField] string _baseUrl = "http://127.0.0.1:8765";
    GameObject _overlayRoot;
    TMP_Text _body;
    Coroutine _fetch;

    [Serializable]
    class LeaderboardResponse
    {
        public LeaderboardEntry[] entries;
    }

    [Serializable]
    class LeaderboardEntry
    {
        public string userId;
        public string displayName;
        public int wins;
        public int losses;
        public int sampleCount;
        public string playerType;
    }

    public static LeaderboardPanel Ensure(Canvas canvas, string baseUrl)
    {
        if (canvas == null) return null;
        var existing = canvas.GetComponentInChildren<LeaderboardPanel>(true);
        if (existing != null)
        {
            existing._baseUrl = baseUrl;
            return existing;
        }
        var go = new GameObject("LeaderboardPanelHost");
        go.transform.SetParent(canvas.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var panel = go.AddComponent<LeaderboardPanel>();
        panel._baseUrl = baseUrl;
        panel.BuildUi(canvas.transform);
        return panel;
    }

    public void SetBaseUrl(string url) => _baseUrl = url;

    void BuildUi(Transform canvasTransform)
    {
        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        var openGo = new GameObject("LeaderboardOpenButton");
        openGo.transform.SetParent(canvasTransform, false);
        var openRt = openGo.AddComponent<RectTransform>();
        openRt.anchorMin = new Vector2(1f, 1f);
        openRt.anchorMax = new Vector2(1f, 1f);
        openRt.pivot = new Vector2(1f, 1f);
        openRt.anchoredPosition = new Vector2(-20f, -20f);
        openRt.sizeDelta = new Vector2(280f, 58f);
        openGo.AddComponent<Image>().color = new Color(0.12f, 0.42f, 0.58f);
        var openBtn = openGo.AddComponent<Button>();
        var openTxt = MakeText(openGo.transform, "Txt", "LEADERBOARD", 28, TextAlignmentOptions.Center, font);
        StretchFull(openTxt.rectTransform);
        openTxt.fontStyle = FontStyles.Bold;
        openBtn.onClick.AddListener(Show);

        _overlayRoot = new GameObject("LeaderboardOverlay");
        _overlayRoot.transform.SetParent(canvasTransform, false);
        var oRt = _overlayRoot.AddComponent<RectTransform>();
        oRt.anchorMin = Vector2.zero;
        oRt.anchorMax = Vector2.one;
        oRt.offsetMin = Vector2.zero;
        oRt.offsetMax = Vector2.zero;
        _overlayRoot.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.9f);

        var title = MakeText(_overlayRoot.transform, "Title", "PROFILES & LEADERBOARD",
            52, TextAlignmentOptions.Center, font);
        var titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -40f);
        titleRt.sizeDelta = new Vector2(1600f, 80f);
        title.fontStyle = FontStyles.Bold;

        var hint = MakeText(_overlayRoot.transform, "Hint",
            "Ranked by wins, then telemetry samples. Playstyle comes from movement patterns (learning_service).",
            26, TextAlignmentOptions.Center, font);
        var hintRt = hint.rectTransform;
        hintRt.anchorMin = new Vector2(0.5f, 1f);
        hintRt.anchorMax = new Vector2(0.5f, 1f);
        hintRt.pivot = new Vector2(0.5f, 1f);
        hintRt.anchoredPosition = new Vector2(0f, -120f);
        hintRt.sizeDelta = new Vector2(1500f, 60f);
        hint.color = new Color(0.75f, 0.78f, 0.85f);

        var bodyGo = new GameObject("BodyScroll");
        bodyGo.transform.SetParent(_overlayRoot.transform, false);
        var bodyRt = bodyGo.AddComponent<RectTransform>();
        bodyRt.anchorMin = new Vector2(0.05f, 0.12f);
        bodyRt.anchorMax = new Vector2(0.95f, 0.82f);
        bodyRt.offsetMin = Vector2.zero;
        bodyRt.offsetMax = Vector2.zero;
        var scroll = bodyGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(bodyGo.transform, false);
        var vpRt = viewport.AddComponent<RectTransform>();
        StretchFull(vpRt);
        viewport.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        scroll.viewport = vpRt;

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var cRt = content.AddComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(0.5f, 1f);
        cRt.anchoredPosition = Vector2.zero;
        cRt.sizeDelta = new Vector2(0f, 800f);
        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var le = content.AddComponent<VerticalLayoutGroup>();
        le.childAlignment = TextAnchor.UpperCenter;
        le.spacing = 8f;
        le.padding = new RectOffset(24, 24, 16, 16);
        scroll.content = cRt;

        _body = MakeText(content.transform, "BodyText", "Loading…", 30, TextAlignmentOptions.TopLeft, font);
        var bRt = _body.rectTransform;
        bRt.anchorMin = new Vector2(0f, 1f);
        bRt.anchorMax = new Vector2(1f, 1f);
        bRt.pivot = new Vector2(0.5f, 1f);
        bRt.sizeDelta = new Vector2(0f, 0f);
        var bodyFitter = _body.gameObject.AddComponent<ContentSizeFitter>();
        bodyFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        _body.enableWordWrapping = true;

        var closeGo = new GameObject("CloseButton");
        closeGo.transform.SetParent(_overlayRoot.transform, false);
        var cBtnRt = closeGo.AddComponent<RectTransform>();
        cBtnRt.anchorMin = new Vector2(0.5f, 0f);
        cBtnRt.anchorMax = new Vector2(0.5f, 0f);
        cBtnRt.pivot = new Vector2(0.5f, 0f);
        cBtnRt.anchoredPosition = new Vector2(0f, 36f);
        cBtnRt.sizeDelta = new Vector2(320f, 72f);
        closeGo.AddComponent<Image>().color = new Color(0.35f, 0.35f, 0.4f);
        var closeBtn = closeGo.AddComponent<Button>();
        var closeTxt = MakeText(closeGo.transform, "CloseTxt", "BACK", 34, TextAlignmentOptions.Center, font);
        StretchFull(closeTxt.rectTransform);
        closeTxt.fontStyle = FontStyles.Bold;
        closeBtn.onClick.AddListener(Hide);

        _overlayRoot.SetActive(false);
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size,
        TextAlignmentOptions align, TMP_FontAsset font)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = Color.white;
        if (font != null) tmp.font = font;
        return tmp;
    }

    static void StretchFull(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    public void Show()
    {
        if (_overlayRoot == null) return;
        _overlayRoot.SetActive(true);
        if (_fetch != null) StopCoroutine(_fetch);
        _fetch = StartCoroutine(FetchAndRender());
    }

    public void Hide()
    {
        if (_overlayRoot != null) _overlayRoot.SetActive(false);
        if (_fetch != null)
        {
            StopCoroutine(_fetch);
            _fetch = null;
        }
    }

    IEnumerator FetchAndRender()
    {
        _body.text = "Loading leaderboard…";
        string url = _baseUrl.TrimEnd('/') + "/leaderboard?limit=40";
        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 8;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                _body.text = "Could not reach learning_service.\n\nStart it from the learning_service folder:\n" +
                             "python -m uvicorn app:app --host 127.0.0.1 --port 8765\n\n" + req.error;
                yield break;
            }

            try
            {
                var resp = JsonUtility.FromJson<LeaderboardResponse>(req.downloadHandler.text);
                _body.text = FormatEntries(resp != null ? resp.entries : null);
            }
            catch (Exception e)
            {
                _body.text = "Parse error: " + e.Message;
            }
        }
    }

    static string FormatEntries(LeaderboardEntry[] entries)
    {
        if (entries == null || entries.Length == 0)
            return "No profiles yet.\n\nPlay matches with your name set and the learning service running — each player gets a stored profile from telemetry.";

        var sb = new StringBuilder();
        int rank = 1;
        foreach (var e in entries)
        {
            string name = string.IsNullOrEmpty(e.displayName) ? e.userId : e.displayName;
            sb.AppendLine($"<b>#{rank}  {name}</b>  <size=85%>(id: {e.userId})</size>");
            sb.AppendLine($"   <color=#9ecfff>Playstyle:</color> {e.playerType}");
            sb.AppendLine($"   Record: {e.wins}W – {e.losses}L   ·   Telemetry samples: {e.sampleCount}");
            sb.AppendLine();
            rank++;
        }
        return sb.ToString();
    }
}
