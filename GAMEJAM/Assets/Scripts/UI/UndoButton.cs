using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Inspector'daki Button'a atanır; OnClick → GameManager.TryUndo().
/// </summary>
[RequireComponent(typeof(Button))]
public class UndoButton : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => GameManager.Instance.TryUndo());
    }
}
