using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public sealed class PlayAreaRootLayout : MonoBehaviour
{
    [SerializeField] private float _playAreaAspect = 1536f / 2752f;

    private RectTransform _rectTransform;
    private RectTransform _parentRectTransform;

    private void OnEnable()
    {
        ApplyLayout();
    }

    private void OnValidate()
    {
        ApplyLayout();
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplyLayout();
    }

    private void ApplyLayout()
    {
        if (!TryCacheTransforms())
            return;

        Rect safeArea = GetSafeAreaInParentSpace();
        if (safeArea.width <= 0f || safeArea.height <= 0f)
            return;

        float safeAspect = safeArea.width / safeArea.height;
        float targetHeight = safeAspect > _playAreaAspect
            ? safeArea.height
            : safeArea.width / _playAreaAspect;
        float targetWidth = targetHeight * _playAreaAspect;

        _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _rectTransform.anchoredPosition = safeArea.center;
        _rectTransform.sizeDelta = new Vector2(targetWidth, targetHeight);
    }

    private bool TryCacheTransforms()
    {
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();

        if (_rectTransform != null && _parentRectTransform == null)
            _parentRectTransform = _rectTransform.parent as RectTransform;

        return _rectTransform != null && _parentRectTransform != null;
    }

    private Rect GetSafeAreaInParentSpace()
    {
        Rect parentRect = _parentRectTransform.rect;
        if (parentRect.width <= 0f || parentRect.height <= 0f)
            return default;

        float screenWidth = Mathf.Max(1f, Screen.width);
        float screenHeight = Mathf.Max(1f, Screen.height);
        Rect safeArea = Screen.safeArea;

        float xScale = parentRect.width / screenWidth;
        float yScale = parentRect.height / screenHeight;

        float safeWidth = safeArea.width * xScale;
        float safeHeight = safeArea.height * yScale;
        float safeCenterX = ((safeArea.x + safeArea.width * 0.5f) - (screenWidth * 0.5f)) * xScale;
        float safeCenterY = ((safeArea.y + safeArea.height * 0.5f) - (screenHeight * 0.5f)) * yScale;

        return new Rect(
            safeCenterX - (safeWidth * 0.5f),
            safeCenterY - (safeHeight * 0.5f),
            safeWidth,
            safeHeight);
    }
}
