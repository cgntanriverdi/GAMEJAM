using System.Collections.Generic;
using UnityEngine;

// ── Result types ─────────────────────────────────────────────────────────────

public enum MoveOutcome
{
    Valid,               // hamle geçerli, oyun devam ediyor
    Win,                 // end hücresine ulaşıldı ve tüm renkler hedefte
    InvalidNotNeighbor,  // hedef hücre mevcut pozisyona komşu değil
    InvalidAlreadyVisited, // hedef hücre seçili yolda zaten var
    InvalidColorOverflow,  // bu adım bir rengi hedefin üstüne çıkarıyor
    EndReachedButIncomplete, // end'e gelindi ama renk sayıları tutmuyor
    InvalidCheckpointLocked, // undo bu indeksin gerisine geçemez
}

public readonly struct ValidationResult
{
    public readonly MoveOutcome Outcome;
    /// <summary>Valid veya Win durumunda hamle sonrası beklenen sayaçlar.</summary>
    public readonly Dictionary<CellColor, int> ProjectedCounts;

    public bool IsAccepted => Outcome == MoveOutcome.Valid || Outcome == MoveOutcome.Win;

    public ValidationResult(MoveOutcome outcome, Dictionary<CellColor, int> projected = null)
    {
        Outcome        = outcome;
        ProjectedCounts = projected;
    }
}

// ── Validator ─────────────────────────────────────────────────────────────────

/// <summary>
/// Her hamleyi Plan §7.9'daki beş kurala göre sırayla denetler.
/// MonoBehaviour değil — GameManager new'leyerek veya field olarak tutar.
/// </summary>
public class RunValidator
{
    private readonly GridManager _grid;

    public RunValidator(GridManager grid)
    {
        _grid = grid;
    }

    // ── Forward move ──────────────────────────────────────────────────────────

    /// <summary>
    /// Oyuncunun 'target' hücresine ilerlemesinin geçerli olup olmadığını kontrol eder.
    /// Plan §7.9 sırası: komşuluk → ziyaret → renk taşması → bitiş.
    /// </summary>
    public ValidationResult ValidateMove(
        GridCoord       target,
        PlayerRunState  state,
        PathSolution    solution)
    {
        GridCoord current = CurrentPosition(state);

        // 1. Komşuluk: target, current'ın 4-bağlantılı komşusu mu?
        if (!IsNeighbor(current, target))
            return new ValidationResult(MoveOutcome.InvalidNotNeighbor);

        // 2. Ziyaret: target daha önce seçildi mi?
        if (state.SelectedPath.Contains(target))
            return new ValidationResult(MoveOutcome.InvalidAlreadyVisited);

        // 3. Renk taşması: bu hücreyi ekleyince herhangi bir renk hedefi aşıyor mu?
        CellData cell = _grid.GetCell(target);
        if (cell == null)
        {
            Debug.LogWarning($"[RunValidator] GetCell null döndü: {target}");
            return new ValidationResult(MoveOutcome.InvalidNotNeighbor);
        }

        // 4. End cell'e ulaşıldı mı? — end hücresinin rengi sayılmaz
        if (cell.IsEnd)
        {
            bool allMatch = CountsMatch(state.CurrentColorCounts, solution.TargetColorCounts);
            var outcome   = allMatch ? MoveOutcome.Win : MoveOutcome.EndReachedButIncomplete;
            return new ValidationResult(outcome, state.CurrentColorCounts);
        }

        var projected = ProjectCounts(state.CurrentColorCounts, cell.Color);

        if (WouldOverflow(projected, solution.TargetColorCounts))
            return new ValidationResult(MoveOutcome.InvalidColorOverflow);

        return new ValidationResult(MoveOutcome.Valid, projected);
    }

    // ── Undo ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Undo işleminin checkpoint kilidini ihlal edip etmediğini kontrol eder.
    /// Plan §7.9 kural 3 ve §7.6.4.
    /// </summary>
    public ValidationResult ValidateUndo(PlayerRunState state)
    {
        // Undo yapılabilmesi için en az 2 hücre seçili olmalı (start hücresi sabit)
        if (state.SelectedPath.Count <= 1)
            return new ValidationResult(MoveOutcome.InvalidCheckpointLocked);

        // Checkpoint kilidi: undo sonrası path, CheckpointLockedLength'in altına inemez
        if (state.SelectedPath.Count <= state.CheckpointLockedLength)
            return new ValidationResult(MoveOutcome.InvalidCheckpointLocked);

        return new ValidationResult(MoveOutcome.Valid);
    }

    // ── Win check (hareket olmaksızın anlık kontrol) ───────────────────────────

    /// <summary>
    /// Mevcut state'in kazanma koşulunu sağlayıp sağlamadığını döner.
    /// GameManager'ın win durumunu güvenli bir şekilde doğrulaması için kullanılabilir.
    /// </summary>
    public bool IsWinState(PlayerRunState state, PathSolution solution, CellData lastCell)
    {
        if (lastCell == null || !lastCell.IsEnd) return false;
        return CountsMatch(state.CurrentColorCounts, solution.TargetColorCounts);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GridCoord CurrentPosition(PlayerRunState state)
    {
        int last = state.SelectedPath.Count - 1;
        return state.SelectedPath[last];
    }

    private static bool IsNeighbor(GridCoord from, GridCoord to)
    {
        int dx = Mathf.Abs(from.X - to.X);
        int dy = Mathf.Abs(from.Y - to.Y);
        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }

    private static Dictionary<CellColor, int> ProjectCounts(
        Dictionary<CellColor, int> current, CellColor addedColor)
    {
        var projected = new Dictionary<CellColor, int>(current);
        if (projected.ContainsKey(addedColor))
            projected[addedColor]++;
        else
            projected[addedColor] = 1;
        return projected;
    }

    private static bool WouldOverflow(
        Dictionary<CellColor, int> projected,
        Dictionary<CellColor, int> targets)
    {
        foreach (var kvp in projected)
        {
            if (!targets.TryGetValue(kvp.Key, out int target))
                return true;  // renk palette dışına çıktı
            if (kvp.Value > target)
                return true;
        }
        return false;
    }

    private static bool CountsMatch(
        Dictionary<CellColor, int> current,
        Dictionary<CellColor, int> targets)
    {
        if (current.Count != targets.Count) return false;
        foreach (var kvp in targets)
        {
            if (!current.TryGetValue(kvp.Key, out int val)) return false;
            if (val != kvp.Value)                            return false;
        }
        return true;
    }
}

// Kullanacak scriptler: GameManager (ValidateMove, ValidateUndo çağrısı ve MoveOutcome'a göre tepki)
