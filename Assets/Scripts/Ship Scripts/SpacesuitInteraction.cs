using UnityEngine;
using TMPro;

public class SpacesuitInteraction : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TMP_Text hudText;
    [TextArea]
    [SerializeField] private string equipMessage = "Press [E], to put on the spacesuit";

    [Header("Объект скафандра")]
    [SerializeField] private GameObject spacesuitVisual; // сам модель/объект скафандра

    [Header("Игрок")]
    [SerializeField] private string playerTag = "Player";

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

        // До прилёта на локацию вообще нельзя взаимодействовать
        if (!PlayerMissionState.HasArrivedAtLocation)
        {
            if (hudText != null)
                hudText.gameObject.SetActive(false);
            return;
        }

        // Если уже надели скафандр – нечего делать
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

        // Прячем визуал скафандра
        if (spacesuitVisual != null)
            spacesuitVisual.SetActive(false);

        // При желании здесь можно:
        // - включить пост-эффект шлема
        // - включить HUD шлема и т.д.
        Debug.Log("Игрок надел скафандр.");
    }
}
