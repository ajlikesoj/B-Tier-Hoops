using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class CourtBuilder
{
    // --- Court palette (character + letter palettes live in CharacterFactory) ---
    static readonly Color WoodLight = new Color(0.78f, 0.55f, 0.32f);
    static readonly Color WoodMid = new Color(0.62f, 0.42f, 0.22f);
    static readonly Color WoodDark = new Color(0.42f, 0.27f, 0.13f);
    static readonly Color CourtLine = new Color(0.97f, 0.96f, 0.90f);
    static readonly Color KeyPaint = new Color(0.78f, 0.18f, 0.18f);
    static readonly Color BackboardWhite = new Color(0.97f, 0.97f, 0.95f);
    static readonly Color BackboardSquare = new Color(0.85f, 0.15f, 0.15f);
    static readonly Color RimOrange = new Color(0.96f, 0.42f, 0.05f);
    static readonly Color NetWhite = new Color(0.92f, 0.92f, 0.92f);
    static readonly Color PoleGray = new Color(0.28f, 0.28f, 0.30f);
    static readonly Color BgDark = new Color(0.06f, 0.05f, 0.09f); // legacy — kept for camera fallback if anything still references it
    // --- Daytime sky palette ---
    static readonly Color SkyBlueTop = new Color(0.45f, 0.72f, 0.95f);
    static readonly Color SkyBlueBottom = new Color(0.78f, 0.90f, 1.00f);
    static readonly Color SunYellow = new Color(1.00f, 0.92f, 0.45f);
    static readonly Color CloudWhite = new Color(1.00f, 1.00f, 1.00f, 0.95f);
    static readonly Color GrassGreen = new Color(0.45f, 0.70f, 0.35f);
    static readonly Color BleacherGray = new Color(0.55f, 0.55f, 0.60f);
    static readonly Color BleacherDark = new Color(0.40f, 0.40f, 0.45f);

    // Crowd member jersey palette — random pick per seat
    static readonly Color[] CrowdShirtColors = new[] {
        new Color(0.85f, 0.20f, 0.25f),  // red
        new Color(0.20f, 0.50f, 0.90f),  // blue
        new Color(0.30f, 0.75f, 0.35f),  // green
        new Color(0.95f, 0.80f, 0.20f),  // yellow
        new Color(0.80f, 0.40f, 0.85f),  // purple
        new Color(0.95f, 0.55f, 0.20f),  // orange
        new Color(0.25f, 0.75f, 0.80f),  // teal
        new Color(0.95f, 0.55f, 0.65f),  // pink
        new Color(0.50f, 0.30f, 0.20f),  // brown
        new Color(0.20f, 0.20f, 0.25f),  // dark
    };
    static readonly Color[] CrowdSkinTones = new[] {
        new Color(0.95f, 0.80f, 0.65f),
        new Color(0.78f, 0.60f, 0.44f),
        new Color(0.66f, 0.50f, 0.36f),
        new Color(0.50f, 0.35f, 0.22f),
        new Color(0.36f, 0.24f, 0.16f),
    };

    // --- Jersey colors used at scene-build time ---
    public static readonly Color JerseyRed = new Color(0.86f, 0.16f, 0.16f);
    public static readonly Color JerseyBlue = new Color(0.18f, 0.40f, 0.85f);

    // --- Ball palette ---
    static readonly Color BallOrange = new Color(0.93f, 0.45f, 0.10f);
    static readonly Color BallSeam = new Color(0f, 0f, 0f, 0.75f);

    struct HoopRefs { public GameObject root; public Transform scoreTrigger; }

    // Thin wrapper so we don't have to type CharacterFactory.Quad everywhere in the court builder.
    static GameObject Quad(string n, Sprite s, Vector3 p, Vector3 sc, Color c, int order, Transform parent)
        => CharacterFactory.Quad(n, s, p, sc, c, order, parent);

    // ---------- Menu items ----------
    // Single "Build Game Scene" entry point — court + player + UI + an AISpawner placeholder.
    // The AISpawner reads MatchSettings.SelectedTier at runtime to instantiate the chosen opponent;
    // the per-tier menu items below just set the spawner's editor-default tier so devs can drop
    // straight into Play mode against a specific opponent without going through the main menu.
    [MenuItem("BTierHoops/Build Game Scene (default vs F - Afsar)")]
    public static void BuildGameSceneVsF() => BuildGameScene(AIController.Tier.F);

    [MenuItem("BTierHoops/Build Game Scene (default vs D - Praneel)")]
    public static void BuildGameSceneVsD() => BuildGameScene(AIController.Tier.D);

    [MenuItem("BTierHoops/Build Game Scene (default vs C - Krish)")]
    public static void BuildGameSceneVsC() => BuildGameScene(AIController.Tier.C);

    [MenuItem("BTierHoops/Build Game Scene (default vs B - Vignesh)")]
    public static void BuildGameSceneVsB() => BuildGameScene(AIController.Tier.B);

    [MenuItem("BTierHoops/Build Game Scene (default vs A - Ishaan)")]
    public static void BuildGameSceneVsA() => BuildGameScene(AIController.Tier.A);

    [MenuItem("BTierHoops/Build Game Scene (default vs S - Jay)")]
    public static void BuildGameSceneVsS() => BuildGameScene(AIController.Tier.S);

    static void BuildGameScene(AIController.Tier defaultTier)
    {
        // Auto-open the Game scene first so we can't accidentally overwrite MainMenu (or another scene)
        // with game content. Falls back to legacy SampleScene.unity, then to a fresh untitled scene.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string gamePath = $"Assets/Scenes/{MatchSettings.GameSceneName}.unity";
        string fallbackPath = "Assets/Scenes/SampleScene.unity";
        string savePath;
        if (File.Exists(gamePath))
        {
            EditorSceneManager.OpenScene(gamePath, OpenSceneMode.Single);
            savePath = gamePath;
        }
        else if (File.Exists(fallbackPath))
        {
            EditorSceneManager.OpenScene(fallbackPath, OpenSceneMode.Single);
            savePath = fallbackPath;
        }
        else
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Directory.CreateDirectory("Assets/Scenes");
            savePath = "Assets/Scenes/Game.unity";
        }

        ClearSceneInternal();

        var ws = CharacterFactory.WhiteSprite;
        var circle = CharacterFactory.CircleSprite;

        BuildBackground(ws);
        BuildFloor(ws);

        var rightHoop = BuildHoop(ws, hoopX: 9f, side: +1);
        var leftHoop = BuildHoop(ws, hoopX: -9f, side: -1);

        var ball = BuildBall(ws, circle, new Vector3(0f, 1f, 0f));
        BuildPlayerContainment();

        var playerSpawn = new Vector3(-3f, 0f, 0f);
        var player = CharacterFactory.BuildCharacter(playerSpawn, JerseyRed, "Player", attachPlayerController: true, initialFacing: +1);

        // Wire player shooting controller
        var shooter = player.AddComponent<ShootingController>();
        shooter.ball = ball;
        shooter.targetHoop = rightHoop.scoreTrigger;

        // Charge meter visual above player
        BuildChargeMeter(ws, player.transform, shooter);

        // Player ↔ ball wiring (opponent ref is set later by AISpawner once the AI is constructed)
        var playerCtrl = player.GetComponent<PlayerController>();
        if (playerCtrl != null) playerCtrl.ball = ball;

        // Game manager + UI
        var gm = BuildGameManager(ball);
        gm.player = player.transform;
        gm.playerSpawnPosition = playerSpawn;
        gm.aiSpawnPosition = new Vector3(3f, 0f, 0f);
        BuildUI(gm);

        SetupCamera();

        BuildSoundHandler();

        // EventSystem — required for the win panel's Return-to-Menu button to receive clicks
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                                          typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        }

        // AISpawner — at runtime, reads MatchSettings.SelectedTier (or defaultTier if menu not used).
        // Spawns the AI character and finishes wiring (gm.ai, playerCtrl.opponent, gm.aiName).
        var spawnerGO = new GameObject("AISpawner");
        var spawner = spawnerGO.AddComponent<AISpawner>();
        spawner.ball = ball;
        spawner.targetHoop = leftHoop.scoreTrigger;
        spawner.ownHoop = rightHoop.scoreTrigger;
        spawner.opponent = player.transform;
        spawner.spawnPosition = new Vector3(3f, 0f, 0f);
        spawner.jerseyColor = JerseyBlue;
        spawner.defaultTierForEditor = defaultTier;
        spawner.gameManager = gm;
        spawner.playerController = playerCtrl;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), savePath);
        Debug.Log($"[BTierHoops] Game scene built and saved to {savePath} — AI default = tier {defaultTier} (overridden by MatchSettings.SelectedTier at runtime).");
    }

    [MenuItem("BTierHoops/Clear Scene")]
    public static void ClearScene() => ClearSceneInternal();

    static void ClearSceneInternal()
    {
        var scene = EditorSceneManager.GetActiveScene();
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.GetComponent<Camera>() != null) continue;
            if (go.GetComponent<Light>() != null) continue;
            Object.DestroyImmediate(go);
        }
        EditorSceneManager.MarkSceneDirty(scene);
    }

    // ---------- Background (daytime sky + bleachers + crowd) ----------
    static void BuildBackground(Sprite ws)
    {
        var circle = CharacterFactory.CircleSprite;
        var root = new GameObject("Background");

        // ---- Sky: two stacked layers (deeper blue up top, lighter near horizon) ----
        Quad("SkyTop",    ws, new Vector3(0,  6f, 5f), new Vector3(40f, 12f, 1f), SkyBlueTop,    -25, root.transform);
        Quad("SkyBottom", ws, new Vector3(0, -2f, 5f), new Vector3(40f, 8f,  1f), SkyBlueBottom, -25, root.transform);

        // ---- Sun ----
        var sun = new GameObject("Sun");
        sun.transform.SetParent(root.transform);
        sun.transform.localPosition = new Vector3(8.5f, 4.5f, 4.8f);
        sun.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
        var sunSr = sun.AddComponent<SpriteRenderer>();
        sunSr.sprite = circle;
        sunSr.color = SunYellow;
        sunSr.sortingOrder = -24;
        // Subtle glow ring
        var glow = new GameObject("SunGlow");
        glow.transform.SetParent(sun.transform);
        glow.transform.localPosition = Vector3.zero;
        glow.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
        var glowSr = glow.AddComponent<SpriteRenderer>();
        glowSr.sprite = circle;
        glowSr.color = new Color(SunYellow.r, SunYellow.g, SunYellow.b, 0.25f);
        glowSr.sortingOrder = -25;

        // ---- Clouds (a few puffy white blobs) ----
        BuildCloud(circle, root.transform, new Vector3(-7f, 4.2f, 4.7f), 1.4f);
        BuildCloud(circle, root.transform, new Vector3( 0f, 5.0f, 4.7f), 1.0f);
        BuildCloud(circle, root.transform, new Vector3( 4f, 3.6f, 4.7f), 1.2f);
        BuildCloud(circle, root.transform, new Vector3(-10f, 2.8f, 4.7f), 0.9f);

        // ---- Distant grass strip behind the court (peeks above the floor) ----
        Quad("GrassStrip", ws, new Vector3(0, -1.9f, 4.4f), new Vector3(40f, 0.6f, 1f), GrassGreen, -22, root.transform);

        // ---- Bleachers: 4 stepped rows behind the court ----
        // Each row is a long horizontal seat plank with a darker step under it.
        var bleachers = new GameObject("Bleachers");
        bleachers.transform.SetParent(root.transform);
        const int bleacherRows = 4;
        const float bleacherStartY = -0.6f;
        const float bleacherSpacingY = 0.85f;
        for (int r = 0; r < bleacherRows; r++)
        {
            float y = bleacherStartY + r * bleacherSpacingY;
            // Riser (darker, slightly behind)
            Quad($"Riser_{r}", ws, new Vector3(0f, y - 0.1f, 4.3f), new Vector3(28f, 0.55f, 1f), BleacherDark, -21, bleachers.transform);
            // Seat plank (lighter, in front)
            Quad($"Seat_{r}", ws, new Vector3(0f, y + 0.2f, 4.2f), new Vector3(28f, 0.18f, 1f), BleacherGray, -20, bleachers.transform);
        }

        // ---- Crowd: pre-build the maximum (~100 seats) across the bleacher rows.
        // CrowdManager hides everything beyond the per-tier attendance count at runtime. ----
        var crowdRoot = new GameObject("Crowd");
        crowdRoot.transform.SetParent(root.transform);
        var crowdManager = crowdRoot.AddComponent<CrowdManager>();
        crowdManager.crowdMembers = new System.Collections.Generic.List<GameObject>();

        // Seat fill order: front-row center outward, then row by row going back. This way
        // sparse crowds (D, C) cluster front-and-center instead of looking randomly scattered.
        const int seatsPerRow = 25;
        var rng = new System.Random(1337); // deterministic so dev iteration is stable
        for (int r = 0; r < bleacherRows; r++)
        {
            float baseY = bleacherStartY + r * bleacherSpacingY + 0.45f;     // sit on top of seat plank
            float z = 4.0f - r * 0.05f;                                       // back rows slightly behind
            // Center-outward x ordering: 0, +1, -1, +2, -2, …
            for (int s = 0; s < seatsPerRow; s++)
            {
                int signedIdx = (s % 2 == 0) ? (s / 2) : -((s + 1) / 2);     // 0, -1, +1, -2, +2, …
                float x = signedIdx * 0.95f + (float)(rng.NextDouble() - 0.5) * 0.12f;
                float yJitter = (float)(rng.NextDouble() - 0.5) * 0.05f;
                var member = BuildCrowdMember(circle, ws, crowdRoot.transform,
                    new Vector3(x, baseY + yJitter, z),
                    rng);
                crowdManager.crowdMembers.Add(member);
            }
        }
    }

    static void BuildCloud(Sprite circle, Transform parent, Vector3 center, float scale)
    {
        var cloud = new GameObject("Cloud");
        cloud.transform.SetParent(parent);
        cloud.transform.localPosition = center;
        cloud.transform.localScale = new Vector3(scale, scale, 1f);
        // Three overlapping puffs make a cartoony cloud
        AddCloudPuff(circle, cloud.transform, new Vector3(-0.8f, 0f, 0f),  1.0f);
        AddCloudPuff(circle, cloud.transform, new Vector3( 0.0f, 0.2f, 0f), 1.3f);
        AddCloudPuff(circle, cloud.transform, new Vector3( 0.8f, 0f, 0f),  1.0f);
        AddCloudPuff(circle, cloud.transform, new Vector3(-0.3f,-0.2f, 0f), 0.9f);
        AddCloudPuff(circle, cloud.transform, new Vector3( 0.3f,-0.2f, 0f), 0.9f);
    }

    static void AddCloudPuff(Sprite circle, Transform parent, Vector3 localPos, float scale)
    {
        var go = new GameObject("Puff");
        go.transform.SetParent(parent);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(scale, scale * 0.85f, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = circle;
        sr.color = CloudWhite;
        sr.sortingOrder = -23;
    }

    static GameObject BuildCrowdMember(Sprite circle, Sprite ws, Transform parent, Vector3 worldPos, System.Random rng)
    {
        var member = new GameObject("CrowdMember");
        member.transform.SetParent(parent);
        member.transform.localPosition = worldPos;
        member.transform.localScale = Vector3.one;

        Color shirt = CrowdShirtColors[rng.Next(CrowdShirtColors.Length)];
        Color skin = CrowdSkinTones[rng.Next(CrowdSkinTones.Length)];
        Color hair = new Color(0.10f, 0.07f, 0.05f);

        // Body (rectangle)
        Quad("Body", ws, new Vector3(0f, 0f, 0f), new Vector3(0.30f, 0.32f, 1f), shirt, -18, member.transform);
        // Head (small circle on top of body)
        var head = new GameObject("Head");
        head.transform.SetParent(member.transform);
        head.transform.localPosition = new Vector3(0f, 0.24f, 0f);
        head.transform.localScale = new Vector3(0.22f, 0.22f, 1f);
        var headSr = head.AddComponent<SpriteRenderer>();
        headSr.sprite = circle;
        headSr.color = skin;
        headSr.sortingOrder = -17;
        // Hair cap (slightly behind head, darker)
        var hairCap = new GameObject("Hair");
        hairCap.transform.SetParent(member.transform);
        hairCap.transform.localPosition = new Vector3(0f, 0.30f, 0f);
        hairCap.transform.localScale = new Vector3(0.20f, 0.14f, 1f);
        var hairSr = hairCap.AddComponent<SpriteRenderer>();
        hairSr.sprite = circle;
        hairSr.color = hair;
        hairSr.sortingOrder = -17;

        return member;
    }

    // ---------- Floor + court markings ----------
    static void BuildFloor(Sprite ws)
    {
        var root = new GameObject("Floor");

        var floor = Quad("FloorMain", ws, new Vector3(0, -3, 0), new Vector3(24, 1.6f, 1), WoodLight, 0, root.transform);
        floor.AddComponent<BoxCollider2D>();

        for (float x = -11.5f; x <= 11.5f; x += 1.4f)
        {
            Quad("Plank", ws, new Vector3(x, -2.6f, 0), new Vector3(0.04f, 1.6f, 1), WoodMid, 1, root.transform);
        }
        Quad("FloorTopEdge", ws, new Vector3(0, -2.21f, 0), new Vector3(24, 0.05f, 1), WoodDark, 2, root.transform);

        BuildKey(ws, 7f, +1, root.transform);
        BuildKey(ws, -7f, -1, root.transform);

        Quad("HalfCourtLine", ws, new Vector3(0f, -2.40f, 0), new Vector3(0.06f, 0.32f, 1), CourtLine, 3, root.transform);
        Quad("CenterCircle", ws, new Vector3(0f, -2.30f, 0), new Vector3(1.4f, 0.05f, 1), CourtLine, 3, root.transform);
    }

    static void BuildKey(Sprite ws, float keyCenterX, int side, Transform parent)
    {
        Quad("PaintedKey", ws, new Vector3(keyCenterX, -2.4f, 0), new Vector3(4.5f, 0.30f, 1), KeyPaint, 2, parent);
        Quad("KeyLineNear", ws, new Vector3(keyCenterX - 2.25f * side, -2.40f, 0), new Vector3(0.06f, 0.30f, 1), CourtLine, 3, parent);
        Quad("KeyLineFar", ws, new Vector3(keyCenterX + 2.25f * side, -2.40f, 0), new Vector3(0.06f, 0.30f, 1), CourtLine, 3, parent);
        Quad("KeyLineTop", ws, new Vector3(keyCenterX, -2.26f, 0), new Vector3(4.5f, 0.05f, 1), CourtLine, 3, parent);
        Quad("FTLineMark", ws, new Vector3(keyCenterX - 2.25f * side, -2.20f, 0), new Vector3(0.10f, 0.10f, 1), CourtLine, 4, parent);
        float tpX = keyCenterX - 4.0f * side;
        if (Mathf.Abs(tpX) <= 11f)
            Quad("ThreePtTick", ws, new Vector3(tpX, -2.40f, 0), new Vector3(0.05f, 0.30f, 1), CourtLine, 3, parent);
    }

    static void AddInvisibleCollider(Transform parent, string name, Vector3 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;
    }

    static void AddOneWayUnderRimBarrier(Transform parent, Vector3 pos, Vector2 size)
    {
        var go = new GameObject("UnderRimBarrier");
        go.transform.SetParent(parent);
        go.transform.position = pos;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        col.usedByEffector = true;
        var effector = go.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.surfaceArc = 180f;
        effector.rotationalOffset = 180f; // rotate the solid arc to face DOWN — solid from below, pass-through from above
    }

    // ---------- Player containment (blocks player, ball passes through) ----------
    static void BuildPlayerContainment()
    {
        var root = new GameObject("PlayerContainment");
        AddPlayerOnlyWall(root.transform, "LeftWall", new Vector3(-12.5f, 0f, 0), new Vector2(1f, 22f));
        AddPlayerOnlyWall(root.transform, "RightWall", new Vector3(12.5f, 0f, 0), new Vector2(1f, 22f));
    }

    static void AddPlayerOnlyWall(Transform parent, string name, Vector3 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        go.AddComponent<WallIgnoreBall>();
    }

    // ---------- Hoop (with score trigger) ----------
    static HoopRefs BuildHoop(Sprite ws, float hoopX, int side)
    {
        string suffix = side > 0 ? "Right" : "Left";
        var root = new GameObject($"Hoop_{suffix}");
        root.AddComponent<HoopNetAnimator>();

        Quad("Pole", ws, new Vector3(hoopX + 1.2f * side, 0.5f, 0), new Vector3(0.22f, 6.5f, 1), PoleGray, 1, root.transform);
        Quad("PoleBase", ws, new Vector3(hoopX + 1.2f * side, -2.3f, 0), new Vector3(0.7f, 0.25f, 1), PoleGray, 2, root.transform);
        Quad("PoleArm", ws, new Vector3(hoopX + 0.55f * side, 3.2f, 0), new Vector3(1.5f, 0.16f, 1), PoleGray, 2, root.transform);

        var backboard = Quad("Backboard", ws, new Vector3(hoopX - 0.3f * side, 2.6f, 0), new Vector3(0.18f, 1.6f, 1), BackboardWhite, 3, root.transform);
        backboard.AddComponent<BoxCollider2D>();
        Quad("BBSquareNear", ws, new Vector3(hoopX - 0.42f * side, 2.45f, 0), new Vector3(0.04f, 0.55f, 1), BackboardSquare, 4, root.transform);
        Quad("BBSquareFar", ws, new Vector3(hoopX - 0.22f * side, 2.45f, 0), new Vector3(0.04f, 0.55f, 1), BackboardSquare, 4, root.transform);
        Quad("BBSquareTop", ws, new Vector3(hoopX - 0.32f * side, 2.70f, 0), new Vector3(0.24f, 0.04f, 1), BackboardSquare, 4, root.transform);
        Quad("BBSquareBot", ws, new Vector3(hoopX - 0.32f * side, 2.20f, 0), new Vector3(0.24f, 0.04f, 1), BackboardSquare, 4, root.transform);

        Quad("Rim", ws, new Vector3(hoopX - 0.95f * side, 2.10f, 0), new Vector3(1.1f, 0.07f, 1), RimOrange, 5, root.transform);

        float rimCenterX = hoopX - 0.95f * side;
        float frontRimX = rimCenterX - 0.55f * side;
        float backRimX = rimCenterX + 0.55f * side;
        AddInvisibleCollider(root.transform, "FrontRim", new Vector3(frontRimX, 2.10f, 0), new Vector2(0.10f, 0.10f));
        AddInvisibleCollider(root.transform, "BackRim", new Vector3(backRimX, 2.10f, 0), new Vector2(0.10f, 0.10f));
        AddOneWayUnderRimBarrier(root.transform, new Vector3(rimCenterX, 2.075f, 0f), new Vector2(1.0f, 0.05f));

        float netCenterX = hoopX - 0.95f * side;
        for (int i = -2; i <= 2; i++)
        {
            float nx = netCenterX + i * 0.22f;
            float h = 0.55f - 0.04f * Mathf.Abs(i);
            Quad($"Net_{i}", ws, new Vector3(nx, 2.10f - h * 0.5f - 0.02f, 0), new Vector3(0.035f, h, 1), NetWhite, 4, root.transform);
        }
        Quad("NetCross1", ws, new Vector3(netCenterX, 1.95f, 0), new Vector3(0.95f, 0.02f, 1), NetWhite, 4, root.transform);
        Quad("NetCross2", ws, new Vector3(netCenterX, 1.78f, 0), new Vector3(0.85f, 0.02f, 1), NetWhite, 4, root.transform);

        var trigger = new GameObject("ScoreTrigger");
        trigger.transform.SetParent(root.transform);
        trigger.transform.position = new Vector3(rimCenterX, 1.95f, 0);
        var tCol = trigger.AddComponent<BoxCollider2D>();
        tCol.size = new Vector2(0.7f, 0.10f);
        tCol.isTrigger = true;
        var hoopScript = trigger.AddComponent<Hoop>();
        hoopScript.side = side;

        return new HoopRefs { root = root, scoreTrigger = trigger.transform };
    }

    // ---------- Ball ----------
    static BasketballBall BuildBall(Sprite ws, Sprite circle, Vector3 position)
    {
        var ball = new GameObject("Ball");
        ball.transform.position = position;
        ball.transform.localScale = new Vector3(0.45f, 0.45f, 1f);

        var sr = ball.AddComponent<SpriteRenderer>();
        sr.sprite = circle;
        sr.color = BallOrange;
        sr.sortingOrder = 16;

        var rb = ball.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1.5f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.5f;
        rb.mass = 0.6f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = ball.AddComponent<CircleCollider2D>();
        var mat = new PhysicsMaterial2D("BallBouncy") { bounciness = 0.72f, friction = 0.5f };
        col.sharedMaterial = mat;

        Quad("VertSeam", ws, new Vector3(0, 0, -0.01f), new Vector3(0.06f, 1f, 1f), BallSeam, 17, ball.transform);
        Quad("HorzSeam", ws, new Vector3(0, 0, -0.01f), new Vector3(1f, 0.06f, 1f), BallSeam, 17, ball.transform);
        Quad("CurveSeamL", ws, new Vector3(-0.32f, 0.04f, -0.01f), new Vector3(0.05f, 0.7f, 1f), BallSeam, 17, ball.transform);
        Quad("CurveSeamR", ws, new Vector3(0.32f, 0.04f, -0.01f), new Vector3(0.05f, 0.7f, 1f), BallSeam, 17, ball.transform);

        return ball.AddComponent<BasketballBall>();
    }

    // ---------- Charge meter (world-space, above player) ----------
    static void BuildChargeMeter(Sprite ws, Transform player, ShootingController shooter)
    {
        var meter = new GameObject("ChargeMeter");
        meter.transform.SetParent(player);
        meter.transform.localPosition = new Vector3(0f, 1.25f, 0f);

        Quad("Bg", ws, Vector3.zero, new Vector3(1.4f, 0.18f, 1f), new Color(0.10f, 0.10f, 0.13f, 0.85f), 30, meter.transform);

        float sweetCenter = (shooter.perfectChargeMin + shooter.perfectChargeMax) * 0.5f;
        float sweetWidth = (shooter.perfectChargeMax - shooter.perfectChargeMin) * 1.4f;
        float sweetX = sweetCenter * 1.4f - 0.7f;
        Quad("SweetZone", ws, new Vector3(sweetX, 0, -0.001f), new Vector3(sweetWidth, 0.16f, 1f), new Color(0.30f, 0.85f, 0.30f, 0.45f), 31, meter.transform);

        var fill = Quad("Fill", ws, new Vector3(-0.7f, 0, -0.002f), new Vector3(0.01f, 0.16f, 1f), new Color(0.95f, 0.75f, 0.20f), 32, meter.transform);

        Quad("BorderTop", ws, new Vector3(0, 0.095f, 0), new Vector3(1.42f, 0.025f, 1), Color.white, 33, meter.transform);
        Quad("BorderBot", ws, new Vector3(0, -0.095f, 0), new Vector3(1.42f, 0.025f, 1), Color.white, 33, meter.transform);
        Quad("BorderL", ws, new Vector3(-0.71f, 0, 0), new Vector3(0.025f, 0.20f, 1), Color.white, 33, meter.transform);
        Quad("BorderR", ws, new Vector3(0.71f, 0, 0), new Vector3(0.025f, 0.20f, 1), Color.white, 33, meter.transform);

        var cmw = player.gameObject.AddComponent<ChargeMeterWorld>();
        cmw.shooter = shooter;
        cmw.meterRoot = meter.transform;
        cmw.fillTransform = fill.transform;
        cmw.fillRenderer = fill.GetComponent<SpriteRenderer>();
        cmw.baseWidth = 1.4f;
        cmw.fillHeight = 0.16f;

        meter.SetActive(false);
    }

    // ---------- Game manager ----------
    static GameManager BuildGameManager(BasketballBall ball)
    {
        var go = new GameObject("GameManager");
        var gm = go.AddComponent<GameManager>();
        go.AddComponent<OpponentLearningService>();
        gm.ball = ball;
        gm.ballSpawnPosition = new Vector3(0f, 1f, 0f);
        return gm;
    }

    // ---------- Sound handler (singleton w/ DontDestroyOnLoad) ----------
    // SoundHandler.Instance must exist for any of Diego's audio hooks (dribble, rim, charge,
    // squeak, etc.) to make sound. SoundHandler.Awake auto-rejects duplicates, so creating it
    // in both Game and MainMenu is safe — whichever loads first wins, the other is destroyed.
    static void BuildSoundHandler()
    {
        var go = new GameObject("SoundHandler");
        go.AddComponent<SoundHandler>();
        // crowdClip auto-resolves via SoundHandler.OnValidate (searches Assets/Audio for "crowd").
    }

    // ---------- UI (timer, score, win panel + return-to-menu button) ----------
    static void BuildUI(GameManager gm)
    {
        var canvasGO = new GameObject("UI");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        gm.timerText = MakeTMPText(canvasGO.transform, "TimerText", "1:00",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -30f), new Vector2(0f, 90f),
            72f, TextAlignmentOptions.Center, font);
        gm.timerText.fontStyle = FontStyles.Bold;

        gm.scoreText = MakeTMPText(canvasGO.transform, "ScoreText", "0   -   0",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -120f), new Vector2(0f, 80f),
            56f, TextAlignmentOptions.Center, font);

        // Win panel — full-screen overlay, hidden until match ends
        var winPanelGO = new GameObject("WinPanel");
        winPanelGO.transform.SetParent(canvasGO.transform, false);
        var panelRect = winPanelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        var panelImg = winPanelGO.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.78f);

        gm.winText = MakeTMPText(winPanelGO.transform, "WinText", "PLAYER WINS!",
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 60f), new Vector2(0f, 220f),
            120f, TextAlignmentOptions.Center, font);
        gm.winText.fontStyle = FontStyles.Bold;

        var hint = MakeTMPText(winPanelGO.transform, "HintText", "Press R to play again",
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -100f), new Vector2(0f, 60f),
            42f, TextAlignmentOptions.Center, font);
        hint.color = new Color(0.85f, 0.85f, 0.85f);

        // Return-to-menu button (visible inside win panel)
        var btnGO = new GameObject("MenuButton");
        btnGO.transform.SetParent(winPanelGO.transform, false);
        var btnRect = btnGO.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = new Vector2(0f, -200f);
        btnRect.sizeDelta = new Vector2(420f, 80f);
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.20f, 0.40f, 0.85f);
        btnGO.AddComponent<Button>();
        // Runtime click handler — lambdas added at edit time aren't serialized, so we attach a
        // small MonoBehaviour that wires the listener in its Start.
        btnGO.AddComponent<ReturnToMenuButton>();
        var btnLabel = MakeTMPText(btnGO.transform, "Label", "RETURN TO MENU",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero,
            36f, TextAlignmentOptions.Center, font);
        btnLabel.fontStyle = FontStyles.Bold;

        winPanelGO.SetActive(false);
        gm.winPanel = winPanelGO;
    }

    static TextMeshProUGUI MakeTMPText(Transform parent, string name, string text,
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

    // ---------- Camera ----------
    static void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.orthographic = true;
        cam.orthographicSize = 6.2f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.backgroundColor = SkyBlueTop;
    }
}
