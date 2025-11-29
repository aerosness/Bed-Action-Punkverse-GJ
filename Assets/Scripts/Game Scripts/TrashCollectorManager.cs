using UnityEngine;
using TMPro;

public class TrashCollectorManager : MonoBehaviour
{
    public static TrashCollectorManager Instance;

    [Header("—колько мусора нужно собрать")]
    [SerializeField] private int requiredTrash = 5;

    [Header("HUD счЄтчик (опционально)")]
    [SerializeField] private TMP_Text counterText;

    private int currentTrash = 0;

    public bool AllCollected => currentTrash >= requiredTrash;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("TrashCollectorManager: второй экземпл€р, уничтожаю.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        UpdateCounterUI();
    }

    public void AddTrash(int amount)
    {
        // защитимс€ от кривых значений
        if (amount <= 0)
        {
            Debug.LogWarning("TrashCollectorManager.AddTrash: amount <= 0");
            return;
        }

        currentTrash += amount;
        if (currentTrash > requiredTrash)
            currentTrash = requiredTrash;

        UpdateCounterUI();

        if (AllCollected)
        {
            PlayerMissionState.HasTrash = true;
            Debug.Log("TrashCollectorManager: собрано достаточно мусора. HasTrash = true");
            // здесь можно вызвать ToDo, если используешь
            // TodoListUI.Instance?.SetTask("¬ернитесь к кораблю");
        }
    }

    private void UpdateCounterUI()
    {
        if (counterText == null)
        {
            // это Ќ≈ критично Ч просто не показываем UI
            // Debug.Log("TrashCollectorManager: counterText не назначен, UI-счЄтчик не обновл€ю.");
            return;
        }

        counterText.text = $"{currentTrash} / {requiredTrash}";
    }
}
