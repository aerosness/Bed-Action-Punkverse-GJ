using UnityEngine;
using TMPro;

public class DoorButtonSlidingDoors : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TMP_Text hudText;

    [TextArea] public string openMessage = "Нажмите [E], чтобы открыть двери";
    [TextArea] public string needSuitMessage = "Наденьте скафандр, чтобы выйти";
    [TextArea] public string doorsAlreadyOpenMessage = "Двери уже открыты";

    [Header("Игрок")]
    [SerializeField] private string playerTag = "Player";

    [Header("Двери")]
    [SerializeField] private Transform doorLeft;
    [SerializeField] private Transform doorRight;

    [Tooltip("На сколько смещать двери по оси Z")]
    [SerializeField] private float openOffsetZ = 1.5f;

    [SerializeField] public float openDuration = 1.0f;

    [Header("Звук")]
    [SerializeField] private AudioSource doorAudio;

    private bool playerInTrigger;
    private bool isOpening;
    private bool isOpened;

    private float openTimer;

    private Vector3 leftClosedPos;
    private Vector3 rightClosedPos;
    private Vector3 leftOpenPos;
    private Vector3 rightOpenPos;

    private void Start()
    {
        if (hudText != null)
            hudText.gameObject.SetActive(false);

        leftClosedPos = doorLeft.localPosition;
        rightClosedPos = doorRight.localPosition;

        leftOpenPos = leftClosedPos + new Vector3(0, 0, -openOffsetZ);
        rightOpenPos = rightClosedPos + new Vector3(0, 0, +openOffsetZ);
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
        HandleDoorAnimation();

        if (!playerInTrigger)
            return;

        if (isOpened)
        {
            hudText.text = doorsAlreadyOpenMessage;
            hudText.gameObject.SetActive(true);
        }

        if (Input.GetKeyDown(KeyCode.E))
            TryOpen();
    }

    private void UpdateHudMessage()
    {
        if (hudText == null) return;

        if (!PlayerMissionState.HasArrivedAtLocation)
        {
            hudText.gameObject.SetActive(false);
            return;
        }

        if (isOpened)
        {
            hudText.text = doorsAlreadyOpenMessage;
            hudText.gameObject.SetActive(true);
            return;
        }

        if (!PlayerMissionState.IsWearingSpacesuit)
        {
            hudText.text = needSuitMessage;
            hudText.gameObject.SetActive(true);
            return;
        }

        hudText.text = openMessage;
        hudText.gameObject.SetActive(true);
    }

    private void TryOpen()
    {
        if (!PlayerMissionState.HasArrivedAtLocation) return;
        if (isOpened) return;

        if (!PlayerMissionState.IsWearingSpacesuit)
        {
            hudText.text = needSuitMessage;
            hudText.gameObject.SetActive(true);
            return;
        }

        StartOpening(false);
    }

    /// <summary>
    /// startClosing = true → закрыть двери
    /// startClosing = false → открыть двери
    /// </summary>
    private void StartOpening(bool startClosing)
    {
        if (isOpening)
            return;

        isOpening = true;
        openTimer = 0f;

        if (startClosing)
        {
            // закрытие
            isOpened = true; // чтобы закрывалось корректно
        }
        TodoListUI.Instance.SetTask("Leave the ship");

        if (doorAudio != null)
            doorAudio.Play();
    }

    private void HandleDoorAnimation()
    {
        if (!isOpening)
            return;

        openTimer += Time.deltaTime;
        float t = Mathf.Clamp01(openTimer / openDuration);

        Vector3 targetLeft = isOpened ? leftClosedPos : leftOpenPos;
        Vector3 targetRight = isOpened ? rightClosedPos : rightOpenPos;

        Vector3 fromLeft = isOpened ? leftOpenPos : leftClosedPos;
        Vector3 fromRight = isOpened ? rightOpenPos : rightClosedPos;

        doorLeft.localPosition = Vector3.Lerp(fromLeft, targetLeft, t);
        doorRight.localPosition = Vector3.Lerp(fromRight, targetRight, t);

        if (t >= 1f)
        {
            // Переключаем состояние
            isOpened = !isOpened;
            isOpening = false;
        }
    }

    public void ForceCloseDoors()
    {
        if (isOpening) return;
        if (!isOpened) return; // было наоборот — из-за ЭТОГО двери не закрывались

        StartOpening(true);
    }
}
