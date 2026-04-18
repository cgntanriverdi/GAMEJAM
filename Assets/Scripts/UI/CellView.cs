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
    private CellData _data;

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
        if (greySprite != null) { _sr.sprite = greySprite; _sr.color = Color.white; }
        else                    { _sr.color  = Color.gray; }
    }

    public void SetAsEnd()
    {
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        if (purpleSprite != null) { _sr.sprite = purpleSprite; _sr.color = Color.white; }
        else                      { _sr.color  = new Color(0.6f, 0f, 0.9f); }
    }

    public void SetHighlight(HighlightState state)
    {
        // TODO: yönsel ok ikonlarını ve parlama efektini buraya ekle (7.6.3)
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
