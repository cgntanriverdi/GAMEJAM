using UnityEngine;

/// <summary>
/// Ekranın herhangi bir yerindeki swipe hareketini algılar, yönü hesaplar
/// ve GameManager.TryMovePlayer() ile ilerletir.
///
/// Tasarım notu (Plan §7.6.1): swipe, parmak grid üzerinde olmak zorunda
/// değildir — küçük grid'lerde parmak altı görünürlüğü için kritik UX kararı.
/// </summary>
public class SwipeInputController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private SwipeSettings settings;  // ScriptableObject

    [Header("Debug (Plan §7.14)")]
    [SerializeField] private bool debugSwipeLog = false;

    // GameManager tarafından her başarılı hamlede SetPlayerPosition() ile güncellenir.
    private GridCoord _currentPlayerPos;
    private bool      _inputEnabled = true;

    // Touch tracking
    private Vector2 _touchStartPos;
    private float   _touchStartTime;
    private bool    _tracking;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Update()
    {
        if (!_inputEnabled) return;

        HandleTouchInput();

#if UNITY_EDITOR
        HandleMouseFallback();
#endif
    }

    // ── Touch ─────────────────────────────────────────────────────────────────

    private void HandleTouchInput()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            _touchStartPos  = touch.position;
            _touchStartTime = Time.time;
            _tracking       = true;
        }
        else if (touch.phase == TouchPhase.Ended && _tracking)
        {
            _tracking = false;
            TryRegisterSwipe(touch.position, Time.time - _touchStartTime);
        }
        else if (touch.phase == TouchPhase.Canceled)
        {
            _tracking = false;
        }
    }

#if UNITY_EDITOR
    // Editor'da mouse ile test kolaylığı — builds'ta derlenmez.
    private Vector2 _mouseStartPos;
    private float   _mouseStartTime;
    private bool    _mouseTracking;

    private void HandleMouseFallback()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _mouseStartPos  = Input.mousePosition;
            _mouseStartTime = Time.time;
            _mouseTracking  = true;
        }
        else if (Input.GetMouseButtonUp(0) && _mouseTracking)
        {
            _mouseTracking = false;
            TryRegisterSwipe(Input.mousePosition, Time.time - _mouseStartTime);
        }
    }
#endif

    // ── Swipe evaluation ──────────────────────────────────────────────────────

    private void TryRegisterSwipe(Vector2 endPos, float duration)
    {
        Vector2 delta = endPos - _touchStartPos;

        if (debugSwipeLog)
            Debug.Log($"[Swipe] delta={delta} magnitude={delta.magnitude:F1} duration={duration:F3}s");

        if (duration > settings.MaxSwipeTime)   return;
        if (delta.magnitude < settings.MinSwipeDistance) return;

        SwipeDirection dir = ComputeDirection(delta);
        OnSwipeDetected(dir);
    }

    private static SwipeDirection ComputeDirection(Vector2 delta)
    {
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return delta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
        else
            return delta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
    }

    private void OnSwipeDetected(SwipeDirection dir)
    {
        GridCoord next = dir switch
        {
            SwipeDirection.Up    => _currentPlayerPos.Up(),
            SwipeDirection.Down  => _currentPlayerPos.Down(),
            SwipeDirection.Left  => _currentPlayerPos.Left(),
            SwipeDirection.Right => _currentPlayerPos.Right(),
            _                    => _currentPlayerPos
        };

        if (debugSwipeLog)
            Debug.Log($"[Swipe] dir={dir} from={_currentPlayerPos} to={next}");

        GameManager.Instance.TryMovePlayer(next);
    }

    // ── Public API (GameManager tarafından çağrılır) ──────────────────────────

    /// <summary>
    /// Oyuncunun mevcut grid pozisyonunu günceller.
    /// GameManager her başarılı hamlede veya undo'da çağırmalıdır.
    /// </summary>
    public void SetPlayerPosition(GridCoord coord)
    {
        _currentPlayerPos = coord;
    }

    /// <summary>Level yüklenirken veya reset sırasında çağrılır.</summary>
    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
        _tracking     = false;
#if UNITY_EDITOR
        _mouseTracking = false;
#endif
    }
}

// Kullanacak scriptler: GameManager (SetPlayerPosition, SetInputEnabled çağrısı)
