using System;
using System.Collections.Generic;
using UnityEngine;

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

    // ── Setup ─────────────────────────────────────────────────────────────────

    public void SetSpriteSource(Func<CellColor, Sprite> source) => _spriteSource = source;

    /// <summary>
    /// Level başında GameManager tarafından çağrılır.
    /// Mevcut entry'leri temizler, aktif renkler için yenilerini oluşturur.
    /// </summary>
    public void Initialize(Dictionary<CellColor, int> targets)
    {
        _targets = targets;

        // Önceki entry'leri temizle
        foreach (Transform child in _container)
            Destroy(child.gameObject);
        _entries.Clear();

        // Her aktif renk için bir entry oluştur
        foreach (var kvp in targets)
        {
            var entry = Instantiate(_entryPrefab, _container);
            entry.Initialize(kvp.Key, _spriteSource?.Invoke(kvp.Key));
            entry.SetCount(0, kvp.Value);
            _entries[kvp.Key] = entry;
        }
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
}

// Kullanacak scriptler: GameManager (SetSpriteSource, Initialize, Refresh, TriggerOverflowFeedback,
//                                    TriggerIncompleteEndFeedback, SetAllComplete)
