using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class BlackHoleBehaviour : MonoBehaviour
{
    [Header("Movement Zone")]
    Transform center;   // Центр системы (обычно тот же, что в LevelGenerator)
    public float innerRadius = 60f;             // Минимальный радиус орбиты
    public float outerRadius = 130f;            // Максимальный радиус орбиты

    [Header("Movement")]
    [Tooltip("Скорость перемещения чёрной дыры между случайными точками.")]
    public float moveSpeed = 5f;
    [Tooltip("Каждые сколько секунд выбирать новую случайную цель движения.")]
    public Vector2 targetChangeInterval = new Vector2(3f, 8f);

    [Header("Rotation")]
    [Tooltip("Максимальная угловая скорость вращения (град/сек).")]
    public float maxRotationSpeedDeg = 120f;

    [Header("Scaling (pulse)")]
    [Tooltip("Базовый множитель размера (1 = исходный scale префаба).")]
    public float baseScaleMultiplier = 1f;

    [Tooltip("Диапазон пульсации размера (минимум/максимум) до рандомизации.")]
    public Vector2 pulseScaleRange = new Vector2(0.8f, 1.3f);

    [Tooltip("Насколько случайно смещать диапазон пульсации (в обе стороны). 0.2 = ±20%.")]
    public float pulseRangeJitter = 0.2f;

    [Tooltip("Диапазон скоростей пульсации (циклов в секунду) для разных дыр.")]
    public Vector2 pulseSpeedRange = new Vector2(0.5f, 1.5f);

    [Tooltip("Время 'проявления' после спавна (ease-in).")]
    public float spawnEaseInTime = 1.5f;

    [Header("Kill behaviour")]
    [Tooltip("Уничтожать ли любые другие объекты при касании (кроме других чёрных дыр).")]
    public bool destroyOtherObjects = true;

    [Tooltip("Событие при касании игрока (объекта с тегом Player).")]
    public UnityEvent onPlayerHit;

    // --- приватные поля ---
    private Vector3 _baseScale;
    private Vector3 _targetPosition;
    private float _nextTargetTime;
    private Vector3 _rotationAxis;
    private float _rotationSpeedDeg;
    private float _spawnTime;

    // Рандомизированные параметры пульсации
    private Vector2 _instancePulseScaleRange;
    private float _pulseSpeed;
    private float _pulsePhaseOffset;

    private void Awake()
    {
        // Коллайдер должен быть триггером
        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }

        _spawnTime = Time.time;

        // Центр — пробуем подтянуть из LevelGenerator, если не указан явно
        if (center == null)
        {
            var generator = FindObjectOfType<LevelGenerator>();
            if (generator != null)
                center = generator.center != null ? generator.center : generator.transform;
        }

        if (center == null)
        {
            Debug.LogWarning("BlackHoleBehaviour: center не назначен, используем (0,0,0).");
        }

        // --- РАНДОМИЗАЦИЯ РАЗМЕРА ---

        // Базовый множитель размера для этой дыры
        // Например, если baseScaleMultiplier = 1, а jitter = ±0.25 → итог от 0.75 до 1.25
        float baseJitter = Random.Range(1f - pulseRangeJitter, 1f + pulseRangeJitter);
        float finalBaseMultiplier = baseScaleMultiplier * baseJitter;
        _baseScale = transform.localScale * finalBaseMultiplier;

        // Диапазон пульсации (min/max) тоже немного шевелим,
        // чтобы одни дыры «дышали» от 0.7 до 1.2, другие — от 0.9 до 1.5 и т.д.
        float rangeMul = Random.Range(1f - pulseRangeJitter, 1f + pulseRangeJitter);
        _instancePulseScaleRange = new Vector2(
            pulseScaleRange.x * rangeMul,
            pulseScaleRange.y * rangeMul
        );

        // Гарантируем, что min <= max и оба > 0
        _instancePulseScaleRange.x = Mathf.Max(0.01f, Mathf.Min(_instancePulseScaleRange.x, _instancePulseScaleRange.y));
        _instancePulseScaleRange.y = Mathf.Max(_instancePulseScaleRange.x, _instancePulseScaleRange.y);

        // --- РАНДОМИЗАЦИЯ СКОРОСТИ ПУЛЬСА И ФАЗЫ ---

        _pulseSpeed = Random.Range(pulseSpeedRange.x, pulseSpeedRange.y);
        _pulsePhaseOffset = Random.Range(0f, Mathf.PI * 2f);

        // --- ВРАЩЕНИЕ ---

        _rotationAxis = Random.onUnitSphere;
        _rotationSpeedDeg = Random.Range(maxRotationSpeedDeg * 0.3f, maxRotationSpeedDeg);

        // Первая цель движения
        PickNewTargetPosition();
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        UpdateMovement(dt);
        UpdateRotation(dt);
        UpdateScale();
    }

    // --------- MOVEMENT ---------
    private void UpdateMovement(float dt)
    {
        if (center == null)
            return;

        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, moveSpeed * dt);

        if (Time.time >= _nextTargetTime ||
            Vector3.Distance(transform.position, _targetPosition) < 0.5f)
        {
            PickNewTargetPosition();
        }

        float distFromCenter = Vector3.Distance(center.position, transform.position);
        if (distFromCenter > outerRadius * 1.2f)
        {
            Vector3 dir = (transform.position - center.position).normalized;
            transform.position = center.position + dir * outerRadius;
            PickNewTargetPosition();
        }
    }

    private void PickNewTargetPosition()
    {
        if (center == null)
        {
            _targetPosition = transform.position;
            return;
        }

        Vector3 dir = Random.onUnitSphere;
        dir.Normalize();

        float r = Random.Range(innerRadius, outerRadius);
        _targetPosition = center.position + dir * r;

        float t = Random.Range(targetChangeInterval.x, targetChangeInterval.y);
        _nextTargetTime = Time.time + t;
    }

    // --------- ROTATION ---------
    private void UpdateRotation(float dt)
    {
        if (_rotationSpeedDeg <= 0f)
            return;

        transform.Rotate(_rotationAxis, _rotationSpeedDeg * dt, Space.Self);
    }

    // --------- SCALE (EASE-IN + RANDOMIZED PULSE) ---------
    private void UpdateScale()
    {
        float timeSinceSpawn = Time.time - _spawnTime;

        // 1) Ease-in от 0 до 1 по плавной функции
        float ease = 1f;
        if (spawnEaseInTime > 0f)
        {
            float t = Mathf.Clamp01(timeSinceSpawn / spawnEaseInTime);
            ease = t * t * (3f - 2f * t); // smoothstep
        }

        // 2) Пульсация: своя скорость + фазовый сдвиг
        float raw = Mathf.Sin(_pulsePhaseOffset + Time.time * _pulseSpeed * Mathf.PI * 2f);
        float pulseT = (raw + 1f) * 0.5f; // 0..1
        pulseT = pulseT * pulseT * (3f - 2f * pulseT); // сгладили края

        float pulseScale = Mathf.Lerp(_instancePulseScaleRange.x, _instancePulseScaleRange.y, pulseT);

        transform.localScale = _baseScale * (ease * pulseScale);
    }

    // --------- COLLISION / TRIGGER ---------
    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        // Игнорируем другие чёрные дыры
        if (other.GetComponent<BlackHoleBehaviour>() != null)
            return;

        // Если задели игрока
        if (other.CompareTag("Player"))
        {
            onPlayerHit?.Invoke();
            // игрока обычно не уничтожаем напрямую, геймовер через событие
            return;
        }

        if (destroyOtherObjects)
        {
            if (center != null && other.transform == center)
                return;

            Destroy(other.gameObject);
        }
    }
}
