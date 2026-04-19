using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Alt paneli yönetir: hedef renk sayılarını gösterir, her adımda günceller.
/// Layout (Plan §7.11): 🔴 3/4  🔵 2/2✅  🟢 1/3  🟡 0/1
/// </summary>
public class CounterPanelUI : MonoBehaviour
{
    [Header("Prefab & container")]
    [SerializeField] private ColorCountEntry  _entryPrefab;
    [SerializeField] private Transform        _container;

    // Renk → entry eşlemesi; Initialize sonrası dolu
    private readonly Dictionary<CellColor, ColorCountEntry> _entries = new();
    private readonly List<ColorCountEntry> _entryOrder = new();
    private Dictionary<CellColor, int> _targets;
    private Func<CellColor, Sprite> _spriteSource;
    private RectTransform _rectTransform;
    private RectTransform _containerRect;
    private HorizontalLayoutGroup _layoutGroup;
    private float _responsiveScale = 1f;

    public int EntryCount => _entryOrder.Count;
    public int PreferredRowCount => GetPreferredRowCount(_entryOrder.Count);

    // ── Setup ─────────────────────────────────────────────────────────────────

    public void SetSpriteSource(Func<CellColor, Sprite> source) => _spriteSource = source;

    /// <summary>
    /// Level başında GameManager tarafından çağrılır.
    /// Mevcut entry'leri temizler, aktif renkler için yenilerini oluşturur.
    /// </summary>
    public void Initialize(Dictionary<CellColor, int> targets)
    {
        _targets = targets;
        CacheLayout();

        // Önceki entry'leri temizle
        foreach (Transform child in _container)
            Destroy(child.gameObject);
        _entries.Clear();
        _entryOrder.Clear();

        // Renk sayısına göre spacing ve scale hesapla
        float countScale   = CountScaleForEntries(targets.Count);
        float countSpacing = CountSpacingForEntries(targets.Count);
        if (_layoutGroup != null)
            _layoutGroup.spacing = countSpacing;

        // Her aktif renk için bir entry oluştur
        foreach (var kvp in targets)
        {
            var entry = Instantiate(_entryPrefab, _container);
            entry.Initialize(kvp.Key, _spriteSource?.Invoke(kvp.Key));
            entry.ApplyResponsiveScale(countScale);
            entry.SetCount(0, kvp.Value);
            _entries[kvp.Key] = entry;
            _entryOrder.Add(entry);
        }
    }

    public void ApplyResponsiveScale(float scale)
    {
        CacheLayout();
        _responsiveScale = CalculateFittingScale(scale);

        foreach (var entry in _entryOrder)
            entry.ApplyResponsiveScale(_responsiveScale);

        ApplyManualLayout();
    }

    // ── Per-step update ───────────────────────────────────────────────────────

    /// <summary>
    /// Her başarılı hamlede ve undo'dan sonra GameManager tarafından çağrılır.
    /// </summary>
    public void Refresh(Dictionary<CellColor, int> currentCounts)
    {
        foreach (var kvp in _targets)
        {
            if (!_entries.TryGetValue(kvp.Key, out var entry)) continue;
            currentCounts.TryGetValue(kvp.Key, out int current);
            entry.SetCount(current, kvp.Value);
        }
    }

    // ── Feedback ──────────────────────────────────────────────────────────────

    /// <summary>
    /// MoveOutcome.InvalidColorOverflow durumunda ilgili entry'yi shake eder.
    /// GameManager, hangi rengin taştığını hesaplar ve buraya iletir.
    /// </summary>
    public void TriggerOverflowFeedback(CellColor color)
    {
        if (_entries.TryGetValue(color, out var entry))
            entry.TriggerOverflowFeedback();
    }

    /// <summary>
    /// EndReachedButIncomplete: oyuncu end cell'e geldi ama sayılar tutmuyor.
    /// Eksik kalan tüm renkleri kısa süre flash'la — "henüz değil" sinyali.
    /// </summary>
    public void TriggerIncompleteEndFeedback(Dictionary<CellColor, int> currentCounts)
    {
        foreach (var kvp in _targets)
        {
            currentCounts.TryGetValue(kvp.Key, out int current);
            if (current < kvp.Value && _entries.TryGetValue(kvp.Key, out var entry))
                entry.TriggerOverflowFeedback();  // aynı shake, renk zaten doğru
        }
    }

    /// <summary>Win durumunda tüm entry'leri "complete" haline getirir.</summary>
    public void SetAllComplete()
    {
        foreach (var kvp in _entries)
        {
            if (_targets.TryGetValue(kvp.Key, out int target))
                kvp.Value.SetCount(target, target);
        }
    }

    // 2 renk → normal boyut; 3 → biraz küçük; 4 → daha küçük
    private static float CountScaleForEntries(int count) => count switch
    {
        <= 2 => 1.00f,
        3    => 0.82f,
        _    => 0.70f,
    };

