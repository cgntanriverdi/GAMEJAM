using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CounterPanel'daki tek bir renk satırını yönetir: 🔴 2/4 veya 🔴 4/4 ✓
/// CounterPanelUI tarafından instantiate edilir ve sürülür.
/// </summary>
public class ColorCountEntry : MonoBehaviour
{
    [SerializeField] private Image            _colorIcon;
    [SerializeField] private TextMeshProUGUI  _countText;
    [SerializeField] private GameObject       _checkmark;   // hedef tamamlandığında aktif

    private CellColor     _color;
    private RectTransform _rect;

    private static readonly UnityEngine.Color NormalTextColor = UnityEngine.Color.white;
    private static readonly UnityEngine.Color OverflowTextColor = new UnityEngine.Color(1f, 0.22f, 0.22f);
    private static readonly UnityEngine.Color CompleteTextColor = new UnityEngine.Color(0.3f, 1f, 0.45f);

    // ── Setup ─────────────────────────────────────────────────────────────────

    public void Initialize(CellColor color, Sprite cellSprite = null)
    {
        _color = color;
        _rect  = GetComponent<RectTransform>();
        if (cellSprite != null)
        {
            _colorIcon.sprite = cellSprite;
            _colorIcon.color  = Color.white;
        }
        else
        {
            _colorIcon.color = ColorForCell(color);
        }
        if (_checkmark) _checkmark.SetActive(false);
        SetCount(0, 1);
    }

    // ── State ─────────────────────────────────────────────────────────────────

    /// <summary>CounterPanelUI.Refresh() tarafından her adımda çağrılır.</summary>
    public void SetCount(int current, int target)
    {
        bool complete  = current == target;
        bool overflow  = current > target;

        _countText.text  = $"{current}/{target}";
        _countText.color = overflow  ? OverflowTextColor
                         : complete  ? CompleteTextColor
                         : NormalTextColor;

        if (_checkmark) _checkmark.SetActive(complete);
    }

    // ── Feedback ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Aşım hata sinyali: kırmızı flash + yatay shake.
    /// Plan §7.11 — GameManager.TryMovePlayer → InvalidColorOverflow.
    /// </summary>
    public void TriggerOverflowFeedback()
    {
        StopAllCoroutines();
        StartCoroutine(ShakeAndFlash());
    }

    private IEnumerator ShakeAndFlash()
    {
        Vector2 origin    = _rect.anchoredPosition;
        float   elapsed   = 0f;
        float   duration  = 0.35f;
        float   magnitude = 8f;
        float   speed     = 40f;

        _colorIcon.color = OverflowTextColor;

        while (elapsed < duration)
        {
            float offsetX = Mathf.Sin(elapsed * speed) * magnitude * (1f - elapsed / duration);
            _rect.anchoredPosition = origin + new Vector2(offsetX, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _rect.anchoredPosition = origin;
        _colorIcon.color       = _colorIcon.sprite != null ? Color.white : ColorForCell(_color);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public CellColor EntryColor => _color;

    private static UnityEngine.Color ColorForCell(CellColor c) => c switch
    {
        CellColor.Red    => new UnityEngine.Color(0.9f,  0.22f, 0.22f),
        CellColor.Blue   => new UnityEngine.Color(0.22f, 0.45f, 0.9f),
        CellColor.Green  => new UnityEngine.Color(0.22f, 0.82f, 0.35f),
        CellColor.Yellow => new UnityEngine.Color(0.95f, 0.82f, 0.12f),
        _                => UnityEngine.Color.white
    };
}

// Kullanacak scriptler: CounterPanelUI (Initialize, SetCount, TriggerOverflowFeedback)
