using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ThrustSoundFader : MonoBehaviour
{
    [Header("References")]
    public SimpleController_ZeroG player;   // контроллер игрока

    [Header("Volume")]
    [Range(0f, 1f)]
    public float maxVolume = 1f;           // громкость при полном thrust

    [Tooltip("Время разгона громкости при начале thrust (сек).")]
    public float fadeInTime = 0.3f;

    [Tooltip("Время затухания громкости после отпускания thrust (сек).")]
    public float fadeOutTime = 0.5f;

    private AudioSource src;

    private void Awake()
    {
        src = GetComponent<AudioSource>();

        if (player == null)
            player = FindObjectOfType<SimpleController_ZeroG>();

        // базовые настройки
        src.loop = true;          // звук двигателя должен крутиться по кругу
        src.playOnAwake = false;  // сами запустим
        src.volume = 0f;
    }

    private void Update()
    {
        if (player == null)
            return;

        bool thrusting = player.IsThrusting;

        // если начали жать thrust — убеждаемся, что звук играет
        if (thrusting && !src.isPlaying)
            src.Play();

        float targetVolume = thrusting ? maxVolume : 0f;

        // скорость изменения громкости
        float t = thrusting ? fadeInTime : fadeOutTime;
        t = Mathf.Max(0.01f, t); // защита от деления на 0
        float speed = maxVolume / t;

        src.volume = Mathf.MoveTowards(src.volume, targetVolume, speed * Time.deltaTime);

        // когда громкость почти нулевая и thrust не жмут — можно стопнуть клип
        if (!thrusting && src.volume <= 0.001f && src.isPlaying)
            src.Stop();
    }
}
