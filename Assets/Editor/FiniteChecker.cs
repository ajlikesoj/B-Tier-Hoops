#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
internal static class FiniteChecker
{
    static double s_lastTime;

    static FiniteChecker()
    {
        s_lastTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += Update;
    }

    static void Update()
    {
        // Run roughly once per second to avoid spamming the editor.
        if (EditorApplication.timeSinceStartup - s_lastTime < 1.0) return;
        s_lastTime = EditorApplication.timeSinceStartup;

        var all = Resources.FindObjectsOfTypeAll<Transform>();
        int reported = 0;
        foreach (var t in all)
        {
            if (t == null) continue;

            Vector3 pos = t.position;
            Vector3 lpos = t.localPosition;
            Vector3 scale = t.localScale;

            bool bad = !IsFinite(pos) || !IsFinite(lpos) || !IsFinite(scale);
            if (bad)
            {
                Debug.LogError($"[FiniteChecker] Non-finite transform: {GetPath(t)} — pos={pos} localPos={lpos} scale={scale}", t);
                reported++;
                if (reported >= 30) break;
            }
        }

        if (reported > 0) EditorApplication.Beep();
    }

    static bool IsFinite(Vector3 v)
    {
        return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }

    static string GetPath(Transform t)
    {
        string path = t.name;
        var p = t.parent;
        while (p != null)
        {
            path = p.name + "/" + path;
            p = p.parent;
        }
        return path;
    }
}
#endif
