using UnityEngine;

public class SceneStart : MonoBehaviour
{
    private void Start()
    {
        // первый визит Ч только полЄт
        if (PlayerMissionState.FirstTime)
            TodoListUI.Instance.SetTask("Go on a mission");
    }
}
