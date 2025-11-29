using UnityEngine;
using TMPro;

public class FurnaceController : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TMP_Text hudText;
    [TextArea]
    [SerializeField] private string interactMessage = "Нажмите [E], чтобы сжечь мусор";

    [Header("Огонь печки")]
    [SerializeField] private GameObject fireBig;
    [SerializeField] private AudioSource fireAudio;

    [Header("Игрок")]
    [SerializeField] private string playerTag = "Player";

    [Header("Анимация огня")]
    [SerializeField] private Vector3 fireMinScale = new Vector3(0.2f, 0.2f, 0.2f);
    [SerializeField] private Vector3 fireMaxScale = new Vector3(0.7f, 0.7f, 0.7f);
    [SerializeField] private float minVolume = 0.2f;
    [SerializeField] private float maxVolume = 1.0f;
    [SerializeField] private float bigFireDuration = 5f;

    private bool playerInTrigger;
    private bool furnaceUsedThisScene;
    private float fireTimer;

    private void Start()
    {
        if (hudText != null)
            hudText.gameObject.SetActive(false);

        if (fireBig != null)
        {
            fireBig.SetActive(false);
            fireBig.transform.localScale = fireMinScale;
        }

        if (fireAudio != null)
        {
            fireAudio.volume = minVolume;
            fireAudio.Stop();
        }
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
        HandleFireAnimation();

        if (!playerInTrigger)
            return;

        // условие: мусор есть и ещё не сожгли
        bool canUseFurnace = PlayerMissionState.HasTrash && !PlayerMissionState.TrashBurned;

        if (!canUseFurnace)
            return;

        if (Input.GetKeyDown(KeyCode.E))
            UseFurnace();
    }

    private void UpdateHud()
    {
        if (hudText == null)
            return;

        // если нет мусора или уже сожгли — печь молчит
        if (!PlayerMissionState.HasTrash || PlayerMissionState.TrashBurned)
        {
            hudText.gameObject.SetActive(false);
            return;
        }

        hudText.text = interactMessage;
        hudText.gameObject.SetActive(true);
    }

    private void UseFurnace()
    {
        if (furnaceUsedThisScene)
            return;

        furnaceUsedThisScene = true;

        // отмечаем в глобальном стейте
        PlayerMissionState.TrashBurned = true;
        PlayerMissionState.HasTrash = false;   // мусор потрачен

        if (hudText != null)
            hudText.gameObject.SetActive(false);

        // запускаем визуал
        if (fireBig != null)
        {
            fireBig.SetActive(true);
            fireBig.transform.localScale = fireMinScale;
        }

        if (fireAudio != null)
        {
            fireAudio.volume = minVolume;
            fireAudio.Play();
        }

        fireTimer = bigFireDuration;

        // сюда же можно повесить ToDo типа:
        // TodoListUI.Instance?.SetTask("Вернитесь к кораблю");
    }

    private void HandleFireAnimation()
    {
        if (fireTimer <= 0f)
            return;

        fireTimer -= Time.deltaTime;
        float elapsed = bigFireDuration - fireTimer;
        float half = bigFireDuration * 0.5f;

        if (fireBig != null)
        {
            if (elapsed <= half)
            {
                float t = elapsed / half;
                fireBig.transform.localScale = Vector3.Lerp(fireMinScale, fireMaxScale, t);
            }
            else
            {
                float t = (elapsed - half) / half;
                fireBig.transform.localScale = Vector3.Lerp(fireMaxScale, fireMinScale, t);
            }
        }

        if (fireAudio != null)
        {
            if (!fireAudio.isPlaying)
                fireAudio.Play();

            if (elapsed <= half)
            {
                float t = elapsed / half;
                fireAudio.volume = Mathf.Lerp(minVolume, maxVolume, t);
            }
            else
            {
                float t = (elapsed - half) / half;
                fireAudio.volume = Mathf.Lerp(maxVolume, minVolume, t);
            }
        }

        if (fireTimer <= 0f)
        {
            if (fireBig != null)
            {
                fireBig.transform.localScale = fireMinScale;
                fireBig.SetActive(false);
            }

            if (fireAudio != null)
            {
                fireAudio.Stop();
                fireAudio.volume = minVolume;
            }
        }
    }
}
