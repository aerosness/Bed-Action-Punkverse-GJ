using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ReturnToShipZone : MonoBehaviour
{
    [Header("Игрок")]
    [SerializeField] private string playerTag = "Player";

    [Header("Название сцены с кораблём")]
    [SerializeField] private string shipSceneName = "Scene_Ship"; // впиши своё имя сцены

    [Header("HUD подсказка")]
    [SerializeField] private TMP_Text hintText;
    [TextArea]
    [SerializeField] private string needMoreTrashMessage = "Сначала соберите весь мусор";
    [TextArea]
    [SerializeField] private string canReturnMessage = "Нажмите [E], чтобы вернуться на корабль";

    private bool playerInTrigger;

    private void Start()
    {
        if (hintText != null)
            hintText.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInTrigger = true;
        UpdateHint();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInTrigger = false;

        if (hintText != null)
            hintText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!playerInTrigger)
            return;

        bool allCollected = TrashCollectorManager.Instance != null &&
                            TrashCollectorManager.Instance.AllCollected;

        if (!allCollected)
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            ReturnToShip();
        }
    }

    private void UpdateHint()
    {
        if (hintText == null)
            return;

        bool allCollected = TrashCollectorManager.Instance != null &&
                            TrashCollectorManager.Instance.AllCollected;

        if (allCollected)
        {
            hintText.text = canReturnMessage;
        }
        else
        {
            hintText.text = needMoreTrashMessage;
        }

        hintText.gameObject.SetActive(true);
    }

    private void ReturnToShip()
    {
        // Первый вылет закончился
        PlayerMissionState.FirstTime = false;

        // Мы возвращаемся в сцену корабля (сцена 1)
        SceneManager.LoadScene(shipSceneName);
    }
}
