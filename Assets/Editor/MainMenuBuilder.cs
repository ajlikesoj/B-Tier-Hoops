using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Linq;

public static class MainMenuBuilder
{
    static readonly Color BgDark = new Color(0.06f, 0.05f, 0.09f);
    static readonly Color CardBg = new Color(0.10f, 0.10f, 0.13f, 0.95f);

    [MenuItem("BTierHoops/Build Main Menu Scene")]
    public static void BuildMainMenu()
    {
        // Prompt to save the currently-open scene (so we don't lose Game.unity work) before
        // creating a fresh untitled scene that we'll save over Assets/Scenes/MainMenu.unity.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var canvasGO = MakeCanvas("UI");
        var canvas = canvasGO.GetComponent<Canvas>();

        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        // Full-screen dark background
        MakeFullScreenImage(canvasGO.transform, "Background", BgDark);

        // Title
        var title = MakeText(canvasGO.transform, "Title", "B-TIER HOOPS",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -60f), new Vector2(0f, 130f),
            120f, TextAlignmentOptions.Center, font);
        title.fontStyle = FontStyles.Bold;

        // Greeting (welcome + name)
        var greeting = MakeText(canvasGO.transform, "Greeting", "WELCOME",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -200f), new Vector2(0f, 60f),
            42f, TextAlignmentOptions.Center, font);
        greeting.color = new Color(0.85f, 0.85f, 0.90f);

        // Result banner (last match outcome — populated by MainMenuController)
        var banner = MakeText(canvasGO.transform, "ResultBanner", "",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -270f), new Vector2(0f, 60f),
            38f, TextAlignmentOptions.Center, font);
        banner.fontStyle = FontStyles.Bold;

        // 6 tier tiles in a 3×2 grid, centered on canvas
        // Tile size 440×240; column centers at -480 / 0 / +480; row centers at -80 / -380 (relative to canvas center)
        var tileLayout = new[] {
            (AIController.Tier.F, -480f, -80f),
            (AIController.Tier.D,    0f, -80f),
            (AIController.Tier.C,  480f, -80f),
            (AIController.Tier.B, -480f, -380f),
            (AIController.Tier.A,    0f, -380f),
            (AIController.Tier.S,  480f, -380f),
        };
        var tileRefs = new MainMenuController.TierTile[tileLayout.Length];
        for (int i = 0; i < tileLayout.Length; i++)
        {
            var (tier, x, y) = tileLayout[i];
            tileRefs[i] = MakeTierTile(canvasGO.transform, tier, new Vector2(x, y), new Vector2(440f, 240f), font);
        }

        // Reset progress button (bottom-left)
        var resetBtn = MakeButton(canvasGO.transform, "ResetButton", "RESET PROGRESS",
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(20f, 20f), new Vector2(280f, 60f),
            new Color(0.40f, 0.10f, 0.10f), font);

        // Name entry overlay (initially active for first launch)
        var nameEntry = BuildNameEntryPanel(canvasGO.transform, font, out var nameInput, out var nameSubmit);

        // Hook up the controller
        var controllerGO = new GameObject("MainMenuController");
        var controller = controllerGO.AddComponent<MainMenuController>();
        controller.tiles = tileRefs;
        controller.greetingText = greeting;
        controller.resultBanner = banner;
        controller.nameEntryPanel = nameEntry;
        controller.nameInput = nameInput;
        controller.nameSubmitButton = nameSubmit;
        controller.resetProgressButton = resetBtn;

        // Camera + EventSystem (NewSceneSetup gave us a Main Camera; ensure its bg is dark)
        var cam = Camera.main;
        if (cam != null) cam.backgroundColor = BgDark;

        // EventSystem is required for UI buttons / input fields
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                                                    typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        }

        // Save the scene to Assets/Scenes/MainMenu.unity
        const string scenePath = "Assets/Scenes/MainMenu.unity";
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, scenePath);

        // Add both scenes to Build Settings so SceneManager.LoadScene works at runtime
        SetupBuildSettings();

        Debug.Log($"[BTierHoops] Main menu scene saved to {scenePath}. Build Settings updated. " +
                  $"If your game scene isn't named '{MatchSettings.GameSceneName}.unity' yet, rename SampleScene.unity → Game.unity in the Project window so SceneManager.LoadScene('{MatchSettings.GameSceneName}') resolves.");
    }

    [MenuItem("BTierHoops/Setup Build Settings (MainMenu + Game)")]
    public static void SetupBuildSettings()
    {
        var menuPath = "Assets/Scenes/MainMenu.unity";
        var gamePath = $"Assets/Scenes/{MatchSettings.GameSceneName}.unity";
        var fallbackGamePath = "Assets/Scenes/SampleScene.unity";
        if (!File.Exists(gamePath) && File.Exists(fallbackGamePath))
        {
            Debug.LogWarning($"[BTierHoops] Couldn't find {gamePath}; falling back to {fallbackGamePath} in Build Settings. Rename it to Game.unity for SceneManager.LoadScene('{MatchSettings.GameSceneName}') to work.");
            gamePath = fallbackGamePath;
        }
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        if (File.Exists(menuPath)) scenes.Add(new EditorBuildSettingsScene(menuPath, true));
        if (File.Exists(gamePath)) scenes.Add(new EditorBuildSettingsScene(gamePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"[BTierHoops] Build Settings scenes: {string.Join(", ", scenes.Select(s => Path.GetFileName(s.path)))}");
    }

    // ---------- UI helpers ----------
    static GameObject MakeCanvas(string name)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    static Image MakeFullScreenImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta,
        float fontSize, TextAlignmentOptions alignment, TMP_FontAsset font)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        if (font != null) tmp.font = font;
        return tmp;
    }

    static Button MakeButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta,
        Color bgColor, TMP_FontAsset font)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        var btn = go.AddComponent<Button>();

        var labelTmp = MakeText(go.transform, "Label", label,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero,
            32f, TextAlignmentOptions.Center, font);
        labelTmp.fontStyle = FontStyles.Bold;
        return btn;
    }

    static MainMenuController.TierTile MakeTierTile(Transform parent, AIController.Tier tier, Vector2 anchoredCenter, Vector2 size, TMP_FontAsset font)
    {
        var go = new GameObject($"Tile_{tier}");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredCenter;
        rect.sizeDelta = size;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.40f, 0.85f);
        var btn = go.AddComponent<Button>();

        // Big tier letter (top of tile)
        var letterLabel = MakeText(go.transform, "TierLetter", tier.ToString(),
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 30f), Vector2.zero,
            120f, TextAlignmentOptions.Center, font);
        letterLabel.fontStyle = FontStyles.Bold;

        // Character name (below letter)
        var nameLabel = MakeText(go.transform, "Name", AIController.GetTierData(tier).displayName,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -70f), Vector2.zero,
            36f, TextAlignmentOptions.Center, font);

        return new MainMenuController.TierTile {
            tier = tier,
            button = btn,
            background = bg,
            tierLetterLabel = letterLabel,
            nameLabel = nameLabel,
        };
    }

    static GameObject BuildNameEntryPanel(Transform parent, TMP_FontAsset font, out TMP_InputField inputField, out Button submitButton)
    {
        // Full-screen dim panel
        var panel = new GameObject("NameEntryPanel");
        panel.transform.SetParent(parent, false);
        var pRect = panel.AddComponent<RectTransform>();
        pRect.anchorMin = Vector2.zero;
        pRect.anchorMax = Vector2.one;
        pRect.offsetMin = Vector2.zero;
        pRect.offsetMax = Vector2.zero;
        var pImg = panel.AddComponent<Image>();
        pImg.color = new Color(0f, 0f, 0f, 0.85f);

        // Centered card
        MakeText(panel.transform, "Prompt", "ENTER YOUR NAME",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 100f), new Vector2(800f, 90f),
            64f, TextAlignmentOptions.Center, font).fontStyle = FontStyles.Bold;

        // Input field
        inputField = MakeInputField(panel.transform, "NameInput", "Your name…",
            new Vector2(0f, 0f), new Vector2(800f, 80f), font);

        // Submit button
        submitButton = MakeButton(panel.transform, "SubmitButton", "START",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -120f), new Vector2(420f, 80f),
            new Color(0.20f, 0.70f, 0.30f), font);

        return panel;
    }

    static TMP_InputField MakeInputField(Transform parent, string name, string placeholderText,
        Vector2 anchoredPos, Vector2 size, TMP_FontAsset font)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.20f, 0.20f, 0.25f);

        // Text Area (viewport for the input text)
        var textArea = new GameObject("TextArea");
        textArea.transform.SetParent(go.transform, false);
        var taRect = textArea.AddComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero;
        taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(20f, 10f);
        taRect.offsetMax = new Vector2(-20f, -10f);
        textArea.AddComponent<RectMask2D>();

        // Placeholder text
        var ph = MakeText(textArea.transform, "Placeholder", placeholderText,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f),
            Vector2.zero, Vector2.zero,
            42f, TextAlignmentOptions.Left, font);
        ph.color = new Color(0.6f, 0.6f, 0.65f);
        ph.fontStyle = FontStyles.Italic;

        // Actual input text
        var txt = MakeText(textArea.transform, "Text", "",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f),
            Vector2.zero, Vector2.zero,
            42f, TextAlignmentOptions.Left, font);
        txt.color = Color.white;

        var input = go.AddComponent<TMP_InputField>();
        input.textViewport = taRect;
        input.textComponent = txt;
        input.placeholder = ph;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = 16;

        return input;
    }
}
