using UnityEngine;

/// <summary>
/// STUB — SwipeInputController derleme bağımlılığını karşılar.
/// Gerçek implementasyon: Plan §7.5, §7.7, §7.9.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// SwipeInputController tarafından her swipe'ta çağrılır.
    /// Gerçek implementasyon: RunValidator → state güncelle → GridManager highlight → win check.
    /// </summary>
    public void TryMovePlayer(GridCoord target)
    {
        // TODO: implement
    }
}

// Kullanacak scriptler: SwipeInputController (TryMovePlayer çağrısı),
//                       LevelManager, RunValidator, CheckpointManager, HintManager
