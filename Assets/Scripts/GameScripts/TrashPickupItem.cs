using UnityEngine;
using TMPro;

public class TrashPickupItem : MonoBehaviour
{
    [Header("Игрок")]
    [SerializeField] private string playerTag = "Player";

    [Header("HUD подсказка")]
    [SerializeField] private TMP_Text hintText;
    [TextArea]
    [SerializeField] private string hintMessage = "Нажмите [E], чтобы подобрать мусор";

    private bool playerInTrigger;
    private bool pickedUp;

    private void Start()
    {
        if (hintText != null)
            hintText.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (pickedUp) return;
        if (!other.CompareTag(playerTag)) return;

        playerInTrigger = true;

        if (hintText != null)
        {
            hintText.text = hintMessage;
            hintText.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInTrigger = false;

        if (hintText != null)
            hintText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!playerInTrigger || pickedUp) return;

        // старый Input, так что у тебя должен стоять Active Input Handling = Both
        if (Input.GetKeyDown(KeyCode.E))
        {
            PickUp();
        }
    }

    private void PickUp()
    {
        pickedUp = true;

        if (hintText != null)
            hintText.gameObject.SetActive(false);

        if (TrashCollectorManager.Instance != null)
        {
            TrashCollectorManager.Instance.AddTrash(1);
        }
        else
        {
            Debug.LogError("TrashPickupItem: TrashCollectorManager.Instance == null. " +
                           "Убедись, что в сцене есть объект с TrashCollectorManager.");
        }

        Destroy(gameObject);
    }
}
