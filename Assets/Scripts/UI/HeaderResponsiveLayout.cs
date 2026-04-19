using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public sealed class HeaderResponsiveLayout : MonoBehaviour
{
    [SerializeField] private Vector2 _referencePlayAreaSize = new(404f, 724f);
    [SerializeField] private float _referenceHeaderHeight = 176f;
    [SerializeField] private float _minScale = 0.9f;
    [SerializeField] private float _maxScale = 1.35f;

    private RectTransform _rectTransform;
    private RectTransform _parentRectTransform;
    private RectTransform _mapButtonRect;
    private RectTransform _timerRect;
    private RectTransform _counterPanelRect;
    private RectTransform _levelTextRect;
    private RectTransform _undoButtonRect;
    private RectTransform _hintButtonRect;
    private CounterPanelUI _counterPanel;

    private Vector2 _mapButtonPosition;
    private Vector2 _timerPosition;
    private Vector2 _counterPanelPosition;
    private Vector2 _levelTextPosition;
    private Vector2 _undoButtonPosition;
    private Vector2 _hintButtonPosition;
    private Vector2 _counterPanelSize;
    private bool _hasCapturedLayout;

    private void OnEnable()
    {
        ApplyLayout();
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplyLayout();
    }

    private void LateUpdate()
    {
        ApplyLayout();
    }

    private void ApplyLayout()
    {
        if (!TryCacheTransforms())
            return;

        CaptureBaselineIfNeeded();

        Vector2 parentSize = _parentRectTransform.rect.size;
        if (parentSize.x <= 0f || parentSize.y <= 0f)
            return;

        Vector2 referenceSize = new(
            Mathf.Min(_referencePlayAreaSize.x, 340f),
            Mathf.Min(_referencePlayAreaSize.y, 620f));
        float widthScale = parentSize.x / referenceSize.x;
        float heightScale = parentSize.y / referenceSize.y;
        float scale = Mathf.Lerp(heightScale, widthScale, 0.62f);
        scale = Mathf.Clamp(scale, Mathf.Max(_minScale, 1f), Mathf.Max(_maxScale, 1.85f));

        float positionScale = Mathf.Lerp(1f, scale, 0.75f);
        float headerHeightScale = Mathf.Lerp(1f, scale, 0.92f);
        float iconScale = Mathf.Clamp(scale * 1.04f, 1f, 1.95f);

        _rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, _referenceHeaderHeight * headerHeightScale);

        ApplyChildScale(_mapButtonRect, _mapButtonPosition * positionScale, iconScale);
        ApplyChildScale(_timerRect, _timerPosition * positionScale, iconScale);
        ApplyCounterPanelLayout(scale, positionScale);
        ApplyChildScale(_levelTextRect, _levelTextPosition * positionScale, iconScale);
        ApplyChildScale(_undoButtonRect, _undoButtonPosition * positionScale, iconScale);
        ApplyChildScale(_hintButtonRect, _hintButtonPosition * positionScale, iconScale);
    }

    private bool TryCacheTransforms()
    {
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();

        if (_rectTransform != null && _parentRectTransform == null)
            _parentRectTransform = _rectTransform.parent as RectTransform;

        _mapButtonRect ??= FindChildRect("LevelMapButton");
        _timerRect ??= FindChildRect("TimerText");
        _counterPanelRect ??= FindChildRect("CounterPanel");
        _levelTextRect ??= FindChildRect("LevelText");
        _undoButtonRect ??= FindChildRect("UndoButton");
        _hintButtonRect ??= FindChildRect("HintButton");
        _counterPanel ??= _counterPanelRect != null ? _counterPanelRect.GetComponent<CounterPanelUI>() : null;

        return _rectTransform != null && _parentRectTransform != null;
    }

    private RectTransform FindChildRect(string childName)
    {
        Transform child = transform.Find(childName);
        return child as RectTransform;
    }

    private void CaptureBaselineIfNeeded()
    {
        if (_hasCapturedLayout)
            return;

        _mapButtonPosition = _mapButtonRect != null ? _mapButtonRect.anchoredPosition : Vector2.zero;
        _timerPosition = _timerRect != null ? _timerRect.anchoredPosition : Vector2.zero;
        _counterPanelPosition = _counterPanelRect != null ? _counterPanelRect.anchoredPosition : Vector2.zero;
        _counterPanelSize = _counterPanelRect != null ? _counterPanelRect.sizeDelta : Vector2.zero;
        _levelTextPosition = _levelTextRect != null ? _levelTextRect.anchoredPosition : Vector2.zero;
        _undoButtonPosition = _undoButtonRect != null ? _undoButtonRect.anchoredPosition : Vector2.zero;
        _hintButtonPosition = _hintButtonRect != null ? _hintButtonRect.anchoredPosition : Vector2.zero;
        _hasCapturedLayout = true;
    }

    private void ApplyCounterPanelLayout(float scale, float positionScale)
    {
        if (_counterPanelRect == null)
            return;

        _counterPanelRect.anchoredPosition = _counterPanelPosition * positionScale;
        _counterPanelRect.sizeDelta = new Vector2(
            _counterPanelSize.x,
            _counterPanelSize.y * Mathf.Lerp(1f, scale, 0.95f));
        _counterPanelRect.localScale = Vector3.one;
        _counterPanel?.ApplyResponsiveScale(Mathf.Lerp(1f, scale, 0.92f));
    }

    private static void ApplyChildScale(RectTransform rect, Vector2 anchoredPosition, float scale)
    {
        if (rect == null)
            return;

        rect.anchoredPosition = anchoredPosition;
        rect.localScale = new Vector3(scale, scale, 1f);
    }
}
