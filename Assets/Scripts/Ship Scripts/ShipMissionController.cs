using UnityEngine;
using TMPro;

public class ShipMissionController : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TMP_Text hudText;
    [TextArea]
    [SerializeField] private string flyMessage = "Press [E], to go on a mission";
    [TextArea]
    [SerializeField] private string needTrashMessage = "Throw trash in the furnace first";

    [Header("Ссылки")]
    [SerializeField] private FurnaceController furnace;              // Печь
    [SerializeField] private string playerTag = "Player";            // Тег игрока
    [SerializeField] private Transform cameraShakeTarget;            // Камера или её родитель
    [SerializeField] private MonoBehaviour playerControllerToLock;   // сюда можно перетащить FirstPersonController

    [Header("Первый вылет")]
    [Tooltip("Можно ли первый раз улететь без печки")]
    [SerializeField] private bool allowFirstMissionWithoutFurnace = true;

    [Tooltip("Если true — считается, что это первый вылет. После первого полёта станет false.")]
    [SerializeField] private bool isFirstMission = true;

    [Header("Параметры полёта")]
    [SerializeField] private float flightDuration = 5f;              // 5–6 секунд
    [SerializeField] private float shakeIntensity = 0.3f;
    [SerializeField] private float shakeFrequency = 25f;

    private bool playerInTrigger;
    private bool isFlying;
    private float flightTimer;

    private Vector3 cameraOriginalLocalPos;

    private void Start()
    {
        if (hudText != null)
            hudText.gameObject.SetActive(false);

        if (cameraShakeTarget == null && Camera.main != null)
            cameraShakeTarget = Camera.main.transform;

        if (cameraShakeTarget != null)
            cameraOriginalLocalPos = cameraShakeTarget.localPosition;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInTrigger = true;
        UpdateHudMessage();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInTrigger = false;

        if (hudText != null)
            hudText.gameObject.SetActive(false);
    }

    private void Update()
    {
        HandleFlight();

        if (!playerInTrigger || isFlying)
            return;

        // Нажатие E у корабля
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!CanFlyNow())
            {
                // Просто обновляем HUD на предупреждение
                ShowWarning();
                return;
            }

            StartFlight();
        }
    }

    // --- ЛОГИКА УСЛОВИЙ ---

    private bool CanFlyNow()
    {
        // Первый вылет — разрешаем без печки
        if (allowFirstMissionWithoutFurnace && isFirstMission)
            return true;

        // Далее — только если печка использована
        if (furnace == null)
            return false;

        return furnace.IsFurnaceUsed;
    }

    private void UpdateHudMessage()
    {
        if (hudText == null)
            return;

        if (CanFlyNow())
        {
            hudText.text = flyMessage;
        }
        else
        {
            hudText.text = needTrashMessage;
        }

        hudText.gameObject.SetActive(true);
    }

    private void ShowWarning()
    {
        if (hudText == null)
            return;

        hudText.text = needTrashMessage;
        hudText.gameObject.SetActive(true);
    }

    // --- СТАРТ ПОЛЁТА ---

    private void StartFlight()
    {
        isFlying = true;
        flightTimer = flightDuration;

        if (hudText != null)
            hudText.gameObject.SetActive(false);

        // Первый полёт случился — больше не первый
        if (isFirstMission)
            isFirstMission = false;

        // Лочим управление игрока, если задано
        if (playerControllerToLock != null)
            playerControllerToLock.enabled = false;

        // Сохраняем исходную позицию камеры
        if (cameraShakeTarget != null)
            cameraOriginalLocalPos = cameraShakeTarget.localPosition;

        Debug.Log("Корабль: старт полёта на миссию.");
    }

    // --- ОБРАБОТКА ПОЛЁТА И ТРЯСКИ ---

    private void HandleFlight()
    {
        if (!isFlying)
            return;

        flightTimer -= Time.deltaTime;

        float t = Mathf.Clamp01(1f - (flightTimer / flightDuration)); // 0 -> 1 по мере полёта

        // Тряска
        if (cameraShakeTarget != null)
        {
            float shake = shakeIntensity * (1f - Mathf.Abs(0.5f - t) * 2f);
            // усиление к середине и спад к концу

            float offsetX = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) - 0.5f) * 2f * shake;
            float offsetY = (Mathf.PerlinNoise(0f, Time.time * shakeFrequency) - 0.5f) * 2f * shake;

            cameraShakeTarget.localPosition = cameraOriginalLocalPos + new Vector3(offsetX, offsetY, 0f);
        }

        if (flightTimer <= 0f)
        {
            EndFlight();
        }
    }

    private void EndFlight()
    {
        isFlying = false;

        if (cameraShakeTarget != null)
            cameraShakeTarget.localPosition = cameraOriginalLocalPos;

        if (playerControllerToLock != null)
            playerControllerToLock.enabled = true;

        PlayerMissionState.HasArrivedAtLocation = true;
        TodoListUI.Instance.SetTask("Put on a spacesuit");


        Debug.Log("Корабль: полёт завершён. Здесь можно вызвать загрузку следующей сцены.");
    }

}
