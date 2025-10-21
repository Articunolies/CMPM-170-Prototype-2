using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LightZone : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private CandleTarget candle; // auto-filled from parent if left empty

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private void Awake()
    {
        if (!candle) candle = GetComponentInParent<CandleTarget>();
        if (!candle)
            Debug.LogWarning("[WickZone] No CandleTarget found in parent chain.");
    }

    public void AddHeat(float dt)
    {
        candle?.AddHeat(dt);
    }

    private void Log(string msg)
    {
        if (debugLogs) Debug.Log($"[WickZone] {msg}");
    }

    private void OnTriggerEnter2D(Collider2D other) => Log($"ENTER by '{other.name}'");
    private void OnTriggerExit2D(Collider2D other) => Log($"EXIT by '{other.name}'");
}
