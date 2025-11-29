using UnityEngine;
using Unity.Cinemachine;

public class ThrustCameraNoise : MonoBehaviour
{
    [Header("References")]
    public SimpleController_ZeroG player;        // Player с IsThrusting
    public CinemachineCamera cineCamera;         // Cinemachine Camera

    [Header("Shake Settings")]
    public float maxAmplitude = 1.0f;            // сила тряски при полном thrust
    public float maxFrequency = 3.0f;            // частота шума при thrust
    public float blendSpeed   = 6.0f;            // скорость нарастания/затухания

    private float currentAmp  = 0f;
    private float currentFreq = 0f;

    private CinemachineBasicMultiChannelPerlin noise; // noise-компонент

    private void Awake()
    {
        if (cineCamera == null)
            cineCamera = GetComponent<CinemachineCamera>();

        if (cineCamera == null)
        {
            Debug.LogError("ThrustCameraNoise: CinemachineCamera не найден на объекте.");
            return;
        }

        // В CM3 иногда компоненты могут сидеть на дочернем объекте, поэтому ищем и там
        noise = GetComponent<CinemachineBasicMultiChannelPerlin>();
        if (noise == null)
            noise = GetComponentInChildren<CinemachineBasicMultiChannelPerlin>(true);

        if (noise == null)
        {
            Debug.LogError(
                "ThrustCameraNoise: не найден CinemachineBasicMultiChannelPerlin. " +
                "На Cinemachine Camera в Procedural Components → Noise выбери 'Basic Multi Channel Perlin' " +
                "и задай Noise Profile (например, 6D Shake)."
            );
        }
    }

    private void Update()
    {
        if (noise == null || player == null)
            return;

        bool thrusting = player.IsThrusting;

        float targetAmp  = thrusting ? maxAmplitude : 0f;
        float targetFreq = thrusting ? maxFrequency : 0f;

        currentAmp  = Mathf.Lerp(currentAmp,  targetAmp,  Time.deltaTime * blendSpeed);
        currentFreq = Mathf.Lerp(currentFreq, targetFreq, Time.deltaTime * blendSpeed);

        noise.AmplitudeGain = currentAmp;
        noise.FrequencyGain = currentFreq;
    }

    // ВСПОМОГАТЕЛЬНЫЙ ТЕСТ: можно вызвать из контекстного меню компонента
    [ContextMenu("Test Constant Shake")]
    private void TestConstantShake()
    {
        if (noise == null)
        {
            Debug.LogWarning("TestConstantShake: noise == null");
            return;
        }

        currentAmp  = maxAmplitude;
        currentFreq = maxFrequency;
        noise.AmplitudeGain = currentAmp;
        noise.FrequencyGain = currentFreq;
        Debug.Log("ThrustCameraNoise: включен тестовый постоянный shake.");
    }
}
