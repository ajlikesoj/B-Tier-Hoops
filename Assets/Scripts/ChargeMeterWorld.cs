using UnityEngine;

public class ChargeMeterWorld : MonoBehaviour
{
    public ShootingController shooter;
    public Transform meterRoot;
    public Transform fillTransform;
    public SpriteRenderer fillRenderer;
    public float baseWidth = 1.4f;
    public float fillHeight = 0.14f;

    static readonly Color UnderColor = new Color(0.95f, 0.75f, 0.20f); // yellow — too weak
    static readonly Color SweetColor = new Color(0.30f, 0.95f, 0.30f); // green — perfect
    static readonly Color OverColor = new Color(0.95f, 0.20f, 0.20f);  // red — overshoot

    void LateUpdate()
    {
        if (shooter == null || meterRoot == null || fillTransform == null) return;

        bool show = shooter.isCharging;
        meterRoot.gameObject.SetActive(show);
        if (!show) return;

        float charge = shooter.currentCharge;
        float w = Mathf.Max(0.01f, baseWidth * charge);
        var lp = fillTransform.localPosition;
        lp.x = -baseWidth * 0.5f + w * 0.5f;
        fillTransform.localPosition = lp;
        fillTransform.localScale = new Vector3(w, fillHeight, 1f);

        Color c;
        if (charge > shooter.perfectChargeMax) c = OverColor;
        else if (charge >= shooter.perfectChargeMin) c = SweetColor;
        else c = UnderColor;

        if (fillRenderer != null)
            fillRenderer.color = c;
    }
}
