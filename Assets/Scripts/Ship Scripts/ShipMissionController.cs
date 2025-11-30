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

    [Header("Связи")]
    [SerializeField] private FurnaceController furnace;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Transform cameraShakeTarget;
    [SerializeField] private MonoBehaviour playerControllerToLock;

    [Header("Первый вылет")]
    [SerializeField] private bool allowFirstMissionWithoutFurnace = true;
    [SerializeField] private bool isFirstMission = true;

    [Header("Параметры полета")]
    [SerializeField] private float flightDuration = 5f;
    [SerializeField] private float shakeIntensity = 0.3f;
    [SerializeField] private float shakeFrequency = 25f;

    // ---------- АУДИО ----------
    [Header("Audio")]
    [SerializeField] private AudioSource flightAudioSource; 
    [SerializeField] private AudioClip flightStartClip;   
    [SerializeField] private AudioClip flightLoopClip;    
    // -----------------------------

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

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!CanFlyNow())
            {
                ShowWarning();
                return;
            }

            StartFlight();
        }
    }

    // --- Проверки ---

    private bool CanFlyNow()
    {
        if (allowFirstMissionWithoutFurnace && isFirstMission)
            return true;

        if (furnace == null)
            return false;

        return PlayerMissionState.TrashBurned;
    }

    private void UpdateHudMessage()
    {
        if (hudText == null)
            return;

        hudText.text = CanFlyNow() ? flyMessage : needTrashMessage;
        hudText.gameObject.SetActive(true);
    }

    private void ShowWarning()
    {
        if (hudText == null)
            return;

        hudText.text = needTrashMessage;
        hudText.gameObject.SetActive(true);
    }

    // --- Старт полета ---

    private void StartFlight()
    {
        PlayerMissionState.ResetForNewMission();

        isFlying = true;
        flightTimer = flightDuration;

        if (hudText != null)
            hudText.gameObject.SetActive(false);

        if (playerControllerToLock != null)
            playerControllerToLock.enabled = false;

        if (cameraShakeTarget != null)
            cameraOriginalLocalPos = cameraShakeTarget.localPosition;

        // ---------- ЗАПУСК ЗВУКА ----------
        if (flightAudioSource != null && flightStartClip != null)
        {
            flightAudioSource.PlayOneShot(flightStartClip);
        }

        if (flightAudioSource != null && flightLoopClip != null)
        {
            flightAudioSource.clip = flightLoopClip;
            flightAudioSource.loop = true;
            flightAudioSource.Play();
        }
        // ----------------------------------

        Debug.Log("Полет: корабль отрывается от платформы.");
    }


    // --- Логика полета + тряска камеры ---

    private void HandleFlight()
    {
        if (!isFlying)
            return;

        flightTimer -= Time.deltaTime;

        float t = Mathf.Clamp01(1f - (flightTimer / flightDuration));

        // Тряска
        if (cameraShakeTarget != null)
        {
            float shake = shakeIntensity * (1f - Mathf.Abs(0.5f - t) * 2f);

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

        // ---------- ОСТАНОВКА ЗВУКА ----------
        if (flightAudioSource != null)
        {
            flightAudioSource.loop = false;
            flightAudioSource.Stop();
        }
        // --------------------------------------

        Debug.Log("Полет завершен. Корабль прибыл.");
    }

}
