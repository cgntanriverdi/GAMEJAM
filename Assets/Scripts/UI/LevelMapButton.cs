using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class LevelMapButton : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(HandleClick);
    }

    private static void HandleClick()
    {
        StartupMenuUI.Instance?.OpenLevelMap();
    }
}
