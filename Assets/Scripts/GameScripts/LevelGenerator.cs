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
    [Tooltip("Сколько объектов мусора на 100 единиц длины окружности.")]
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

    // --- ЧЁРНЫЕ ДЫРЫ ---
    [Header("Black Holes")]
    [Tooltip("Префабы чёрных дыр (объекты-препятствия).")]
    public GameObject[] blackHolePrefabs;

    [Tooltip("Минимальное и максимальное количество чёрных дыр на уровне.")]
    public int minBlackHoles = 2;
    public int maxBlackHoles = 5;

    [Tooltip("Радиус, внутри которого чёрные дыры не появляются (зона безопасности вокруг центра).")]
    public float blackHoleInnerRadius = 60f;

    [Tooltip("Максимальный радиус появления чёрных дыр.")]
    public float blackHoleOuterRadius = 130f;

    [Tooltip("Половина угла по вертикали (в градусах) для пояса чёрных дыр.")]
    public float blackHoleBeltHalfAngle = 35f;

    [Tooltip("Минимальное расстояние от чёрной дыры до мусора.")]
    public float minDistanceBlackHoleToDebris = 15f;

    [Tooltip("Минимальное расстояние между самими чёрными дырами.")]
    public float minDistanceBetweenBlackHoles = 25f;

    // --- ХИМЕРЫ ---
    [Header("Chimeras")]
    [Tooltip("Префабы химеры (вражеский ИИ).")]
    public GameObject[] chimeraPrefabs;

    [Tooltip("Минимальное и максимальное количество химер.")]
    public int minChimeras = 1;
    public int maxChimeras = 3;

    [Tooltip("Минимальный радиус спавна химер от центра.")]
    public float chimeraInnerRadius = 40f;

    [Tooltip("Максимальный радиус спавна химер от центра.")]
    public float chimeraOuterRadius = 140f;

    [Tooltip("Половина угла по вертикали (в градусах) для пояса химер. 90 = почти везде.")]
    public float chimeraBeltHalfAngle = 80f;

    [Tooltip("Минимальное расстояние от химеры до мусора.")]
    public float minDistanceChimeraToDebris = 10f;

    [Tooltip("Минимальное расстояние от химеры до чёрных дыр.")]
    public float minDistanceChimeraToBlackHoles = 15f;

    [Tooltip("Минимальное расстояние между химерми.")]
    public float minDistanceBetweenChimeras = 25f;

    // --- ТОПЛИВО / COLLECTIBLES ---
    [Header("Fuel Collectibles")]
    [Tooltip("Префабы топлива (важные collectible).")]
    public GameObject[] fuelPrefabs;

    [Tooltip("Минимальное и максимальное количество канистр топлива на уровне.")]
    public int minFuelCount = 3;
    public int maxFuelCount = 6;

    [Tooltip("Минимальный радиус спавна топлива от центра (чтобы не было халявы у базы).")]
    public float fuelInnerRadius = 50f;

    [Tooltip("Максимальный радиус спавна топлива от центра.")]
    public float fuelOuterRadius = 140f;

    [Tooltip("Половина угла по вертикали (в градусах) для пояса топлива.")]
    public float fuelBeltHalfAngle = 85f;

    [Tooltip("Минимальное расстояние от топлива до мусора.")]
    public float minDistanceFuelToDebris = 6f;

    [Tooltip("Минимальное расстояние от топлива до чёрных дыр.")]
    public float minDistanceFuelToBlackHoles = 12f;

    [Tooltip("Минимальное расстояние от топлива до химер.")]
    public float minDistanceFuelToChimeras = 12f;

    [Tooltip("Минимальное расстояние между самими канистрами топлива.")]
    public float minDistanceBetweenFuel = 18f;

    [Header("Player Zone")]
    public Transform player;
    public UnityEvent onPlayerExitZone;
    public bool oneShotExitEvent = true;

    private bool exitEventFired = false;

    private readonly List<Transform> spawnedDebris      = new List<Transform>();
    private readonly List<Transform> spawnedBlackHoles  = new List<Transform>();
    private readonly List<Transform> spawnedChimeras    = new List<Transform>();
    private readonly List<Transform> spawnedFuel        = new List<Transform>();

    private void Start()
    {
        if (center == null)
            center = transform;

        if (!useRandomSeed)
            Random.InitState(randomSeed);

        ClearExistingObjects();
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

        ClearExistingObjects();
        GenerateField();
    }
