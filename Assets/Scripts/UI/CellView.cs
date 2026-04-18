using UnityEngine;

// Highlight durumları — GridManager tarafından kullanılır.
public enum HighlightState { None, Selected, Reachable, Dimmed }

/// <summary>
/// Bir grid hücresinin görsel bileşeni.
/// STUB — UI sistemi implementasyonu sırasında doldurulacak.
/// GridManager bu bileşeni sürüyor; görsel mantık buraya ait.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class CellView : MonoBehaviour
{
    private SpriteRenderer _sr;
    private CellData _data;

    public void Initialize(CellData data)
    {
        _sr   = GetComponent<SpriteRenderer>();
        _data = data;
        SetColor(data.Color);
    }

    public void SetColor(CellColor color)
    {
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        _sr.color = ColorForCell(color);
    }

    public void SetHighlight(HighlightState state)
    {
        // TODO: yönsel ok ikonlarını ve parlama efektini buraya ekle (7.6.3)
    }

    private static Color ColorForCell(CellColor c) => c switch
    {
        CellColor.Red    => new Color(0.9f, 0.2f, 0.2f),
        CellColor.Blue   => new Color(0.2f, 0.4f, 0.9f),
        CellColor.Green  => new Color(0.2f, 0.8f, 0.3f),
        CellColor.Yellow => new Color(0.95f, 0.8f, 0.1f),
        _                => Color.white
    };
}

// Kullanacak scriptler: GridManager (Initialize, SetColor, SetHighlight)
