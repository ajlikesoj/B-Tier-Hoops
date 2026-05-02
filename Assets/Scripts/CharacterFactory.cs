using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime-callable factory for building characters (player + AI), jersey letters, and floating
/// name tags. Logic was extracted from the editor-only CourtBuilder so AISpawner can construct
/// the AI dynamically at runtime based on the tier the user picked from the main menu.
///
/// Both CourtBuilder (edit-time scene assembly) and AISpawner (runtime) call into this class.
/// </summary>
public static class CharacterFactory
{
    // ---------- Body palette (shared across all characters) ----------
    public static readonly Color SkinDefault = new Color(0.93f, 0.74f, 0.56f);
    public static readonly Color HairDefault = new Color(0.10f, 0.07f, 0.05f);
    static readonly Color ShortsDark = new Color(0.12f, 0.12f, 0.14f);
    static readonly Color ShoeWhite = new Color(0.96f, 0.96f, 0.96f);
    public static readonly Color JerseyTrim = new Color(0.98f, 0.98f, 0.95f);

    // ---------- Cached sprites (regenerated lazily; survives scene loads but recreated if cleared) ----------
    static Sprite _whiteSprite;
    static Sprite _circleSprite;
    public static Sprite WhiteSprite { get { if (_whiteSprite == null) _whiteSprite = MakeWhiteSprite(); return _whiteSprite; } }
    public static Sprite CircleSprite { get { if (_circleSprite == null) _circleSprite = MakeCircleSprite(96); return _circleSprite; } }

