using UnityEngine;

public class CandleTarget : MonoBehaviour
{
    [Header("Wick heat to ignite")]
    [SerializeField] private float secondsToIgnite = 1.0f;

    [Header("Visuals")]
    [SerializeField] private GameObject flameVisual; // assign candle flame sprite/particles

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private bool logHeatEveryAdd = true;

    private float _heat;
    private bool _lit;

    public float Heat => _heat;
    public bool IsLit => _lit;

    private void Log(string msg)
    {
        if (debugLogs) Debug.Log($"[Wick] {msg}");
    }

    private void Start()
    {
        Log("CandleTarget started.");
    }

    public void ResetCandle()
    {
        _heat = 0f;
        _lit = false;
        if (flameVisual) flameVisual.SetActive(false);
        Log("ResetCandle -> heat=0, lit=false, flame hidden.");
    }

    public void AddHeat(float dt)
    {
        if (_lit) return;

        _heat += dt;
        if (logHeatEveryAdd)
            Log($"+{dt:F3}s heat -> total={_heat:F3}/{secondsToIgnite:F3} (lit={_lit})");

        if (_heat >= secondsToIgnite)
            Ignite();
    }

    private void Ignite()
    {
        if (_lit) return;
        _lit = true;
        if (flameVisual) flameVisual.SetActive(true);
        Log($"Candle lit! (needed {secondsToIgnite:F3}s, had {_heat:F3}s)");
    }

    // Optional: visualize the wick zone radius in Scene view when selected
    private void OnDrawGizmosSelected()
    {
        var wick = GetComponentInChildren<CircleCollider2D>();
        if (wick == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            wick.transform.position,
            wick.radius * Mathf.Max(wick.transform.lossyScale.x, wick.transform.lossyScale.y)
        );
    }
}
