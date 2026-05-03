using UnityEngine;

public static class MathUtils
{
    public static bool IsFinite(Vector3 v)
    {
        return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }

    public static bool IsFinite(Vector2 v)
    {
        return float.IsFinite(v.x) && float.IsFinite(v.y);
    }

    public static Vector3 ClampFinite(Vector3 v, Vector3 fallback)
    {
        if (!IsFinite(v)) return fallback;
        return v;
    }

    public static Vector2 ClampFinite(Vector2 v, Vector2 fallback)
    {
        if (!IsFinite(v)) return fallback;
        return v;
    }
}
