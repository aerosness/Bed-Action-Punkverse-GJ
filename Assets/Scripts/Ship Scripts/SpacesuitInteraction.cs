using UnityEngine;
using TMPro;

public class SpacesuitInteraction : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TMP_Text hudText;
    [TextArea]
    [SerializeField] private string equipMessage = "Press [E], to put on the spacesuit";

    [Header("Визуал скафандра")]
    [SerializeField] private GameObject spacesuitVisual;

    [Header("Игрок")]
    [SerializeField] private string playerTag = "Player";

    // ---------- АУДИО ----------
    [Header("Audio")]
    [SerializeField] private AudioSource equipAudioSource;
    [SerializeField] private AudioClip equipSound;
    [SerializeField] private AudioClip equipLoopSound;
    // -----------------------------

    private bool playerInTrigger;
    private bool equippedLocally;

    private void Start()
    {
        if (hudText != null)
            hudText.gameObject.SetActive(false);

        if (spacesuitVisual == null)
            spacesuitVisual = gameObject;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInTrigger = true;
        UpdateHud();
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
        if (!playerInTrigger)
            return;

        if (!PlayerMissionState.HasArrivedAtLocation)
        {
            if (hudText != null)
                hudText.gameObject.SetActive(false);
            return;
        }

        if (PlayerMissionState.IsWearingSpacesuit || equippedLocally)
        {
            if (hudText != null)
                hudText.gameObject.SetActive(false);
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            EquipSpacesuit();
        }
    }

    private void UpdateHud()
    {
        if (hudText == null)
            return;

        if (!PlayerMissionState.HasArrivedAtLocation ||
            PlayerMissionState.IsWearingSpacesuit || equippedLocally)
        {
            hudText.gameObject.SetActive(false);
            return;
        }

        hudText.text = equipMessage;
        hudText.gameObject.SetActive(true);
    }

    private void EquipSpacesuit()
    {
        PlayerMissionState.IsWearingSpacesuit = true;
        TodoListUI.Instance.SetTask("Open the doors");
        equippedLocally = true;

        if (hudText != null)
            hudText.gameObject.SetActive(false);

        // убрать скафандр из мира
        if (spacesuitVisual != null)
            spacesuitVisual.SetActive(false);

        // ---------- ЗВУК НАДЕВАНИЯ ----------
        if (equipAudioSource != null && equipSound != null)
        {
            equipAudioSource.PlayOneShot(equipSound);
        }

        if (equipAudioSource != null && equipLoopSound != null)
        {
            equipAudioSource.clip = equipLoopSound;
            equipAudioSource.loop = true;
            equipAudioSource.Play();
        }
        // -------------------------------------

        Debug.Log("Скафандр надет.");
    }
}
