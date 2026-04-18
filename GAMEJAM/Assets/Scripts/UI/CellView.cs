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

    private SpriteRenderer _sr;
    private SpriteRenderer _overlaySr;
    private CellData _data;

    public void Initialize(CellData data)
    {
        _sr   = GetComponent<SpriteRenderer>();
        _data = data;
        SetColor(data.Color);
    }

    public void ShowOverlay(Sprite sprite)
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
    }

    public void SetColor(CellColor color)
    {
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        _sr.sprite = SpriteForCell(color);
    }

    public void SetHighlight(HighlightState state)
    {
        // TODO: yönsel ok ikonlarını ve parlama efektini buraya ekle (7.6.3)
    }

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
