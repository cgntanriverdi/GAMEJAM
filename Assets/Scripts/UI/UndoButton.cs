using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UndoButton : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => GameManager.Instance?.TryUndo());
    }
}
