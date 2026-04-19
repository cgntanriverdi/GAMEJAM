using System.Collections;
using UnityEngine;

/// <summary>
/// Oyuncunun grid üzerindeki konumunu temsil eden hareket eden nesne.
/// GameManager her başarılı hamlede MoveTo(), level başlangıcında Teleport() çağırır.
/// </summary>
public class PlayerToken : MonoBehaviour
{
    [SerializeField] private float  moveDuration = 0.2f;
    [SerializeField] private Sprite rabbitSprite;

    private SpriteRenderer _sr;
    private Sprite         _dogSprite;
    private Coroutine      _moveCoroutine;

    private void Awake()
    {
        _sr        = GetComponent<SpriteRenderer>();
        _dogSprite = _sr != null ? _sr.sprite : null;
        _baseScale = transform.localScale;
    }

    private Sprite _catSprite;
    private Vector3 _baseScale;

    public void ApplyCharacter()
    {
        if (_sr == null) return;

        if (CharacterManager.Current == CharacterManager.CharacterType.Rabbit)
        {
            if (rabbitSprite == null)
            {
                var sprites = Resources.LoadAll<Sprite>("rabbit_final");
                if (sprites != null && sprites.Length > 0)
                {
                    rabbitSprite = sprites[0];
                }
            }
            _sr.sprite = rabbitSprite != null ? rabbitSprite : _dogSprite;
        }
        else if (CharacterManager.Current == CharacterManager.CharacterType.Cat)
        {
            if (_catSprite == null)
            {
                var sprites = Resources.LoadAll<Sprite>("kedi_final");
                if (sprites != null && sprites.Length > 0)
                {
                    _catSprite = sprites[0];
                }
            }
            _sr.sprite = _catSprite != null ? _catSprite : _dogSprite;
        }
        else
        {
            _sr.sprite = _dogSprite;
        }
    }

    public void MoveTo(Vector3 targetPos)
    {
        targetPos.z = transform.position.z;
        if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
        _moveCoroutine = StartCoroutine(SmoothMove(targetPos));
    }

    /// <summary>
    /// Geçersiz hamle geri bildirimi: originPos'tan targetPos'a %40 yaklaşıp geri döner.
    /// originPos her zaman oyuncunun gerçek grid hücresinin dünya konumu olmalı;
    /// böylece spam durumunda token sürüklenmez.
    /// </summary>
    public void BounceToward(Vector3 originPos, Vector3 targetPos)
    {
        originPos.z = transform.position.z;
        targetPos.z = transform.position.z;
        if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
        _moveCoroutine = StartCoroutine(BounceCoroutine(originPos, targetPos));
    }

    public void Teleport(Vector3 pos)
    {
        if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
        pos.z = transform.position.z;
        transform.position = pos;
    }

    private IEnumerator BounceCoroutine(Vector3 origin, Vector3 target)
    {
        transform.position = origin;
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
