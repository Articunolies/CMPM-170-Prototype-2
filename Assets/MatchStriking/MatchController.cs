using UnityEditor.Overlays;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class MatchController : MonoBehaviour
{
    public enum MatchState { Unlit, Lit, BurnedOut }

    [Header("Mouse follow")]
    [SerializeField] private float followZ = 0f;          // keep 0 in a 2D scene

    [Header("Striking (to ignite)")]
    [SerializeField] private float minStrikeSpeed = 2.0f;  // world units/sec
    [SerializeField] private float strikeProgressNeeded = 0.35f;
    [SerializeField] private bool requireLMBForStrike = true;

    [Header("Heating the wick")]
    [SerializeField] private bool requireLMBToHeat = true; // hold LMB while over wick

    [Header("After ignition")]
    [SerializeField] private GameObject flameVisual;        // child flame sprite/particles
    [SerializeField] private float burnSeconds = 6f;        // <=0 disables burnout

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private bool logSpeedEveryFrame = false;

    private Camera _cam;
    private Vector3 _prevPos;
    private float _lastSpeed;
    private float _strikeProgress;
    private float _burnTimer;
    private MatchState _state = MatchState.Unlit;

    public bool IsLit => _state == MatchState.Lit;

    #region Helpers
    private void Log(string msg)
    {
        if (debugLogs) Debug.Log($"[Match] {msg}");
    }

    private bool LMB => Input.GetMouseButton(0);
    #endregion

    private void Awake()
    {
        _cam = Camera.main;
        if (!_cam) Debug.LogWarning("[Match] No Main Camera found.");
        if (flameVisual) flameVisual.SetActive(false);

        // Strongly recommended Rigidbody2D settings:
        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        // The collider on THIS object should be on the match head (not trigger).
        var col = GetComponent<Collider2D>();
        if (col.isTrigger)
        {
            Debug.LogWarning("[Match] Head collider should NOT be a trigger. Turning it off.");
            col.isTrigger = false;
        }
    }

    private void OnEnable()
    {
        Cursor.visible = false;
        ResetUnlit();
        Log("Enabled. State=Unlit.");
    }

    private void OnDisable()
    {
        Cursor.visible = true;
        Log("Disabled.");
    }

    public void ResetUnlit()
    {
        _state = MatchState.Unlit;
        _strikeProgress = 0f;
        _burnTimer = 0f;
        if (flameVisual) flameVisual.SetActive(false);
    }

    private void Update()
    {
        // 1) Follow mouse in world space
        Vector3 m = Input.mousePosition;
        m.z = Mathf.Abs((_cam ? _cam.transform.position.z : 10f) - followZ);
        Vector3 world = (_cam ? _cam.ScreenToWorldPoint(m) : new Vector3(0, 0, followZ));
        world.z = followZ;
        transform.position = world;

        // 2) Speed in world units/sec
        _lastSpeed = (transform.position - _prevPos).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        _prevPos = transform.position;

        if (logSpeedEveryFrame) Log($"Speed={_lastSpeed:F2} wu/s");

        // 3) Burnout timer
        if (_state == MatchState.Lit && burnSeconds > 0f)
        {
            _burnTimer += Time.deltaTime;
            if (_burnTimer >= burnSeconds)
            {
                _state = MatchState.BurnedOut;
                if (flameVisual) flameVisual.SetActive(false);
                Log("Match burned out.");
            }
        }

        // Handy test reset
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetUnlit();
            Log("Manual reset to Unlit.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Log($"ENTER -> {other.name} (state={_state})");
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // ---------- A) STRIKING ----------
        // Allow striking when Unlit OR BurnedOut (so you can re-light).
        if ((_state == MatchState.Unlit || _state == MatchState.BurnedOut) && other.GetComponent<StrikeArea>())
        {
            bool okLMB = !requireLMBForStrike || LMB;
            if (!okLMB)
            {
                Log("On striker but LMB not held.");
            }
            else
            {
                Log($"On striker '{other.name}' | speed={_lastSpeed:F2}, need>={minStrikeSpeed:F2}");
                if (_lastSpeed >= minStrikeSpeed)
                {
                    // accumulate progress proportional to speed
                    _strikeProgress += _lastSpeed * Time.deltaTime * 0.2f;
                    Log($"Striking... progress={_strikeProgress:F3}/{strikeProgressNeeded:F3}");

                    if (_strikeProgress >= strikeProgressNeeded)
                        Ignite();
                }
                else
                {
                    Log("On striker but too slow.");
                }
            }
        }

        // ---------- B) HEATING THE WICK ----------
        // Be robust about where WickZone lives in the hierarchy
        LightZone wick =
            other.GetComponent<LightZone>() ??
            other.GetComponentInParent<LightZone>() ??
            other.GetComponentInChildren<LightZone>();

        if (wick)
        {
            Log($"Over WICK '{other.name}' (state={_state}, LMB={LMB})");

            bool okLMBHeat = !requireLMBToHeat || LMB;

            if (_state == MatchState.Lit && okLMBHeat)
            {
                wick.AddHeat(Time.deltaTime); // pass time as "heat"
                Log($"Heating wick +{Time.deltaTime:F3}s");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Log($"EXIT -> {other.name} (state={_state})");
    }

    private void Ignite()
    {
        _state = MatchState.Lit;
        if (flameVisual) flameVisual.SetActive(true);
        _burnTimer = 0f; // restart burnout timer
        Log("Match lit!");
    }
}