#endif

    private void Update()
    {
        CheckPlayerZone();
    }

    // --- очистка всех детей-объектов ---
    private void ClearExistingObjects()
    {
        spawnedDebris.Clear();
        spawnedBlackHoles.Clear();
        spawnedChimeras.Clear();
        spawnedFuel.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Object.DestroyImmediate(child.gameObject);
            else
                Object.Destroy(child.gameObject);
#else
            Object.Destroy(child.gameObject);
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
        spawnedBlackHoles.Clear();
        spawnedChimeras.Clear();
        spawnedFuel.Clear();

        float[] ringRadii = GenerateRingRadii();

        float beltHalfRad = beltHalfAngle * Mathf.Deg2Rad;
        float maxAbsY = Mathf.Sin(beltHalfRad);

        // 1) Мусор
        foreach (float radius in ringRadii)
        {
            GenerateRingAtRadius(radius, maxAbsY);
        }

        // 2) Чёрные дыры
        GenerateBlackHoles();

        // 3) Химеры
        GenerateChimeras();

        // 4) Топливо (последним, чтобы учесть всё остальное)
        GenerateFuel();

        Debug.Log(
            $"LevelGenerator: мусор={spawnedDebris.Count}, " +
            $"чёрные дыры={spawnedBlackHoles.Count}, " +
            $"химеры={spawnedChimeras.Count}, " +
            $"топливо={spawnedFuel.Count}."
        );
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
                    !IsFarEnoughFromList(worldPos, spawnedDebris, minDistanceBetweenDebris))
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

    private bool IsFarEnoughFromList(Vector3 pos, List<Transform> list, float minDist)
    {
        float minSqr = minDist * minDist;
        for (int i = 0; i < list.Count; i++)
        {
            Transform t = list[i];
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

    // ---------- ЧЁРНЫЕ ДЫРЫ ----------
    private void GenerateBlackHoles()
    {
        if (blackHolePrefabs == null || blackHolePrefabs.Length == 0)
        {
            Debug.LogWarning("LevelGenerator: не заданы префабы чёрных дыр.");
            return;
        }

        int bhCount = Mathf.Clamp(
            Random.Range(minBlackHoles, maxBlackHoles + 1),
            0,
            100
        );

        if (bhCount <= 0)
            return;

        float beltRad = blackHoleBeltHalfAngle * Mathf.Deg2Rad;
        float maxAbsY = Mathf.Sin(beltRad);

        for (int i = 0; i < bhCount; i++)
        {
            bool placed = false;

            for (int attempt = 0; attempt < 50 && !placed; attempt++)
            {
                Vector3 dir = Random.onUnitSphere;
                if (Mathf.Abs(dir.y) > maxAbsY)
                    continue;

                float r = Random.Range(blackHoleInnerRadius, blackHoleOuterRadius);
                Vector3 worldPos = center.position + dir * r;

                if (minDistanceBlackHoleToDebris > 0f &&
                    !IsFarEnoughFromList(worldPos, spawnedDebris, minDistanceBlackHoleToDebris))
                {
                    continue;
                }

                if (minDistanceBetweenBlackHoles > 0f &&
                    !IsFarEnoughFromList(worldPos, spawnedBlackHoles, minDistanceBetweenBlackHoles))
                {
                    continue;
                }

                GameObject prefab = blackHolePrefabs[Random.Range(0, blackHolePrefabs.Length)];
                if (prefab == null)
                    continue;

                GameObject bh = Instantiate(prefab, worldPos, Quaternion.identity, transform);
                spawnedBlackHoles.Add(bh.transform);
                placed = true;
            }
        }
    }

    // ---------- ХИМЕРЫ ----------
    private void GenerateChimeras()
    {
        if (chimeraPrefabs == null || chimeraPrefabs.Length == 0)
        {
            Debug.Log("LevelGenerator: префабы химер не заданы — пропускаем.");
            return;
        }

        int chimCount = Mathf.Clamp(
            Random.Range(minChimeras, maxChimeras + 1),
            0,
            50
        );

        if (chimCount <= 0)
            return;

        float beltRad = chimeraBeltHalfAngle * Mathf.Deg2Rad;
        float maxAbsY = Mathf.Sin(beltRad);

        for (int i = 0; i < chimCount; i++)
        {
            bool placed = false;

            for (int attempt = 0; attempt < 50 && !placed; attempt++)
            {
                Vector3 dir = Random.onUnitSphere;
                if (Mathf.Abs(dir.y) > maxAbsY)
                    continue;

                float r = Random.Range(chimeraInnerRadius, chimeraOuterRadius);
                Vector3 worldPos = center.position + dir * r;

                if (minDistanceChimeraToDebris > 0f &&
                    !IsFarEnoughFromList(worldPos, spawnedDebris, minDistanceChimeraToDebris))
                {
                    continue;
                }

                if (minDistanceChimeraToBlackHoles > 0f &&
                    !IsFarEnoughFromList(worldPos, spawnedBlackHoles, minDistanceChimeraToBlackHoles))
                {
                    continue;
                }

                if (minDistanceBetweenChimeras > 0f &&
                    !IsFarEnoughFromList(worldPos, spawnedChimeras, minDistanceBetweenChimeras))
                {
                    continue;
                }

                GameObject prefab = chimeraPrefabs[Random.Range(0, chimeraPrefabs.Length)];
                if (prefab == null)
                    continue;

                GameObject chim = Instantiate(prefab, worldPos, Quaternion.identity, transform);
                spawnedChimeras.Add(chim.transform);

                // Можно сразу подтянуть центр патруля
                var ai = chim.GetComponent<EnemyChimeraDashAI>();
                if (ai != null && ai.patrolCenter == null)
                    ai.patrolCenter = center;

                placed = true;
            }
        }
    }

    // ---------- ТОПЛИВО ----------
    private void GenerateFuel()
    {
        if (fuelPrefabs == null || fuelPrefabs.Length == 0)
        {
            Debug.Log("LevelGenerator: префабы топлива не заданы — пропускаем.");
            return;
        }

        int fuelCount = Mathf.Clamp(
            Random.Range(minFuelCount, maxFuelCount + 1),
            0,
            50
        );

        if (fuelCount <= 0)
            return;

        float beltRad = fuelBeltHalfAngle * Mathf.Deg2Rad;
        float maxAbsY = Mathf.Sin(beltRad);

        for (int i = 0; i < fuelCount; i++)
        {
            bool placed = false;

            for (int attempt = 0; attempt < 60 && !placed; attempt++)
            {
                Vector3 dir = Random.onUnitSphere;
                if (Mathf.Abs(dir.y) > maxAbsY)
                    continue;

                float r = Random.Range(fuelInnerRadius, fuelOuterRadius);
                Vector3 worldPos = center.position + dir * r;

                // не вплотную к мусору
                if (minDistanceFuelToDebris > 0f &&
                    !IsFarEnoughFromList(worldPos, spawnedDebris, minDistanceFuelToDebris))
                {
                    continue;
                }

                // не вплотную к чёрным дырам
                if (minDistanceFuelToBlackHoles > 0f &&
                    !IsFarEnoughFromList(worldPos, spawnedBlackHoles, minDistanceFuelToBlackHoles))
                {
                    continue;
                }

                // не вплотную к химерм
                if (minDistanceFuelToChimeras > 0f &&
                    !IsFarEnoughFromList(worldPos, spawnedChimeras, minDistanceFuelToChimeras))
                {
                    continue;
                }

                // не стэкаем канистры
                if (minDistanceBetweenFuel > 0f &&
                    !IsFarEnoughFromList(worldPos, spawnedFuel, minDistanceBetweenFuel))
                {
                    continue;
                }

                GameObject prefab = fuelPrefabs[Random.Range(0, fuelPrefabs.Length)];
                if (prefab == null)
                    continue;

                GameObject fuel = Instantiate(prefab, worldPos, Quaternion.identity, transform);
                spawnedFuel.Add(fuel.transform);
                placed = true;
            }
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
            if (oneShotExitEvent)
                exitEventFired = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (center == null) center = transform;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center.position, playRadius);

        Gizmos.color = new Color(1f, 0.2f, 0.4f, 0.4f);
        Gizmos.DrawWireSphere(center.position, blackHoleInnerRadius);
        Gizmos.DrawWireSphere(center.position, blackHoleOuterRadius);

        Gizmos.color = new Color(0.3f, 0.9f, 0.3f, 0.4f);
        Gizmos.DrawWireSphere(center.position, chimeraInnerRadius);
        Gizmos.DrawWireSphere(center.position, chimeraOuterRadius);

        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(center.position, fuelInnerRadius);
        Gizmos.DrawWireSphere(center.position, fuelOuterRadius);
    }
}
