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

    private Vector2 _levelTextPosition;
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
        int targetRows = _counterPanel != null ? _counterPanel.PreferredRowCount : 1;
        float baseHeaderHeight = targetRows > 1 ? 322f : 268f;
        float minHeaderHeight = targetRows > 1 ? 290f : 244f;
        float maxHeaderHeight = targetRows > 1 ? 430f : 360f;
        float headerHeight = Mathf.Clamp(baseHeaderHeight * headerHeightScale, minHeaderHeight, maxHeaderHeight);
        float sideControlX = Mathf.Clamp(parentSize.x * 0.12f, 42f, 74f * positionScale);
        float mapY = -Mathf.Clamp(62f * positionScale, 52f, 86f);
        float timerY = -Mathf.Clamp(54f * positionScale, 46f, 76f);
        float hintY = -Mathf.Clamp(62f * positionScale, 52f, 86f);

        _rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, headerHeight);

        ApplyTopLeftChildScale(_mapButtonRect, new Vector2(sideControlX, mapY), iconScale);
        ApplyCenteredTimerLayout(_timerRect, timerY, iconScale);
        ApplyTopRightChildScale(_hintButtonRect, new Vector2(-sideControlX, hintY), iconScale);
        ApplyCounterPanelLayout(scale, positionScale, parentSize.x, targetRows);
        ApplyFloatingUndoLayout(_undoButtonRect, headerHeight, sideControlX, Mathf.Clamp(iconScale * 0.92f, 1f, 1.65f));
        ApplyChildScale(_levelTextRect, _levelTextPosition * positionScale, iconScale);
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

        _levelTextPosition = _levelTextRect != null ? _levelTextRect.anchoredPosition : Vector2.zero;
        _hasCapturedLayout = true;
    }

    private void ApplyCounterPanelLayout(float scale, float positionScale, float parentWidth, int targetRows)
    {
        if (_counterPanelRect == null)
            return;

        float horizontalInset = Mathf.Clamp(parentWidth * 0.06f, 18f, 42f * positionScale);
        float bottomPadding = Mathf.Clamp(18f * positionScale, 14f, 28f);
        float rowHeight = targetRows > 1
            ? Mathf.Clamp(168f * scale, 146f, 230f)
            : Mathf.Clamp(122f * scale, 106f, 168f);

        _counterPanelRect.anchorMin = new Vector2(0f, 0f);
        _counterPanelRect.anchorMax = new Vector2(1f, 0f);
        _counterPanelRect.pivot = new Vector2(0.5f, 0.5f);
        _counterPanelRect.anchoredPosition = new Vector2(0f, bottomPadding + (rowHeight * 0.5f));
        _counterPanelRect.sizeDelta = new Vector2(
            -(horizontalInset * 2f),
            rowHeight);
        _counterPanelRect.localScale = Vector3.one;
        _counterPanel?.ApplyResponsiveScale(Mathf.Lerp(1f, scale, 0.92f) * 1.22f);
    }

    private static void ApplyChildScale(RectTransform rect, Vector2 anchoredPosition, float scale)
    {
        if (rect == null)
            return;

        rect.anchoredPosition = anchoredPosition;
        rect.localScale = new Vector3(scale, scale, 1f);
    }

    private static void ApplyCenteredTimerLayout(RectTransform rect, float anchoredY, float scale)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, rect.anchorMin.y);
        rect.anchorMax = new Vector2(0.5f, rect.anchorMax.y);
        rect.pivot = new Vector2(0.5f, rect.pivot.y);
        rect.anchoredPosition = new Vector2(0f, anchoredY);
        rect.localScale = new Vector3(scale, scale, 1f);
    }

    private static void ApplyTopLeftChildScale(RectTransform rect, Vector2 anchoredPosition, float scale)
    {
        ApplyAnchoredChildScale(rect, Vector2.up, Vector2.up, new Vector2(0.5f, 0.5f), anchoredPosition, scale);
    }

    private static void ApplyTopRightChildScale(RectTransform rect, Vector2 anchoredPosition, float scale)
    {
        ApplyAnchoredChildScale(rect, Vector2.one, Vector2.one, new Vector2(0.5f, 0.5f), anchoredPosition, scale);
    }

    private static void ApplyFloatingUndoLayout(RectTransform rect, float headerHeight, float anchoredX, float scale)
    {
        if (rect == null)
            return;

        float baseHeight = rect.rect.height > 0f ? rect.rect.height : Mathf.Max(68f, rect.sizeDelta.y);
        float gap = Mathf.Clamp(22f * scale, 18f, 34f);
        float anchoredY = -(headerHeight + gap + (baseHeight * scale * 0.5f));
        ApplyAnchoredChildScale(rect, Vector2.up, Vector2.up, new Vector2(0.5f, 0.5f), new Vector2(anchoredX, anchoredY), scale);
    }

    private static void ApplyAnchoredChildScale(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, float scale)
    {
        if (rect == null)
            return;

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = new Vector3(scale, scale, 1f);
    }
}