    private static float CountSpacingForEntries(int count) => count switch
    {
        <= 2 => 12f,
        3    =>  6f,
        _    =>  3f,
    };

    private void CacheLayout()
    {
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();

        if (_container == null)
            return;

        if (_containerRect == null)
            _containerRect = _container as RectTransform;

        _layoutGroup ??= _container.GetComponent<HorizontalLayoutGroup>();
        if (_layoutGroup != null)
            _layoutGroup.enabled = false;

        if (_containerRect == null)
            return;

        _containerRect.anchorMin = Vector2.zero;
        _containerRect.anchorMax = Vector2.one;
        _containerRect.pivot = new Vector2(0.5f, 0.5f);
        _containerRect.anchoredPosition = Vector2.zero;
        _containerRect.sizeDelta = Vector2.zero;
    }

    private float CalculateFittingScale(float desiredScale)
    {
        float clampedScale = Mathf.Clamp(desiredScale, 0.9f, 1.95f);
        if (_rectTransform == null || _entryPrefab == null || _entryOrder.Count == 0)
            return clampedScale;

        float availableWidth = _rectTransform.rect.width;
        float availableHeight = _rectTransform.rect.height;
        if (availableWidth <= 0f || availableHeight <= 0f)
            return clampedScale;

        RectTransform entryPrefabRect = _entryPrefab.GetComponent<RectTransform>();
        float entryWidth = entryPrefabRect != null ? entryPrefabRect.sizeDelta.x : 80f;
        float entryHeight = entryPrefabRect != null ? entryPrefabRect.sizeDelta.y : 72f;
        int entryCount = _entryOrder.Count;
        int rows = GetPreferredRowCount(entryCount);
        int columns = Mathf.CeilToInt(entryCount / (float)rows);
        float spacingX = CalculateSpacingX(entryCount, clampedScale);
        float spacingY = Mathf.Clamp(10f * clampedScale, 8f, 18f);
        float maxByWidth = (availableWidth - (spacingX * Mathf.Max(0, columns - 1))) / Mathf.Max(1f, entryWidth * columns);
        float maxByHeight = (availableHeight - (spacingY * Mathf.Max(0, rows - 1))) / Mathf.Max(1f, entryHeight * rows);
        float fittingScale = Mathf.Min(clampedScale, Mathf.Min(maxByWidth, maxByHeight));

        return Mathf.Clamp(fittingScale, 0.78f, clampedScale);
    }

    private void ApplyManualLayout()
    {
        if (_containerRect == null || _entryPrefab == null || _entryOrder.Count == 0)
            return;

        RectTransform entryPrefabRect = _entryPrefab.GetComponent<RectTransform>();
        float entryWidth = (entryPrefabRect != null ? entryPrefabRect.sizeDelta.x : 80f) * _responsiveScale;
        float entryHeight = (entryPrefabRect != null ? entryPrefabRect.sizeDelta.y : 72f) * _responsiveScale;
        int entryCount = _entryOrder.Count;
        int rows = GetPreferredRowCount(entryCount);
        int columns = Mathf.CeilToInt(entryCount / (float)rows);
        float spacingX = CalculateSpacingX(entryCount, _responsiveScale);
        float spacingY = Mathf.Clamp(10f * _responsiveScale, 8f, 18f);
        float totalHeight = (rows * entryHeight) + (Mathf.Max(0, rows - 1) * spacingY);
        float firstRowY = (totalHeight * 0.5f) - (entryHeight * 0.5f);

        for (int i = 0; i < entryCount; i++)
        {
            int row = Mathf.FloorToInt(i / (float)columns);
            int column = i % columns;
            int rowEntryCount = Mathf.Min(columns, entryCount - (row * columns));
            float totalRowWidth = (rowEntryCount * entryWidth) + (Mathf.Max(0, rowEntryCount - 1) * spacingX);
            float firstColumnX = -(totalRowWidth * 0.5f) + (entryWidth * 0.5f);
            Vector2 anchoredPosition = new(
                firstColumnX + (column * (entryWidth + spacingX)),
                firstRowY - (row * (entryHeight + spacingY)));

            RectTransform entryRect = _entryOrder[i].GetComponent<RectTransform>();
            entryRect.anchorMin = new Vector2(0.5f, 0.5f);
            entryRect.anchorMax = new Vector2(0.5f, 0.5f);
            entryRect.pivot = new Vector2(0.5f, 0.5f);
            entryRect.anchoredPosition = anchoredPosition;
        }
    }

    private static float CalculateSpacingX(int entryCount, float scale)
    {
        return entryCount <= 2
            ? Mathf.Clamp(62f * scale, 46f, 96f)
            : Mathf.Clamp(34f * scale, 26f, 64f);
    }

    private static int GetPreferredRowCount(int entryCount) => entryCount >= 4 ? 2 : 1;
}

// Kullanacak scriptler: GameManager (SetSpriteSource, Initialize, Refresh, TriggerOverflowFeedback,
//                                    TriggerIncompleteEndFeedback, SetAllComplete)
