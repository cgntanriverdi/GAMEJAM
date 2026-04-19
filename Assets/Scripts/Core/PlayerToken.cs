using System.Collections;
using UnityEngine;

/// <summary>
/// Oyuncunun grid üzerindeki konumunu temsil eden hareket eden nesne.
/// GameManager her başarılı hamlede MoveTo(), level başlangıcında Teleport() çağırır.
/// </summary>
public class PlayerToken : MonoBehaviour
{
    [SerializeField] private float moveDuration = 0.2f;

    private Coroutine _moveCoroutine;

    public void MoveTo(Vector3 targetPos)
    {
        targetPos.z = transform.position.z;
        if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
        _moveCoroutine = StartCoroutine(SmoothMove(targetPos));
    }

    /// <summary>Geçersiz hamle geri bildirimi: hedefe %40 yaklaşıp geri döner.</summary>
    public void BounceToward(Vector3 targetPos)
    {
        targetPos.z = transform.position.z;
        if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
        _moveCoroutine = StartCoroutine(BounceCoroutine(targetPos));
    }

    public void Teleport(Vector3 pos)
    {
        if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
        pos.z = transform.position.z;
        transform.position = pos;
    }

    private IEnumerator BounceCoroutine(Vector3 target)
    {
        Vector3 origin  = transform.position;
        Vector3 peak    = Vector3.Lerp(origin, target, 0.4f);
        float   halfDur = moveDuration * 0.5f;
        float   elapsed = 0f;

        while (elapsed < halfDur)
        {
            elapsed           += Time.deltaTime;
            transform.position = Vector3.Lerp(origin, peak, elapsed / halfDur);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < halfDur)
        {
            elapsed           += Time.deltaTime;
            transform.position = Vector3.Lerp(peak, origin, elapsed / halfDur);
            yield return null;
        }
        transform.position = origin;
    }

    private IEnumerator SmoothMove(Vector3 target)
    {
        Vector3 start   = transform.position;
        float   elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed           += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target, elapsed / moveDuration);
            yield return null;
        }
        transform.position = target;
    }
}

// Kullanacak scriptler: GameManager (Teleport, MoveTo çağrısı)
