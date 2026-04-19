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
    private Dictionary<CellColor, int> _targets;
    private Func<CellColor, Sprite> _spriteSource;
    private HorizontalLayoutGroup _layoutGroup;
    private float _baseSpacing = -1f;
    private float _responsiveScale = 1f;

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
        }
    }

    public void ApplyResponsiveScale(float scale)
    {
        CacheLayout();
        float clampedScale = Mathf.Clamp(scale, 1f, 1.9f);
        if (Mathf.Approximately(_responsiveScale, clampedScale))
            return;

        _responsiveScale = clampedScale;

        if (_layoutGroup != null)
            _layoutGroup.spacing = _baseSpacing * Mathf.Lerp(1f, _responsiveScale, 0.8f);

        foreach (var entry in _entries.Values)
            entry.ApplyResponsiveScale(_responsiveScale);
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
        if (_container == null)
            return;

        _layoutGroup ??= _container.GetComponent<HorizontalLayoutGroup>();
        if (_layoutGroup != null && _baseSpacing < 0f)
            _baseSpacing = _layoutGroup.spacing;
    }
}

// Kullanacak scriptler: GameManager (SetSpriteSource, Initialize, Refresh, TriggerOverflowFeedback,
//                                    TriggerIncompleteEndFeedback, SetAllComplete)
