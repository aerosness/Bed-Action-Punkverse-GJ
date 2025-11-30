using UnityEngine;

public class DeathMenu : MonoBehaviour
{
    public GameObject deathScreen; // перетащи канвас-объект сюда
    [SerializeField] private AudioSource equipAudioSource;

    public void Die()
    {
        equipAudioSource.Play();
        Destroy(gameObject);
        // активировать меню смерти
        if (deathScreen != null)
            deathScreen.SetActive(true);

        // остановить время (если нужно)

        // освободить мышку
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Player died.");
    }
}
