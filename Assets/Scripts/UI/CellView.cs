using System.Collections;
using UnityEngine;

// Highlight durumları — GridManager tarafından kullanılır.
public enum HighlightState { None, Selected, Reachable, Dimmed, Hint }

[RequireComponent(typeof(SpriteRenderer))]
public class CellView : MonoBehaviour
{
    [SerializeField] private Sprite redSprite;
    [SerializeField] private Sprite blueSprite;
    [SerializeField] private Sprite greenSprite;
    [SerializeField] private Sprite yellowSprite;
    [SerializeField] private Sprite greySprite;
    [SerializeField] private Sprite purpleSprite;

    private SpriteRenderer _sr;
    private SpriteRenderer _overlaySr;
    private SpriteRenderer _prisonOverlaySr;
    private CellData _data;

    private Coroutine _hintPulse;
    private Vector3   _baseScale;

    public void Initialize(CellData data)
    {
        _sr   = GetComponent<SpriteRenderer>();
        _data = data;
        SetColor(data.Color);
    }

    public void ShowOverlay(Sprite sprite, float worldSize = 1f)
    {
        if (_overlaySr == null)
        {
            var child = new GameObject("Overlay");
            child.transform.SetParent(transform, false);
            _overlaySr = child.AddComponent<SpriteRenderer>();
            _overlaySr.sortingLayerName = _sr.sortingLayerName;
            _overlaySr.sortingOrder     = _sr.sortingOrder + 1;
        }
        _overlaySr.sprite = sprite;

        // Sprite'ın piksel boyutundan bağımsız olarak worldSize kadar görünsün.
        // Parent'ın scale'i cellSize olduğu için tersine normalize et.
        if (sprite != null && worldSize > 0f)
        {
            float ppu        = sprite.pixelsPerUnit;
            float spriteSize = sprite.rect.width / ppu; // sprite'ın 1x'teki dünya boyutu
            float parentScale = transform.lossyScale.x;
            float targetLocal = parentScale > 0f ? (worldSize / spriteSize) / parentScale : 1f;
            _overlaySr.transform.localScale = new Vector3(targetLocal, targetLocal, 1f);
        }
    }

    public void SetColor(CellColor color)
    {
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        _sr.color  = Color.white;
        _sr.sprite = SpriteForCell(color);
    }

    public void SetAsStart()
    {
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        if (purpleSprite != null) { _sr.sprite = purpleSprite; _sr.color = Color.white; }
        else                      { _sr.color  = new Color(0.6f, 0f, 0.9f); }
    }

    public void SetAsEnd()
    {
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        if (purpleSprite != null) { _sr.sprite = purpleSprite; _sr.color = Color.white; }
        else                      { _sr.color  = new Color(0.6f, 0f, 0.9f); }
    }

    /// <summary>End hücresinin arka planını gizler; sadece overlay'ler görünür kalır.</summary>
    public void ClearBackground()
    {
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        _sr.sprite = null;
        _sr.color  = Color.clear;
    }

    /// <summary>
    /// End hücresinin üstüne kafes görselini yerleştirir (sortingOrder +2).
    /// Hücreyi tam kaplayacak şekilde ölçeklenir.
    /// </summary>
    public void ShowPrisonOverlay(Sprite sprite, float worldSize = 1f)
    {
        if (_prisonOverlaySr == null)
        {
            var child = new GameObject("PrisonOverlay");
            child.transform.SetParent(transform, false);
            _prisonOverlaySr = child.AddComponent<SpriteRenderer>();
            _prisonOverlaySr.sortingLayerName = _sr.sortingLayerName;
            _prisonOverlaySr.sortingOrder     = _sr.sortingOrder + 2;
        }
        _prisonOverlaySr.sprite = sprite;

        if (sprite != null && worldSize > 0f)
        {
            float ppu         = sprite.pixelsPerUnit;
            float spriteW     = sprite.rect.width  / ppu;
            float spriteH     = sprite.rect.height / ppu;
            float parentScale = transform.lossyScale.x;
            // Hücreyi tam kaplayacak en büyük scale'i seç
            float scaleX      = parentScale > 0f ? (worldSize / spriteW) / parentScale : 1f;
            float scaleY      = parentScale > 0f ? (worldSize / spriteH) / parentScale : 1f;
            float targetLocal = Mathf.Max(scaleX, scaleY);
            _prisonOverlaySr.transform.localScale = new Vector3(targetLocal, targetLocal, 1f);
        }
    }

    public void SetPrisonOverlayVisible(bool visible)
    {
        if (_prisonOverlaySr != null)
            _prisonOverlaySr.color = visible ? Color.white : Color.clear;
    }

    public void SetOverlayColor(Color color)
    {
        if (_overlaySr != null)
            _overlaySr.color = color;
    }

    public void SetHighlight(HighlightState state)
    {
        // TODO: yönsel ok ikonlarını ve parlama efektini buraya ekle (7.6.3)
    }

    public void StartHintPulse()
    {
        StopHintPulse();
        _baseScale = transform.localScale;
        _hintPulse = StartCoroutine(PulseRoutine());
    }

    public void StopHintPulse()
    {
        if (_hintPulse != null)
        {
            StopCoroutine(_hintPulse);
            _hintPulse = null;
        }
        if (_baseScale != Vector3.zero)
            transform.localScale = _baseScale;
    }

    private IEnumerator PulseRoutine()
    {
        float elapsed = 0f;
        const float speed = 6f;      // ~1 tam salınım / saniye
        const float amplitude = 0.12f;
        while (true)
        {
            float factor = 1f + amplitude * Mathf.Sin(elapsed * speed);
            transform.localScale = _baseScale * factor;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public Sprite GetSprite(CellColor color) => SpriteForCell(color);

    private Sprite SpriteForCell(CellColor c) => c switch
    {
        CellColor.Red    => redSprite,
        CellColor.Blue   => blueSprite,
        CellColor.Green  => greenSprite,
        CellColor.Yellow => yellowSprite,
        _                => null
    };
}

// Kullanacak scriptler: GridManager (Initialize, SetColor, SetHighlight)
