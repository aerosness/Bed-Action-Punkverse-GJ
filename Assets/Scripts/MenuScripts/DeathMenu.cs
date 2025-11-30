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
        Time.timeScale = 0f;

        // освободить мышку
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Player died.");
    }
}
