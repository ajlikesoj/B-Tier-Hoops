using System.Collections.Generic;
using UnityEngine;

public class HoopNetAnimator : MonoBehaviour
{
    [Header("Swish")]
    public float swishDuration = 0.26f;
    public float lateralSway = 0.10f;
    public float compressY = 0.78f;

    struct NetPart
    {
        public Transform t;
        public Vector3 pos;
        public Vector3 scale;
    }

    readonly List<NetPart> parts = new List<NetPart>();
    float swishTimer;
    float swayDir = 1f;

    void Awake()
    {
        parts.Clear();
        CacheFromChildren(transform);
    }

    void Update()
    {
        if (parts.Count == 0) return;

        if (swishTimer > 0f)
        {
            swishTimer -= Time.deltaTime;
            float p = 1f - Mathf.Clamp01(swishTimer / Mathf.Max(0.001f, swishDuration));
            float envelope = Mathf.Sin(p * Mathf.PI);

            for (int i = 0; i < parts.Count; i++)
            {
                NetPart part = parts[i];
                if (part.t == null) continue;

                float signed = part.pos.x >= 0f ? 1f : -1f;
                float xOffset = swayDir * signed * lateralSway * envelope;
                float yScale = Mathf.Lerp(1f, compressY, envelope);

                part.t.localPosition = part.pos + new Vector3(xOffset, 0f, 0f);
                part.t.localScale = new Vector3(part.scale.x, part.scale.y * yScale, part.scale.z);
            }
        }
        else
        {
            RestoreBasePose();
        }
    }

    public void TriggerSwish(Vector2 ballVelocity)
    {
        if (parts.Count == 0) return;
        swayDir = ballVelocity.x >= 0f ? 1f : -1f;
        swishTimer = swishDuration;
    }

    void RestoreBasePose()
    {
        for (int i = 0; i < parts.Count; i++)
        {
            NetPart part = parts[i];
            if (part.t == null) continue;
            part.t.localPosition = part.pos;
            part.t.localScale = part.scale;
        }
    }

    void CacheFromChildren(Transform root)
    {
        if (root == null) return;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            string n = child.name;
            if (n.StartsWith("Net_") || n.StartsWith("NetCross"))
            {
                parts.Add(new NetPart { t = child, pos = child.localPosition, scale = child.localScale });
            }

            CacheFromChildren(child);
        }
    }
}
