using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class CourtBuilder
{
    // Block-letter shape table. Each letter is a list of (centerX, centerY, width, height) rectangles
    // in a unit square [-0.5, 0.5] x [-0.5, 0.5]. Built from SpriteRenderer Quads — no TMP component
    // means no editor gizmo can ever appear on the character.
    static readonly Dictionary<char, (float x, float y, float w, float h)[]> LetterShapes =
        new Dictionary<char, (float x, float y, float w, float h)[]>
    {
        ['A'] = new (float x, float y, float w, float h)[] {
            (-0.40f, -0.05f, 0.20f, 0.90f),
            ( 0.40f, -0.05f, 0.20f, 0.90f),
            ( 0.00f,  0.40f, 1.00f, 0.20f),
            ( 0.00f,  0.00f, 1.00f, 0.20f),
        },
        ['B'] = new (float x, float y, float w, float h)[] {
            (-0.40f,  0.00f, 0.20f, 1.00f),
            (-0.10f,  0.40f, 0.80f, 0.20f),
            (-0.10f,  0.00f, 0.80f, 0.20f),
            (-0.10f, -0.40f, 0.80f, 0.20f),
            ( 0.40f,  0.20f, 0.20f, 0.40f),
            ( 0.40f, -0.20f, 0.20f, 0.40f),
        },
        ['C'] = new (float x, float y, float w, float h)[] {
            (-0.40f,  0.00f, 0.20f, 0.80f),
            ( 0.00f,  0.40f, 1.00f, 0.20f),
            ( 0.00f, -0.40f, 1.00f, 0.20f),
        },
        ['D'] = new (float x, float y, float w, float h)[] {
            (-0.40f,  0.00f, 0.20f, 1.00f),
            (-0.10f,  0.40f, 0.80f, 0.20f),
            (-0.10f, -0.40f, 0.80f, 0.20f),
            ( 0.40f,  0.00f, 0.20f, 0.80f),
        },
        ['E'] = new (float x, float y, float w, float h)[] {
            (-0.40f,  0.00f, 0.20f, 1.00f),
            ( 0.00f,  0.40f, 1.00f, 0.20f),
            (-0.05f,  0.00f, 0.90f, 0.20f),
            ( 0.00f, -0.40f, 1.00f, 0.20f),
        },
        ['F'] = new (float x, float y, float w, float h)[] {
            (-0.40f,  0.00f, 0.20f, 1.00f),
            ( 0.00f,  0.40f, 1.00f, 0.20f),
            (-0.05f,  0.00f, 0.90f, 0.20f),
        },
        ['G'] = new (float x, float y, float w, float h)[] {
            (-0.40f,  0.00f, 0.20f, 0.80f),
            ( 0.00f,  0.40f, 1.00f, 0.20f),
            ( 0.00f, -0.40f, 1.00f, 0.20f),
            ( 0.40f, -0.20f, 0.20f, 0.40f),
            ( 0.20f, -0.05f, 0.40f, 0.18f),
        },
        ['H'] = new (float x, float y, float w, float h)[] {
            (-0.40f,  0.00f, 0.20f, 1.00f),
            ( 0.40f,  0.00f, 0.20f, 1.00f),
            ( 0.00f,  0.00f, 1.00f, 0.20f),
        },
        ['I'] = new (float x, float y, float w, float h)[] {
            ( 0.00f,  0.40f, 1.00f, 0.20f),
            ( 0.00f,  0.00f, 0.20f, 0.80f),
            ( 0.00f, -0.40f, 1.00f, 0.20f),
        },
        ['J'] = new (float x, float y, float w, float h)[] {
            ( 0.10f,  0.40f, 0.80f, 0.20f),    // top horizontal
            ( 0.30f,  0.05f, 0.20f, 0.90f),    // right vertical (full height)
            ( 0.00f, -0.40f, 0.60f, 0.20f),    // bottom horizontal (hook)
            (-0.30f, -0.20f, 0.20f, 0.30f),    // left vertical (hook stem)
        },
        ['K'] = new (float x, float y, float w, float h)[] {
            (-0.40f,  0.00f, 0.20f, 1.00f),
            (-0.10f,  0.05f, 0.20f, 0.20f),
            ( 0.10f,  0.25f, 0.20f, 0.20f),
            ( 0.30f,  0.40f, 0.20f, 0.20f),
            (-0.10f, -0.05f, 0.20f, 0.20f),
            ( 0.10f, -0.25f, 0.20f, 0.20f),
            ( 0.30f, -0.40f, 0.20f, 0.20f),
        },
        ['L'] = new (float x, float y, float w, float h)[] {
            (-0.40f,  0.00f, 0.20f, 1.00f),
            ( 0.00f, -0.40f, 1.00f, 0.20f),
        },
        ['N'] = new (float x, float y, float w, float h)[] {
            (-0.40f,  0.00f, 0.20f, 1.00f),
            ( 0.40f,  0.00f, 0.20f, 1.00f),
            (-0.15f,  0.20f, 0.20f, 0.20f),
            ( 0.00f,  0.00f, 0.20f, 0.20f),
            ( 0.15f, -0.20f, 0.20f, 0.20f),
        },
        ['P'] = new (float x, float y, float w, float h)[] {
            (-0.40f,  0.00f, 0.20f, 1.00f),
            (-0.10f,  0.40f, 0.80f, 0.20f),
            (-0.10f,  0.05f, 0.80f, 0.20f),
            ( 0.40f,  0.25f, 0.20f, 0.50f),
        },
        ['R'] = new (float x, float y, float w, float h)[] {
            (-0.40f,  0.00f, 0.20f, 1.00f),
            (-0.10f,  0.40f, 0.80f, 0.20f),
            (-0.10f,  0.05f, 0.80f, 0.20f),
            ( 0.40f,  0.25f, 0.20f, 0.50f),
            ( 0.10f, -0.10f, 0.20f, 0.20f),
            ( 0.30f, -0.30f, 0.20f, 0.20f),
        },
        ['S'] = new (float x, float y, float w, float h)[] {
            ( 0.00f,  0.40f, 1.00f, 0.20f),
            (-0.40f,  0.20f, 0.20f, 0.40f),
            ( 0.00f,  0.00f, 1.00f, 0.20f),
            ( 0.40f, -0.20f, 0.20f, 0.40f),
            ( 0.00f, -0.40f, 1.00f, 0.20f),
        },
        ['V'] = new (float x, float y, float w, float h)[] {
            (-0.40f,  0.20f, 0.20f, 0.60f),
            ( 0.40f,  0.20f, 0.20f, 0.60f),
            (-0.20f, -0.10f, 0.20f, 0.30f),
            ( 0.20f, -0.10f, 0.20f, 0.30f),
            ( 0.00f, -0.35f, 0.20f, 0.30f),
        },
        ['Y'] = new (float x, float y, float w, float h)[] {
            (-0.40f,  0.30f, 0.20f, 0.40f),
            ( 0.40f,  0.30f, 0.20f, 0.40f),
            (-0.20f,  0.05f, 0.20f, 0.20f),
            ( 0.20f,  0.05f, 0.20f, 0.20f),
            ( 0.00f, -0.20f, 0.20f, 0.60f),
        },
    };

    // --- Court palette ---
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
    static readonly Color BgDark = new Color(0.06f, 0.05f, 0.09f);
    static readonly Color WallMid = new Color(0.14f, 0.10f, 0.16f);
    static readonly Color CrowdDark = new Color(0.10f, 0.08f, 0.13f);
    static readonly Color CrowdMid = new Color(0.20f, 0.15f, 0.22f);

    // --- Player palette ---
    static readonly Color SkinTone = new Color(0.93f, 0.74f, 0.56f);
    static readonly Color HairBlack = new Color(0.10f, 0.07f, 0.05f);
    static readonly Color ShortsDark = new Color(0.12f, 0.12f, 0.14f);
    static readonly Color ShoeWhite = new Color(0.96f, 0.96f, 0.96f);
    static readonly Color JerseyRed = new Color(0.86f, 0.16f, 0.16f);
    static readonly Color JerseyBlue = new Color(0.18f, 0.40f, 0.85f);
    static readonly Color JerseyTrim = new Color(0.98f, 0.98f, 0.95f);

    // --- Ball palette ---
    static readonly Color BallOrange = new Color(0.93f, 0.45f, 0.10f);
    static readonly Color BallSeam = new Color(0f, 0f, 0f, 0.75f);

    struct HoopRefs { public GameObject root; public Transform scoreTrigger; }

    // Player has no name yet — the planned account/signup flow will let the user enter one.
    // Until then the score and win text just show the score for the player side.
    const string PlayerName = "";

    [MenuItem("BTierHoops/Build Court (vs F - Afsar)")]
    public static void BuildCourtVsF() => BuildCourtForTier(AIController.Tier.F);

    [MenuItem("BTierHoops/Build Court (vs D - Praneel)")]
    public static void BuildCourtVsD() => BuildCourtForTier(AIController.Tier.D);

    [MenuItem("BTierHoops/Build Court (vs C - Krish)")]
    public static void BuildCourtVsC() => BuildCourtForTier(AIController.Tier.C);

    [MenuItem("BTierHoops/Build Court (vs B - Vignesh)")]
    public static void BuildCourtVsB() => BuildCourtForTier(AIController.Tier.B);

    [MenuItem("BTierHoops/Build Court (vs A - Ishaan)")]
    public static void BuildCourtVsA() => BuildCourtForTier(AIController.Tier.A);

    [MenuItem("BTierHoops/Build Court (vs S - Jay)")]
    public static void BuildCourtVsS() => BuildCourtForTier(AIController.Tier.S);

    static void BuildCourtForTier(AIController.Tier aiTier)
    {
        ClearSceneInternal();

        var ws = MakeWhiteSprite();
        var circle = MakeCircleSprite(96);

        BuildBackground(ws);
        BuildFloor(ws);

        var rightHoop = BuildHoop(ws, hoopX: 9f, side: +1);
        var leftHoop = BuildHoop(ws, hoopX: -9f, side: -1);

        var ball = BuildBall(ws, circle, new Vector3(0f, 1f, 0f));
        BuildPlayerContainment();
        var playerSpawn = new Vector3(-3f, 0f, 0f);
        var player = BuildPlayer(ws, circle, playerSpawn, JerseyRed, "Player", attachController: true, initialFacing: +1);

        // Wire player shooting controller
        var shooter = player.AddComponent<ShootingController>();
        shooter.ball = ball;
        shooter.targetHoop = rightHoop.scoreTrigger;

        // Charge meter visual above player
        BuildChargeMeter(ws, player.transform, shooter);

        // AI opponent — blue jersey, right-side spawn, attacks left hoop
        var aiSpawn = new Vector3(3f, 0f, 0f);
        var aiTierData = AIController.GetTierData(aiTier);
        var ai = BuildPlayer(ws, circle, aiSpawn, JerseyBlue, "AI", attachController: false, initialFacing: -1, tierData: aiTierData);
        // Big tier letter on the AI's jersey (e.g. "F" for Afsar, "D" for Praneel)
        BuildJerseyLetter(ws, ai.transform, aiTier.ToString()[0], JerseyTrim, aiTierData.widthScale, aiTierData.heightScale);
        // Floating name tag above the AI's head
        BuildNameTag(ws, ai.transform, aiTierData.displayName, aiTierData.heightScale);
        var aiCtrl = ai.AddComponent<AIController>();
        aiCtrl.tier = aiTier;
        aiCtrl.ApplyTierPreset();
        aiCtrl.ball = ball;
        aiCtrl.targetHoop = leftHoop.scoreTrigger;
        aiCtrl.opponent = player.transform;
        aiCtrl.ownHoop = rightHoop.scoreTrigger;

        // Wire player ↔ ai for steal mechanic
        var playerCtrl = player.GetComponent<PlayerController>();
        if (playerCtrl != null)
        {
            playerCtrl.opponent = ai.transform;
            playerCtrl.ball = ball;
        }

        // Game manager + UI
        var gm = BuildGameManager(ball);
        gm.player = player.transform;
        gm.playerSpawnPosition = playerSpawn;
        gm.ai = ai.transform;
        gm.aiSpawnPosition = aiSpawn;
        gm.playerName = PlayerName;
        gm.aiName = aiTierData.displayName;
        BuildUI(gm);

        SetupCamera();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        string playerLabel = string.IsNullOrEmpty(PlayerName) ? "Player" : PlayerName;
        Debug.Log($"[BTierHoops] Court built — {playerLabel} vs {aiTierData.displayName} (tier {aiTier}).");
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

    // ---------- Background ----------
    static void BuildBackground(Sprite ws)
    {
        var root = new GameObject("Background");
        Quad("Sky", ws, new Vector3(0, 4, 5), new Vector3(40, 14, 1), BgDark, -20, root.transform);
        Quad("Wall", ws, new Vector3(0, 1.2f, 4.5f), new Vector3(40, 9, 1), WallMid, -19, root.transform);
        Quad("CrowdBack", ws, new Vector3(0, 0.5f, 4f), new Vector3(40, 3.6f, 1), CrowdDark, -18, root.transform);
        Quad("CrowdFront", ws, new Vector3(0, -0.4f, 3.5f), new Vector3(40, 1.6f, 1), CrowdMid, -17, root.transform);
        for (int i = -12; i <= 12; i++)
        {
            float x = i * 1.0f + 0.4f * Mathf.Sin(i * 1.7f);
            Quad($"Head_{i}", ws, new Vector3(x, 0.35f, 3.4f), new Vector3(0.55f, 0.55f, 1), CrowdDark, -16, root.transform);
        }
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
        effector.rotationalOffset = 180f; // rotate the solid arc to face DOWN — ball from below = solid, ball from above = pass-through
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

        // Two small rim colliders at the front and back tips of the rim — ball passes through the gap, bounces off the tips
        float rimCenterX = hoopX - 0.95f * side;
        float frontRimX = rimCenterX - 0.55f * side; // court-side tip
        float backRimX = rimCenterX + 0.55f * side;  // backboard-side tip
        AddInvisibleCollider(root.transform, "FrontRim", new Vector3(frontRimX, 2.10f, 0), new Vector2(0.10f, 0.10f));
        AddInvisibleCollider(root.transform, "BackRim", new Vector3(backRimX, 2.10f, 0), new Vector2(0.10f, 0.10f));

        // One-way barrier inside the rim opening — solid for balls moving UP through it (so a shot
        // fired from underneath the rim bounces off the underside instead of popping above and
        // dropping back through to score), pass-through for balls falling from ABOVE (legitimate
        // shots and dunks score normally). Positioned so a ball moving up gets stopped while still
        // inside the score-trigger volume — ensures only the upward OnTriggerEnter fires (no score),
        // never a second downward one.
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

        // Score trigger inside the hoop, just below the rim
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
        rb.linearDamping = 0f;        // 0 so launch math hits the target — damping caused airballs
        rb.angularDamping = 0.5f;     // damp spin so ball settles on ground
        rb.mass = 0.6f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = ball.AddComponent<CircleCollider2D>();
        var mat = new PhysicsMaterial2D("BallBouncy") { bounciness = 0.72f, friction = 0.5f };
        col.sharedMaterial = mat;

        // Seams (children, rotate with ball)
        Quad("VertSeam", ws, new Vector3(0, 0, -0.01f), new Vector3(0.06f, 1f, 1f), BallSeam, 17, ball.transform);
        Quad("HorzSeam", ws, new Vector3(0, 0, -0.01f), new Vector3(1f, 0.06f, 1f), BallSeam, 17, ball.transform);
        Quad("CurveSeamL", ws, new Vector3(-0.32f, 0.04f, -0.01f), new Vector3(0.05f, 0.7f, 1f), BallSeam, 17, ball.transform);
        Quad("CurveSeamR", ws, new Vector3(0.32f, 0.04f, -0.01f), new Vector3(0.05f, 0.7f, 1f), BallSeam, 17, ball.transform);

        return ball.AddComponent<BasketballBall>();
    }

    // ---------- Player (side-profile, +X = front/forward) ----------
    static GameObject BuildPlayer(Sprite ws, Sprite circle, Vector3 position, Color jersey, string name, bool attachController, int initialFacing = 1, AIController.TierData? tierData = null)
    {
        Color skin = tierData?.skinColor ?? SkinTone;
        Color hairColor = tierData?.hairColor ?? HairBlack;
        float hScale = tierData?.heightScale ?? 1f;
        float wScale = tierData?.widthScale ?? 1f;

        var player = new GameObject(name);
        player.transform.position = position;

        var rb = player.AddComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = 3.2f;
        rb.linearDamping = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = player.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.85f * wScale, 1.7f * hScale);
        col.offset = Vector2.zero;

        var visuals = new GameObject("Visuals");
        visuals.transform.SetParent(player.transform);
        visuals.transform.localPosition = Vector3.zero;
        float facingSign = initialFacing < 0 ? -1f : 1f;
        visuals.transform.localScale = new Vector3(facingSign * wScale, hScale, 1f);

        Quad("BackLeg", ws, new Vector3(-0.10f, -0.55f, 0), new Vector3(0.26f, 0.55f, 1), ShortsDark, 9, visuals.transform);
        Quad("BackShoe", ws, new Vector3(-0.10f, -0.83f, 0), new Vector3(0.32f, 0.10f, 1), ShoeWhite, 9, visuals.transform);
        Quad("BackArm", ws, new Vector3(-0.18f, 0.05f, 0), new Vector3(0.16f, 0.65f, 1), skin, 10, visuals.transform);
        Quad("BackHand", ws, new Vector3(-0.18f, -0.30f, 0), new Vector3(0.20f, 0.18f, 1), skin, 10, visuals.transform);
        Quad("Body", ws, new Vector3(0f, 0.05f, 0), new Vector3(0.62f, 0.78f, 1), jersey, 11, visuals.transform);
        Quad("Collar", ws, new Vector3(0f, 0.42f, 0), new Vector3(0.36f, 0.05f, 1), JerseyTrim, 12, visuals.transform);
        Quad("BeltLine", ws, new Vector3(0f, -0.30f, 0), new Vector3(0.62f, 0.06f, 1), JerseyTrim, 12, visuals.transform);
        Quad("FrontLeg", ws, new Vector3(0.15f, -0.55f, 0), new Vector3(0.26f, 0.55f, 1), ShortsDark, 12, visuals.transform);
        Quad("FrontShoe", ws, new Vector3(0.15f, -0.83f, 0), new Vector3(0.32f, 0.10f, 1), ShoeWhite, 12, visuals.transform);
        Quad("FrontArm", ws, new Vector3(0.30f, 0.05f, 0), new Vector3(0.18f, 0.7f, 1), skin, 13, visuals.transform);
        Quad("FrontHand", ws, new Vector3(0.30f, -0.32f, 0), new Vector3(0.22f, 0.20f, 1), skin, 13, visuals.transform);

        // Reparent hands under their matching arms so they inherit arm rotation naturally.
        var backArm = visuals.transform.Find("BackArm");
        var backHand = visuals.transform.Find("BackHand");
        var frontArm = visuals.transform.Find("FrontArm");
        var frontHand = visuals.transform.Find("FrontHand");
        if (backArm != null && backHand != null)
        {
            backHand.SetParent(backArm, true);
            backHand.localPosition = new Vector3(0.02f, -0.36f, 0f);
        }
        if (frontArm != null && frontHand != null)
        {
            frontHand.SetParent(frontArm, true);
            frontHand.localPosition = new Vector3(0.02f, -0.37f, 0f);
        }

        var head = new GameObject("Head");
        head.transform.SetParent(visuals.transform);
        head.transform.localPosition = new Vector3(0.05f, 0.65f, 0);
        head.transform.localScale = new Vector3(0.50f, 0.50f, 1f);
        var headSr = head.AddComponent<SpriteRenderer>();
        headSr.sprite = circle;
        headSr.color = skin;
        headSr.sortingOrder = 14;

        var hair = new GameObject("Hair");
        hair.transform.SetParent(visuals.transform);
        hair.transform.localPosition = new Vector3(-0.10f, 0.72f, 0);
        hair.transform.localScale = new Vector3(0.45f, 0.40f, 1f);
        var hairSr = hair.AddComponent<SpriteRenderer>();
        hairSr.sprite = circle;
        hairSr.color = hairColor;
        hairSr.sortingOrder = 14;

        Quad("Eye", ws, new Vector3(0.13f, 0.66f, 0), new Vector3(0.06f, 0.07f, 1), hairColor, 15, visuals.transform);
        Quad("Nose", ws, new Vector3(0.22f, 0.60f, 0), new Vector3(0.06f, 0.05f, 1), skin, 15, visuals.transform);
        Quad("Mouth", ws, new Vector3(0.13f, 0.52f, 0), new Vector3(0.07f, 0.02f, 1), hairColor, 15, visuals.transform);

        var groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform);
        groundCheck.transform.localPosition = new Vector3(0f, -0.88f * hScale, 0f);

        player.AddComponent<CharacterAnimationController>();

        if (attachController)
        {
            var pc = player.AddComponent<PlayerController>();
            pc.groundCheck = groundCheck.transform;
        }
        return player;
    }

    // ---------- Block-letter helpers (sprite-based, no TMP, no gizmo) ----------
    static GameObject BuildBlockLetter(Sprite ws, Transform parent, char letter, Vector3 localPos, Vector2 size, Color color, int sortingOrder)
    {
        var go = new GameObject($"Letter_{letter}");
        go.transform.SetParent(parent);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        // localScale is the world size of the letter's bounding box ([-0.5, 0.5] x [-0.5, 0.5] unit square × size)
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        char key = char.ToUpper(letter);
        if (!LetterShapes.TryGetValue(key, out var shapes)) return go;
        for (int i = 0; i < shapes.Length; i++)
        {
            var s = shapes[i];
            Quad($"part_{i}", ws, new Vector3(s.x, s.y, 0f), new Vector3(s.w, s.h, 1f), color, sortingOrder, go.transform);
        }
        return go;
    }

    static void BuildBlockText(Sprite ws, Transform parent, string text, Vector3 localPos, Vector2 letterSize, float letterSpacing, Color color, int sortingOrder)
    {
        var root = new GameObject($"Text_{text}");
        root.transform.SetParent(parent);
        root.transform.localPosition = localPos;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        int n = text.Length;
        float spanWidth = (n - 1) * letterSpacing;
        float startX = -spanWidth * 0.5f;
        for (int i = 0; i < n; i++)
        {
            BuildBlockLetter(ws, root.transform, text[i], new Vector3(startX + i * letterSpacing, 0f, 0f), letterSize, color, sortingOrder);
        }
    }

    // ---------- Jersey tier letter (large letter on chest) ----------
    static void BuildJerseyLetter(Sprite ws, Transform playerRoot, char letter, Color textColor, float widthScale, float heightScale)
    {
        // Parented to the player ROOT (not Visuals) so the letter never mirrors when the AI changes facing.
        // -z so it draws in front of body sprites; sortingOrder 12 sits above body (11), below front arm (13).
        var go = BuildBlockLetter(ws, playerRoot, letter,
            new Vector3(0f, 0.05f * heightScale, -0.01f),
            new Vector2(0.45f * widthScale, 0.55f * heightScale),
            textColor, 12);
        go.name = "JerseyLetter";
    }

    // ---------- Floating name tag (block-letter, above the head) ----------
    static void BuildNameTag(Sprite ws, Transform playerRoot, string displayName, float heightScale)
    {
        // Parented to player root so it never mirrors when facing flips. Y offset scales with character height.
        BuildBlockText(ws, playerRoot, displayName.ToUpper(),
            new Vector3(0f, 1.10f * heightScale, 0f),
            new Vector2(0.22f, 0.28f), 0.28f,
            Color.white, 50);
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
        gm.ball = ball;
        gm.ballSpawnPosition = new Vector3(0f, 1f, 0f);
        return gm;
    }

    // ---------- UI (timer, score, win panel) ----------
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

        // Timer (top center, large)
        gm.timerText = MakeTMPText(canvasGO.transform, "TimerText", "1:00",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -30f), new Vector2(0f, 90f),
            72f, TextAlignmentOptions.Center, font);
        gm.timerText.fontStyle = FontStyles.Bold;

        // Score (below timer)
        gm.scoreText = MakeTMPText(canvasGO.transform, "ScoreText", "PLAYER  0   -   0  CPU",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -120f), new Vector2(0f, 80f),
            56f, TextAlignmentOptions.Center, font);

        // Win panel (full-screen overlay, hidden until match ends)
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
        cam.backgroundColor = BgDark;
    }

    // ---------- Helpers ----------
    static GameObject Quad(string name, Sprite sprite, Vector3 pos, Vector3 scale, Color color, int sortingOrder, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        return go;
    }

    static Sprite MakeWhiteSprite()
    {
        var tex = new Texture2D(2, 2);
        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
    }

    static Sprite MakeCircleSprite(int size)
    {
        var tex = new Texture2D(size, size);
        var pixels = new Color[size * size];
        Vector2 c = new Vector2((size - 1) / 2f, (size - 1) / 2f);
        float r = size / 2f - 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                pixels[y * size + x] = (d <= r) ? Color.white : new Color(0, 0, 0, 0);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
