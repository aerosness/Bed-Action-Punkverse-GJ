using UnityEngine;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void RefreshTask()
    {
        // ――― 1. Первый заход в игру ―――
        if (PlayerMissionState.FirstTime)
        {
            TodoListUI.Instance.SetTask("Go on a Mission");
            return;
        }

        // ――― 2. Игрок прилетел с мусором ―――
        if (PlayerMissionState.HasTrash && !PlayerMissionState.TrashBurned)
        {
            TodoListUI.Instance.SetTask("Throw trash in the furnace");
            return;
        }

        // ――― 3. Мусор сожжён → следующее действие ―――
        if (PlayerMissionState.TrashBurned && !PlayerMissionState.HasArrivedAtLocation)
        {
            TodoListUI.Instance.SetTask("Return to the ship");
            return;
        }

        // ――― 4. Игрок прилетел на базу ―――
        if (PlayerMissionState.HasArrivedAtLocation && !PlayerMissionState.IsWearingSpacesuit)
        {
            TodoListUI.Instance.SetTask("Put on a spacesuit");
            return;
        }

        // ――― 5. После скафандра ―――
        if (PlayerMissionState.IsWearingSpacesuit)
        {
            TodoListUI.Instance.SetTask("Open the doors and exit");
            return;
        }

        // ――― 6. На случай непредвиденного ―――
        TodoListUI.Instance.SetTask("Seek further instructions");
    }
}
