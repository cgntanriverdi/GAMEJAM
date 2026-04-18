using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class HintButton : MonoBehaviour
{
    private const float TriggerDebounceSeconds = 0.1f;

    [SerializeField] private HintManager _hintManager;

    private Button _button;
    private RectTransform _rectTransform;
    private bool _mousePressedInside;
    private int _trackedTouchId = -1;
    private float _lastTriggerTime = -10f;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _rectTransform = (RectTransform)transform;

        if (_hintManager == null)
            _hintManager = FindFirstObjectByType<HintManager>();

        _button.onClick.AddListener(HandleClick);
    }

    private void Update()
    {
        HandleMouseFallback();
        HandleTouchFallback();
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(HandleClick);
    }

    private void HandleClick()
    {
        if (!CanTrigger())
            return;

        _hintManager?.UseHint();
    }

    private bool CanTrigger()
    {
        if (_button == null || !_button.interactable || !isActiveAndEnabled)
            return false;

        if (Time.unscaledTime - _lastTriggerTime < TriggerDebounceSeconds)
            return false;

        _lastTriggerTime = Time.unscaledTime;
        return true;
    }

    private void HandleMouseFallback()
    {
        if (Input.touchCount > 0)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            _mousePressedInside = IsInside(Input.mousePosition);
            return;
        }

        if (!Input.GetMouseButtonUp(0))
            return;

        bool shouldTrigger = _mousePressedInside && IsInside(Input.mousePosition);
        _mousePressedInside = false;

        if (shouldTrigger)
            HandleClick();
    }

    private void HandleTouchFallback()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (touch.phase == TouchPhase.Began)
            {
                if (_trackedTouchId == -1 && IsInside(touch.position))
                    _trackedTouchId = touch.fingerId;

                continue;
            }

            if (touch.fingerId != _trackedTouchId)
                continue;

            if (touch.phase == TouchPhase.Ended)
            {
                bool shouldTrigger = IsInside(touch.position);
                _trackedTouchId = -1;

                if (shouldTrigger)
                    HandleClick();
            }
            else if (touch.phase == TouchPhase.Canceled)
            {
                _trackedTouchId = -1;
            }
        }
    }

    private bool IsInside(Vector2 screenPosition)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(_rectTransform, screenPosition, null);
    }
}
