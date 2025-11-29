public static class PlayerMissionState
{
    public static bool FirstTime = true;      // первый заход в игру
    public static bool HasTrash = false;      // собран нужный мусор
    public static bool TrashBurned = false;   // мусор сожжён в печке
    public static bool HasArrivedAtLocation = false;
    public static bool IsWearingSpacesuit = false;

    /// <summary>
    /// Сброс состояния под НОВУЮ миссию (вылетаем снова).
    /// Вызываем в момент вылета с корабля.
    /// </summary>
    public static void ResetForNewMission()
    {
        HasTrash = false;             // новый мусор будем собирать с нуля
        TrashBurned = false;          // печка снова "пустая"
        HasArrivedAtLocation = false; // ещё не прилетели на локацию
        IsWearingSpacesuit = false;   // внутри корабля считаем, что без скафандра
        // FirstTime НЕ трогаем – он нужен, чтобы отличать первый заход в игру
    }
}
