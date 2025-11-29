using UnityEngine;
using TMPro;

public class FurnaceController : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TMP_Text hudText;
    [TextArea]
    [SerializeField] private string interactMessage = "Press [E], to throw the trash in";

    [Header("Огонь печки")]
    [SerializeField] private GameObject fireBig;

    [Header("Звук огня")]
    [SerializeField] private AudioSource fireAudio;
    [SerializeField] private float minVolume = 0f;
    [SerializeField] private float maxVolume = 1.0f;

    [Header("Игрок")]
    [SerializeField] private string playerTag = "Player";

    [Header("Заглушка прогресса")]
    [SerializeField] private bool debugHasTrash = false;

    [Header("Анимация масштаба")]
    [SerializeField] private Vector3 fireMinScale = new Vector3(0.2f, 0.2f, 0.2f);
    [SerializeField] private Vector3 fireMaxScale = new Vector3(0.7f, 0.7f, 0.7f);

    private bool playerInTrigger;
    private bool furnaceUsed;
    public bool IsFurnaceUsed => furnaceUsed;
    private float fireTimer = 0f;
    private const float bigFireDuration = 30f;

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

    private bool HasTrashFromPreviousScene => debugHasTrash;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInTrigger = true;

        if (!furnaceUsed && HasTrashFromPreviousScene && hudText != null)
        {
            hudText.text = interactMessage;
            hudText.gameObject.SetActive(true);
        }
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
        // --- Огонь и звук активен ---
        if (furnaceUsed && fireTimer > 0f)
        {
            fireTimer -= Time.deltaTime;

            float elapsed = bigFireDuration - fireTimer;
            float halfDuration = bigFireDuration * 0.5f;

            // === МАСШТАБ ===
            if (fireBig != null)
            {
                if (elapsed <= halfDuration)
                {
                    float t = elapsed / halfDuration;
                    fireBig.transform.localScale = Vector3.Lerp(fireMinScale, fireMaxScale, t);
                }
                else
                {
                    float t = (elapsed - halfDuration) / halfDuration;
                    fireBig.transform.localScale = Vector3.Lerp(fireMaxScale, fireMinScale, t);
                }
            }

            // === ЗВУК ===
            if (fireAudio != null)
            {
                if (!fireAudio.isPlaying)
                    fireAudio.Play();

                if (elapsed <= halfDuration)
                {
                    float t = elapsed / halfDuration;
                    fireAudio.volume = Mathf.Lerp(minVolume, maxVolume, t);
                }
                else
                {
                    float t = (elapsed - halfDuration) / halfDuration;
                    fireAudio.volume = Mathf.Lerp(maxVolume, minVolume, t);
                }
            }

            // === Завершение огня ===
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

        if (!playerInTrigger || furnaceUsed)
            return;

        if (!HasTrashFromPreviousScene)
        {
            hudText?.gameObject.SetActive(false);
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            furnaceUsed = true;
            PlayerMissionState.TrashBurned = true;

            hudText?.gameObject.SetActive(false);

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

            Debug.Log("Печь: запущен большой огонь с анимацией масштаба и громкости.");
        }
    }
}
