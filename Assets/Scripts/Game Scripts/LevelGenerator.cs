using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LevelGenerator : MonoBehaviour
{
    [Header("Center & Play Area")]
    public Transform center;
    public float playRadius = 150f;

    [Header("Rings (auto)")]
    public int ringCount = 3;
    public float minRingRadius = 40f;
    public float maxRingRadius = 120f;
    public float ringThickness = 10f;

    [Tooltip("Половина угла по вертикали (в градусах) для пояса мусора в кольце. 0 = идеальный экватор, 90 = полный шар.")]
    public float beltHalfAngle = 25f;

    [Header("Density")]
    public float debrisPer100UnitsOfCircumference = 2f;

    [Header("Debris Prefabs")]
    public GameObject[] debrisPrefabs;

    [Tooltip("Минимальное расстояние между объектами мусора. 0 = не проверять.")]
    public float minDistanceBetweenDebris = 2f;

    [Tooltip("Максимальное количество попыток найти позицию для одного объекта.")]
    public int maxPlacementTriesPerDebris = 10;

    [Header("Randomization")]
    public Vector2 debrisScaleRange = new Vector2(0.8f, 1.3f);
    public bool randomRotation = true;

    [Header("Initial Velocity")]
    public Vector2 initialSpeedRange = new Vector2(0.5f, 2.5f);
    public float maxAngularSpeedDegPerSec = 90f;

    [Header("Random Seed")]
    public bool useRandomSeed = true;
    public int randomSeed = 0;

    // --- ЗОНА ДЛЯ ИГРОКА ---
    [Header("Player Zone")]
    public Transform player;
    public UnityEvent onPlayerExitZone;   // сюда в инспекторе потом повесишь конец игры
    public bool oneShotExitEvent = true;  // вызвать один раз

    private bool exitEventFired = false;

    private readonly List<Transform> spawnedDebris = new List<Transform>();

    private void Start()
    {
        if (center == null)
            center = transform;

        if (!useRandomSeed)
            Random.InitState(randomSeed);

        ClearExistingDebris();   // ← вот это важно
        GenerateField();
    }

#if UNITY_EDITOR
    [ContextMenu("Regenerate In Editor")]
    private void RegenerateInEditor()
    {
        if (center == null)
            center = transform;

        if (!useRandomSeed)
            Random.InitState(randomSeed);

        ClearExistingDebris();   // ← очищаем всё, что было создано ранее
        GenerateField();
    }
#endif

    private void Update()
    {
        CheckPlayerZone();
    }

    // --- очистка всех детей-объектов мусора ---
    private void ClearExistingDebris()
    {
        spawnedDebris.Clear();

        // Удаляем всех детей генератора
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(child.gameObject);
            else
                Destroy(child.gameObject);
#else
            Destroy(child.gameObject);
#endif
        }
    }

    private void GenerateField()
    {
        if (debrisPrefabs == null || debrisPrefabs.Length == 0)
        {
            Debug.LogWarning("LevelGenerator: не заданы префабы мусора.");
            return;
        }

        spawnedDebris.Clear();

        float[] ringRadii = GenerateRingRadii();

        float beltHalfRad = beltHalfAngle * Mathf.Deg2Rad;
        float maxAbsY = Mathf.Sin(beltHalfRad);

        foreach (float radius in ringRadii)
        {
            GenerateRingAtRadius(radius, maxAbsY);
        }

        Debug.Log($"LevelGenerator: сгенерировано {spawnedDebris.Count} объектов мусора.");
    }

    private float[] GenerateRingRadii()
    {
        if (ringCount <= 0)
            return new float[0];

        float[] radii = new float[ringCount];
        float span = Mathf.Max(0.01f, maxRingRadius - minRingRadius);
        float step = span / ringCount;

        for (int i = 0; i < ringCount; i++)
        {
            float baseR = minRingRadius + step * (i + 0.5f);
            float jitter = Random.Range(-step * 0.3f, step * 0.3f);
            float r = Mathf.Clamp(baseR + jitter, minRingRadius, maxRingRadius);
            radii[i] = r;
        }

        System.Array.Sort(radii);
        return radii;
    }

    private void GenerateRingAtRadius(float radius, float maxAbsY)
    {
        float circumference = 2f * Mathf.PI * radius;
        int debrisCount = Mathf.RoundToInt(
            (circumference / 100f) * debrisPer100UnitsOfCircumference
        );

        if (debrisCount <= 0)
            return;

        for (int i = 0; i < debrisCount; i++)
        {
            bool placed = false;

            for (int attempt = 0; attempt < maxPlacementTriesPerDebris && !placed; attempt++)
            {
                Vector3 dir = GetRandomDirectionInBelt(maxAbsY);

                float inner = radius - ringThickness * 0.5f;
                float outer = radius + ringThickness * 0.5f;
                inner = Mathf.Max(0.1f, inner);

                float r = Random.Range(inner, outer);
                Vector3 worldPos = center.position + dir * r;

                if (minDistanceBetweenDebris > 0f &&
                    !IsFarEnoughFromOthers(worldPos, minDistanceBetweenDebris))
                {
                    continue;
                }

                GameObject prefab = debrisPrefabs[Random.Range(0, debrisPrefabs.Length)];
                if (prefab == null)
                    continue;

                GameObject go = Instantiate(prefab, worldPos, Quaternion.identity, transform);

                float scale = Random.Range(debrisScaleRange.x, debrisScaleRange.y);
                go.transform.localScale *= scale;

                if (randomRotation)
                    go.transform.rotation = Random.rotation;

                ApplyInitialVelocity(go);

                spawnedDebris.Add(go.transform);
                placed = true;
            }
        }
    }

    private Vector3 GetRandomDirectionInBelt(float maxAbsY)
    {
        for (int i = 0; i < 50; i++)
        {
            Vector3 dir = Random.onUnitSphere;
            if (Mathf.Abs(dir.y) <= maxAbsY)
                return dir;
        }

        return Random.onUnitSphere;
    }

    private bool IsFarEnoughFromOthers(Vector3 pos, float minDist)
    {
        float minSqr = minDist * minDist;
        for (int i = 0; i < spawnedDebris.Count; i++)
        {
            Transform t = spawnedDebris[i];
            if (t == null) continue;

            if ((t.position - pos).sqrMagnitude < minSqr)
                return false;
        }
        return true;
    }

    private void ApplyInitialVelocity(GameObject go)
    {
        var rb = go.GetComponent<Rigidbody>();
        if (rb == null)
            return;

        if (initialSpeedRange.y > 0f)
        {
            float speed = Random.Range(initialSpeedRange.x, initialSpeedRange.y);
            Vector3 dir = Random.onUnitSphere;
            rb.linearVelocity = dir * speed;
        }

        if (maxAngularSpeedDegPerSec > 0f)
        {
            Vector3 axis = Random.onUnitSphere;
            float angSpeedDeg = Random.Range(0f, maxAngularSpeedDegPerSec);
            float angSpeedRad = angSpeedDeg * Mathf.Deg2Rad;
            rb.angularVelocity = axis * angSpeedRad;
        }
    }

    // ---------- ПРОВЕРКА ВЫХОДА ИГРОКА ЗА ПРЕДЕЛЫ ЗОНЫ ----------
    private void CheckPlayerZone()
    {
        if (player == null || center == null)
            return;

        float dist = Vector3.Distance(center.position, player.position);

        if (dist > playRadius)
        {
            if (oneShotExitEvent)
            {
                if (!exitEventFired)
                {
                    exitEventFired = true;
                    onPlayerExitZone?.Invoke();
                }
            }
            else
            {
                onPlayerExitZone?.Invoke();
            }
        }
        else
        {
            // если игрок вернулся обратно внутрь, можно снова разрешить триггер
            if (oneShotExitEvent)
                exitEventFired = false;
        }
    }

    // Просто чтобы видеть границу в редакторе
    private void OnDrawGizmosSelected()
    {
        if (center == null) center = transform;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center.position, playRadius);
    }
}
