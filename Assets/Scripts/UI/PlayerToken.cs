using UnityEngine;

/// <summary>
/// Oyuncu tokeninin (köpek) ve bitiş hücresi overlay'inin (kemik) görsel bileşeni.
/// GameManager her hamlede ve undo'da MoveTo() çağırır.
/// Scale, GridManager.CellSize'a göre Initialize() sırasında ayarlanır.
/// </summary>
public class PlayerToken : MonoBehaviour
{
    [Header("Overlays (isteğe bağlı)")]
    [SerializeField] private Transform[] _scaledChildren;

    /// <summary>
    /// Level başında GridManager.CellSize ve başlangıç pozisyonu ile çağrılır.
    /// </summary>
    public void Initialize(float cellSize, Vector3 startWorldPos)
    {
        transform.localScale = new Vector3(cellSize, cellSize, 1f);
        transform.position   = startWorldPos;
    }

    /// <summary>Oyuncu bir hücreye geçtiğinde veya undo'da yeni pozisyona taşır.</summary>
    public void MoveTo(Vector3 worldPos)
    {
        transform.position = worldPos;
    }
}