    // ---------- Block-letter shape table (centerX, centerY, width, height) in [-0.5, 0.5] unit square ----------
    static readonly Dictionary<char, (float x, float y, float w, float h)[]> LetterShapes =
        new Dictionary<char, (float x, float y, float w, float h)[]>
    {
        ['A'] = new (float, float, float, float)[] {
            (-0.40f, -0.05f, 0.20f, 0.90f),
            ( 0.40f, -0.05f, 0.20f, 0.90f),
            ( 0.00f,  0.40f, 1.00f, 0.20f),
            ( 0.00f,  0.00f, 1.00f, 0.20f),
        },
        ['B'] = new (float, float, float, float)[] {
            (-0.40f,  0.00f, 0.20f, 1.00f),
            (-0.10f,  0.40f, 0.80f, 0.20f),
            (-0.10f,  0.00f, 0.80f, 0.20f),
            (-0.10f, -0.40f, 0.80f, 0.20f),
            ( 0.40f,  0.20f, 0.20f, 0.40f),
            ( 0.40f, -0.20f, 0.20f, 0.40f),
        },
        ['C'] = new (float, float, float, float)[] {
            (-0.40f,  0.00f, 0.20f, 0.80f),
            ( 0.00f,  0.40f, 1.00f, 0.20f),
            ( 0.00f, -0.40f, 1.00f, 0.20f),
        },
        ['D'] = new (float, float, float, float)[] {
            (-0.40f,  0.00f, 0.20f, 1.00f),
            (-0.10f,  0.40f, 0.80f, 0.20f),
            (-0.10f, -0.40f, 0.80f, 0.20f),
            ( 0.40f,  0.00f, 0.20f, 0.80f),
        },
        ['E'] = new (float, float, float, float)[] {
            (-0.40f,  0.00f, 0.20f, 1.00f),
            ( 0.00f,  0.40f, 1.00f, 0.20f),
            (-0.05f,  0.00f, 0.90f, 0.20f),
            ( 0.00f, -0.40f, 1.00f, 0.20f),
        },
        ['F'] = new (float, float, float, float)[] {
            (-0.40f,  0.00f, 0.20f, 1.00f),
            ( 0.00f,  0.40f, 1.00f, 0.20f),
            (-0.05f,  0.00f, 0.90f, 0.20f),
        },
        ['G'] = new (float, float, float, float)[] {
            (-0.40f,  0.00f, 0.20f, 0.80f),
            ( 0.00f,  0.40f, 1.00f, 0.20f),
            ( 0.00f, -0.40f, 1.00f, 0.20f),
            ( 0.40f, -0.20f, 0.20f, 0.40f),
            ( 0.20f, -0.05f, 0.40f, 0.18f),
        },
        ['H'] = new (float, float, float, float)[] {
            (-0.40f,  0.00f, 0.20f, 1.00f),
            ( 0.40f,  0.00f, 0.20f, 1.00f),
            ( 0.00f,  0.00f, 1.00f, 0.20f),
        },
        ['I'] = new (float, float, float, float)[] {
            ( 0.00f,  0.40f, 1.00f, 0.20f),
            ( 0.00f,  0.00f, 0.20f, 0.80f),
            ( 0.00f, -0.40f, 1.00f, 0.20f),
        },
        ['J'] = new (float, float, float, float)[] {
            ( 0.10f,  0.40f, 0.80f, 0.20f),
            ( 0.30f,  0.05f, 0.20f, 0.90f),
            ( 0.00f, -0.40f, 0.60f, 0.20f),
            (-0.30f, -0.20f, 0.20f, 0.30f),
        },
        ['K'] = new (float, float, float, float)[] {
            (-0.40f,  0.00f, 0.20f, 1.00f),
            (-0.10f,  0.05f, 0.20f, 0.20f),
            ( 0.10f,  0.25f, 0.20f, 0.20f),
            ( 0.30f,  0.40f, 0.20f, 0.20f),
            (-0.10f, -0.05f, 0.20f, 0.20f),
            ( 0.10f, -0.25f, 0.20f, 0.20f),
            ( 0.30f, -0.40f, 0.20f, 0.20f),
        },
        ['L'] = new (float, float, float, float)[] {
            (-0.40f,  0.00f, 0.20f, 1.00f),
            ( 0.00f, -0.40f, 1.00f, 0.20f),
        },
        ['N'] = new (float, float, float, float)[] {
            (-0.40f,  0.00f, 0.20f, 1.00f),
            ( 0.40f,  0.00f, 0.20f, 1.00f),
            (-0.15f,  0.20f, 0.20f, 0.20f),
            ( 0.00f,  0.00f, 0.20f, 0.20f),
            ( 0.15f, -0.20f, 0.20f, 0.20f),
        },
        ['P'] = new (float, float, float, float)[] {
            (-0.40f,  0.00f, 0.20f, 1.00f),
            (-0.10f,  0.40f, 0.80f, 0.20f),
            (-0.10f,  0.05f, 0.80f, 0.20f),
            ( 0.40f,  0.25f, 0.20f, 0.50f),
        },
        ['R'] = new (float, float, float, float)[] {
            (-0.40f,  0.00f, 0.20f, 1.00f),
            (-0.10f,  0.40f, 0.80f, 0.20f),
            (-0.10f,  0.05f, 0.80f, 0.20f),
            ( 0.40f,  0.25f, 0.20f, 0.50f),
            ( 0.10f, -0.10f, 0.20f, 0.20f),
            ( 0.30f, -0.30f, 0.20f, 0.20f),
        },
        ['S'] = new (float, float, float, float)[] {
            ( 0.00f,  0.40f, 1.00f, 0.20f),
            (-0.40f,  0.20f, 0.20f, 0.40f),
            ( 0.00f,  0.00f, 1.00f, 0.20f),
            ( 0.40f, -0.20f, 0.20f, 0.40f),
            ( 0.00f, -0.40f, 1.00f, 0.20f),
        },
        ['V'] = new (float, float, float, float)[] {
            (-0.40f,  0.20f, 0.20f, 0.60f),
            ( 0.40f,  0.20f, 0.20f, 0.60f),
            (-0.20f, -0.10f, 0.20f, 0.30f),
            ( 0.20f, -0.10f, 0.20f, 0.30f),
            ( 0.00f, -0.35f, 0.20f, 0.30f),
        },
        ['Y'] = new (float, float, float, float)[] {
            (-0.40f,  0.30f, 0.20f, 0.40f),
            ( 0.40f,  0.30f, 0.20f, 0.40f),
            (-0.20f,  0.05f, 0.20f, 0.20f),
            ( 0.20f,  0.05f, 0.20f, 0.20f),
            ( 0.00f, -0.20f, 0.20f, 0.60f),
        },
    };

    // ---------- Sprite primitive ----------
    public static GameObject Quad(string name, Sprite sprite, Vector3 pos, Vector3 scale, Color color, int sortingOrder, Transform parent)
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

    // ---------- Block-letter helpers (sprite-based — no TMP, no editor gizmo) ----------
    public static GameObject BuildBlockLetter(Transform parent, char letter, Vector3 localPos, Vector2 size, Color color, int sortingOrder)
    {
        var go = new GameObject($"Letter_{letter}");
        go.transform.SetParent(parent);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        char key = char.ToUpper(letter);
        if (!LetterShapes.TryGetValue(key, out var shapes)) return go;
        for (int i = 0; i < shapes.Length; i++)
        {
            var s = shapes[i];
            Quad($"part_{i}", WhiteSprite, new Vector3(s.x, s.y, 0f), new Vector3(s.w, s.h, 1f), color, sortingOrder, go.transform);
        }
        return go;
    }

