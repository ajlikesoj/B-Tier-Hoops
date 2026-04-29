using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class CourtBuilder
{
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
    static readonly Color JerseyTrim = new Color(0.98f, 0.98f, 0.95f);

    // --- Ball palette ---
    static readonly Color BallOrange = new Color(0.93f, 0.45f, 0.10f);
    static readonly Color BallSeam = new Color(0f, 0f, 0f, 0.75f);

    struct HoopRefs { public GameObject root; public Transform scoreTrigger; }

    [MenuItem("BTierHoops/Build Court Scene")]
    public static void BuildCourt()
    {
        ClearSceneInternal();

        var ws = MakeWhiteSprite();
        var circle = MakeCircleSprite(96);

        BuildBackground(ws);
        BuildFloor(ws);

        var rightHoop = BuildHoop(ws, hoopX: 9f, side: +1);
        BuildHoop(ws, hoopX: -9f, side: -1);

        var ball = BuildBall(ws, circle, new Vector3(0f, 1f, 0f));
        BuildPlayerContainment();
        var playerSpawn = new Vector3(-3f, 0f, 0f);
        var player = BuildPlayer(ws, circle, playerSpawn, JerseyRed, "Player", attachController: true);

        // Wire shooting controller
        var shooter = player.AddComponent<ShootingController>();
        shooter.ball = ball;
        shooter.targetHoop = rightHoop.scoreTrigger;

        // Charge meter visual above player
        BuildChargeMeter(ws, player.transform, shooter);

        // Game manager + UI
        var gm = BuildGameManager(ball);
        gm.player = player.transform;
        gm.playerSpawnPosition = playerSpawn;
        BuildUI(gm);

        SetupCamera();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[BTierHoops] Court built. Walk into the ball, hold F to charge, release to shoot.");
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
    static GameObject BuildPlayer(Sprite ws, Sprite circle, Vector3 position, Color jersey, string name, bool attachController)
    {
        var player = new GameObject(name);
        player.transform.position = position;

        var rb = player.AddComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = 3.2f;
        rb.linearDamping = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = player.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.85f, 1.7f);
        col.offset = Vector2.zero;

        var visuals = new GameObject("Visuals");
        visuals.transform.SetParent(player.transform);
        visuals.transform.localPosition = Vector3.zero;
        visuals.transform.localScale = new Vector3(1f, 1f, 1f);

        Quad("BackLeg", ws, new Vector3(-0.10f, -0.55f, 0), new Vector3(0.26f, 0.55f, 1), ShortsDark, 9, visuals.transform);
        Quad("BackShoe", ws, new Vector3(-0.10f, -0.83f, 0), new Vector3(0.32f, 0.10f, 1), ShoeWhite, 9, visuals.transform);
        Quad("BackArm", ws, new Vector3(-0.18f, 0.05f, 0), new Vector3(0.16f, 0.65f, 1), SkinTone, 10, visuals.transform);
        Quad("BackHand", ws, new Vector3(-0.18f, -0.30f, 0), new Vector3(0.20f, 0.18f, 1), SkinTone, 10, visuals.transform);
        Quad("Body", ws, new Vector3(0f, 0.05f, 0), new Vector3(0.62f, 0.78f, 1), jersey, 11, visuals.transform);
        Quad("Collar", ws, new Vector3(0f, 0.42f, 0), new Vector3(0.36f, 0.05f, 1), JerseyTrim, 12, visuals.transform);
        Quad("BeltLine", ws, new Vector3(0f, -0.30f, 0), new Vector3(0.62f, 0.06f, 1), JerseyTrim, 12, visuals.transform);
        Quad("FrontLeg", ws, new Vector3(0.15f, -0.55f, 0), new Vector3(0.26f, 0.55f, 1), ShortsDark, 12, visuals.transform);
        Quad("FrontShoe", ws, new Vector3(0.15f, -0.83f, 0), new Vector3(0.32f, 0.10f, 1), ShoeWhite, 12, visuals.transform);
        Quad("FrontArm", ws, new Vector3(0.30f, 0.05f, 0), new Vector3(0.18f, 0.7f, 1), SkinTone, 13, visuals.transform);
        Quad("FrontHand", ws, new Vector3(0.30f, -0.32f, 0), new Vector3(0.22f, 0.20f, 1), SkinTone, 13, visuals.transform);

        var head = new GameObject("Head");
        head.transform.SetParent(visuals.transform);
        head.transform.localPosition = new Vector3(0.05f, 0.65f, 0);
        head.transform.localScale = new Vector3(0.50f, 0.50f, 1f);
        var headSr = head.AddComponent<SpriteRenderer>();
        headSr.sprite = circle;
        headSr.color = SkinTone;
        headSr.sortingOrder = 14;

        var hair = new GameObject("Hair");
        hair.transform.SetParent(visuals.transform);
        hair.transform.localPosition = new Vector3(-0.10f, 0.72f, 0);
        hair.transform.localScale = new Vector3(0.45f, 0.40f, 1f);
        var hairSr = hair.AddComponent<SpriteRenderer>();
        hairSr.sprite = circle;
        hairSr.color = HairBlack;
        hairSr.sortingOrder = 14;

        Quad("Eye", ws, new Vector3(0.13f, 0.66f, 0), new Vector3(0.06f, 0.07f, 1), HairBlack, 15, visuals.transform);
        Quad("Nose", ws, new Vector3(0.22f, 0.60f, 0), new Vector3(0.06f, 0.05f, 1), SkinTone, 15, visuals.transform);
        Quad("Mouth", ws, new Vector3(0.13f, 0.52f, 0), new Vector3(0.07f, 0.02f, 1), HairBlack, 15, visuals.transform);

        var groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform);
        groundCheck.transform.localPosition = new Vector3(0f, -0.88f, 0f);

        if (attachController)
        {
            var pc = player.AddComponent<PlayerController>();
            pc.groundCheck = groundCheck.transform;
        }
        return player;
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
