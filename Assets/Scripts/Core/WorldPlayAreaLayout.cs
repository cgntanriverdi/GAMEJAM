using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class WorldPlayAreaLayout : MonoBehaviour
{
    [SerializeField] private Camera _targetCamera;
    [SerializeField] private float _playAreaAspect = 1536f / 2752f;

    private SpriteRenderer _spriteRenderer;
    private Vector2Int _lastScreenSize;
    private Rect _lastSafeArea;
    private float _lastCameraAspect;
    private float _lastOrthographicSize;

    private void OnEnable()
    {
        ApplyLayout();
    }

    private void OnValidate()
    {
        ApplyLayout();
    }

    private void LateUpdate()
    {
        if (!HasLayoutChanged())
            return;

        ApplyLayout();
    }

    private bool HasLayoutChanged()
    {
        if (_targetCamera == null || !_targetCamera.orthographic)
            return false;

        Vector2Int currentScreenSize = new(Screen.width, Screen.height);
        Rect currentSafeArea = Screen.safeArea;

        return currentScreenSize != _lastScreenSize
            || currentSafeArea != _lastSafeArea
            || !Mathf.Approximately(_lastCameraAspect, _targetCamera.aspect)
            || !Mathf.Approximately(_lastOrthographicSize, _targetCamera.orthographicSize);
    }

    private void ApplyLayout()
    {
        _spriteRenderer ??= GetComponent<SpriteRenderer>();
        if (_targetCamera == null || _spriteRenderer == null || _spriteRenderer.sprite == null || !_targetCamera.orthographic)
            return;

        Rect safeArea = Screen.safeArea;
        float screenWidth = Mathf.Max(1f, Screen.width);
        float screenHeight = Mathf.Max(1f, Screen.height);

        float fullWorldHeight = _targetCamera.orthographicSize * 2f;
        float fullWorldWidth = fullWorldHeight * _targetCamera.aspect;
        float safeWorldWidth = fullWorldWidth * (safeArea.width / screenWidth);
        float safeWorldHeight = fullWorldHeight * (safeArea.height / screenHeight);

        float safeAspect = safeWorldWidth / safeWorldHeight;
        float targetHeight = safeAspect > _playAreaAspect
            ? safeWorldHeight
            : safeWorldWidth / _playAreaAspect;

        float targetWidth = targetHeight * _playAreaAspect;
        Vector2 targetCenter = new(
            _targetCamera.transform.position.x + (((safeArea.x + safeArea.width * 0.5f) / screenWidth) - 0.5f) * fullWorldWidth,
            _targetCamera.transform.position.y + (((safeArea.y + safeArea.height * 0.5f) / screenHeight) - 0.5f) * fullWorldHeight);

        Vector2 spriteSize = _spriteRenderer.sprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
            return;

        float scale = Mathf.Min(targetWidth / spriteSize.x, targetHeight / spriteSize.y);
        transform.position = new Vector3(targetCenter.x, targetCenter.y, transform.position.z);
        transform.localScale = new Vector3(scale, scale, 1f);

        _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        _lastSafeArea = safeArea;
        _lastCameraAspect = _targetCamera.aspect;
        _lastOrthographicSize = _targetCamera.orthographicSize;
    }
}