    public static void BuildBlockText(Transform parent, string text, Vector3 localPos, Vector2 letterSize, float letterSpacing, Color color, int sortingOrder)
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
            BuildBlockLetter(root.transform, text[i], new Vector3(startX + i * letterSpacing, 0f, 0f), letterSize, color, sortingOrder);
        }
    }

    /// <summary>Big tier letter on the chest. Parented to player root so it doesn't mirror with facing.</summary>
    public static void BuildJerseyLetter(Transform playerRoot, char letter, Color textColor, float widthScale, float heightScale)
    {
        var go = BuildBlockLetter(playerRoot, letter,
            new Vector3(0f, 0.05f * heightScale, -0.01f),
            new Vector2(0.45f * widthScale, 0.55f * heightScale),
            textColor, 12);
        go.name = "JerseyLetter";
    }

    /// <summary>Floating name above the head, built from block letters so no TMP gizmo can appear.</summary>
    public static void BuildNameTag(Transform playerRoot, string displayName, float heightScale)
    {
        if (string.IsNullOrEmpty(displayName)) return;
        BuildBlockText(playerRoot, displayName.ToUpper(),
            new Vector3(0f, 1.10f * heightScale, 0f),
            new Vector2(0.22f, 0.28f), 0.28f,
            Color.white, 50);
    }

    // ---------- Character body (side-profile, +X = front/forward) ----------
    /// <summary>
    /// Build a character GameObject with rigidbody, collider, multi-part visuals, ground check, and
    /// (optionally) a PlayerController. tierData supplies skin/hair color and height/width scaling.
    /// </summary>
    public static GameObject BuildCharacter(Vector3 position, Color jersey, string name, bool attachPlayerController, int initialFacing = 1, AIController.TierData? tierData = null)
    {
        Color skin = tierData?.skinColor ?? SkinDefault;
        Color hairColor = tierData?.hairColor ?? HairDefault;
        float hScale = tierData?.heightScale ?? 1f;
        float wScale = tierData?.widthScale ?? 1f;

        var player = new GameObject(name);
        player.transform.position = position;

        var rb = player.AddComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = 3.2f;
        rb.linearDamping = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Capsule collider — rounded top means balls can't balance on the head (the old BoxCollider2D
        // gave a flat surface where the ball would stably rest, requiring physics dislodge tricks).
        var col = player.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.85f * wScale, 1.7f * hScale);
        col.direction = CapsuleDirection2D.Vertical;
        col.offset = Vector2.zero;

        var visuals = new GameObject("Visuals");
        visuals.transform.SetParent(player.transform);
        visuals.transform.localPosition = Vector3.zero;
        float facingSign = initialFacing < 0 ? -1f : 1f;
        visuals.transform.localScale = new Vector3(facingSign * wScale, hScale, 1f);

        var ws = WhiteSprite;
        var circle = CircleSprite;

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

        if (attachPlayerController)
        {
            var pc = player.AddComponent<PlayerController>();
            pc.groundCheck = groundCheck.transform;
        }
        return player;
    }

    /// <summary>
    /// High-level: build the AI character + jersey letter + name tag, attach AIController, and wire
    /// references. Used at edit-time by CourtBuilder when the scene is initially constructed *and*
    /// at runtime by AISpawner when a new tier is selected from the main menu.
    /// </summary>
    public static GameObject BuildAI(Vector3 spawnPos, Color jerseyColor, AIController.Tier tier,
                                     BasketballBall ball, Transform targetHoop, Transform ownHoop, Transform opponent)
    {
        var tierData = AIController.GetTierData(tier);
        var ai = BuildCharacter(spawnPos, jerseyColor, "AI", attachPlayerController: false, initialFacing: -1, tierData: tierData);

        BuildJerseyLetter(ai.transform, tier.ToString()[0], JerseyTrim, tierData.widthScale, tierData.heightScale);
        BuildNameTag(ai.transform, tierData.displayName, tierData.heightScale);

        var aiCtrl = ai.AddComponent<AIController>();
        aiCtrl.tier = tier;
        aiCtrl.ApplyTierPreset();
        aiCtrl.ball = ball;
        aiCtrl.targetHoop = targetHoop;
        aiCtrl.ownHoop = ownHoop;
        aiCtrl.opponent = opponent;

        return ai;
    }
}
