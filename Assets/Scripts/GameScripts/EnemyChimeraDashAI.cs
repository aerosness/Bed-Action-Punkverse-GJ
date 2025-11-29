using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyChimeraDashAI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Detection")]
    [Tooltip("Радиус, в котором химера агрится на игрока.")]
    public float detectionRadius = 30f;

    [Tooltip("Радиус, после которого химера считает, что игрок ушёл (немного больше detection, чтобы не дёргалось).")]
    public float loseTargetRadius = 40f;

    [Header("Patrol")]
    [Tooltip("Скорость патрулирования.")]
    public float patrolSpeed = 5f;

    [Tooltip("Точки патруля (опционально). Если пусто — химера будет просто дрейфовать вокруг стартовой позиции.")]
    public Transform[] patrolPoints;

    [Tooltip("Насколько близко подлететь к точке, чтобы считать её достигнутой.")]
    public float patrolPointReachDistance = 1.0f;

    [Tooltip("Скорость поворота при патруле и idle.")]
    public float lookRotateSpeed = 4f;

    [Header("Dash Settings")]
    public float windupTime = 0.4f;      // фаза перед рывком
    public float dashForce = 60f;        // сила рывка
    public float dashDuration = 0.6f;    // как долго продолжается рывок
    public float recoveryTime = 0.8f;    // фаза восстановления
    public float maxSpeed = 20f;         // максимальная скорость при рывке

    [Header("Smoothing")]
    public float dragDuringDash = 0.5f;  // линейный демпфинг во время рывка
    public float dragNormal = 2f;        // обычный демпфинг

    private Rigidbody rb;
    private float stateTimer = 0f;

    private int currentPatrolIndex = 0;
    private Vector3 initialPosition;
    private bool playerInAggro = false;

    private enum AIState { Idle, Patrol, Windup, Dash, Recover }
    private AIState state = AIState.Idle;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = dragNormal;

        initialPosition = transform.position;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        // Стартуем в патруле, если есть точки, иначе — idle
        if (patrolPoints != null && patrolPoints.Length > 0)
            SwitchState(AIState.Patrol);
        else
            SwitchState(AIState.Idle);
    }

    private void FixedUpdate()
    {
        if (player == null)
            return;

        stateTimer -= Time.fixedDeltaTime;

        float distToPlayer = Vector3.Distance(transform.position, player.position);
        UpdateAggro(distToPlayer);

        switch (state)
        {
            case AIState.Idle:
                IdleBehavior();
                break;

            case AIState.Patrol:
                PatrolBehavior();
                break;

            case AIState.Windup:
                WindupBehavior();
                break;

            case AIState.Dash:
                DashBehavior();
                break;

            case AIState.Recover:
                RecoverBehavior();
                break;
        }
    }

    /// <summary>
    /// Логика агро / деагро в зависимости от дистанции.
    /// </summary>
    private void UpdateAggro(float distToPlayer)
    {
        // Входим в агро-зону
        if (!playerInAggro && distToPlayer <= detectionRadius)
        {
            playerInAggro = true;

            // Если мы были в Idle/Patrol — начинаем атаку
            if (state == AIState.Idle || state == AIState.Patrol)
            {
                SwitchState(AIState.Windup);
            }
        }
        // Выходим из агро-зоны
        else if (playerInAggro && distToPlayer >= loseTargetRadius)
        {
            // Сбрасываем флаг
            playerInAggro = false;

            // Если сейчас не в самом рывке — сразу возвращаемся к патрулю/idle
            if (state != AIState.Dash && state != AIState.Windup)
            {
                if (patrolPoints != null && patrolPoints.Length > 0)
                    SwitchState(AIState.Patrol);
                else
                    SwitchState(AIState.Idle);
            }
            // Если в Dash/Windup — дадим рывку/циклу завершиться,
            // а уже после Recover вернёмся к патрулю (см. RecoverBehavior).
        }
    }

    // ---------- IDLE ----------
    private void IdleBehavior()
    {
        // Медленно гасим скорость
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 2f);

        // Можем плавно крутиться, смотреть в случайную сторону, но для простоты оставим так.
        if (stateTimer <= 0f)
        {
            // Idle по таймеру можно сменить на Patrol, если есть точки
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                SwitchState(AIState.Patrol);
            }
            else
            {
                // Если хочешь, чтобы в Idle химера просто оставалась вечно — можешь убрать это.
                stateTimer = 9999f;
            }
        }
    }

    // ---------- PATROL ----------
    private void PatrolBehavior()
    {
        // Если игрок занёсся в агро — логику патруля не выполняем
        if (playerInAggro)
            return;

        Vector3 targetPos;

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Transform patrolTarget = patrolPoints[currentPatrolIndex];
            if (patrolTarget != null)
            {
                targetPos = patrolTarget.position;

                // Движение к точке
                Vector3 dir = (targetPos - transform.position);
                float dist = dir.magnitude;

                if (dist > patrolPointReachDistance)
                {
                    dir.Normalize();
                    rb.linearVelocity = dir * patrolSpeed;

                    // Поворот лицом к направлению движения
                    if (dir.sqrMagnitude > 0.0001f)
                    {
                        Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
                        transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.fixedDeltaTime * lookRotateSpeed);
                    }
                }
                else
                {
                    // Переключаемся на следующую точку
                    currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                }
            }
        }
        else
        {
            // Если патрульных точек нет, просто дрейфуем вокруг стартовой позиции
            Vector3 dirToCenter = initialPosition - transform.position;
            float dist = dirToCenter.magnitude;

            if (dist > 2f)
            {
                dirToCenter.Normalize();
                rb.linearVelocity = dirToCenter * patrolSpeed * 0.6f;
            }
            else
            {
                // Немного тормозим у центра
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 1.5f);
            }
        }
    }

    // ---------- WINDUP (подготовка к рывку) ----------
    private void WindupBehavior()
    {
        // Поворачиваемся к игроку
        Vector3 dir = (player.position - transform.position).normalized;
        Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.fixedDeltaTime * 6f);

        if (stateTimer <= 0f)
        {
            PerformDash();
        }
    }

    // ---------- DASH ----------
    private void DashBehavior()
    {
        // Ограничиваем скорость
        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;

        if (stateTimer <= 0f)
        {
            SwitchState(AIState.Recover);
        }
    }

    // ---------- RECOVER ----------
    private void RecoverBehavior()
    {
        if (stateTimer <= 0f)
        {
            // Если игрок всё ещё в агро-зоне — повторяем цикл атаки
            if (playerInAggro)
            {
                SwitchState(AIState.Windup);
            }
            else
            {
                // Иначе возвращаемся к патрулю/idle
                if (patrolPoints != null && patrolPoints.Length > 0)
                    SwitchState(AIState.Patrol);
                else
                    SwitchState(AIState.Idle);
            }
        }
    }

    // ---------- Выполнение рывка ----------
    private void PerformDash()
    {
        Vector3 dir = (player.position - transform.position).normalized;

        rb.linearDamping = dragDuringDash;
        rb.AddForce(dir * dashForce, ForceMode.VelocityChange);

        SwitchState(AIState.Dash, dashDuration);
    }

    // ---------- Смена состояния ----------
    private void SwitchState(AIState newState, float customTime = -1f)
    {
        state = newState;

        switch (newState)
        {
            case AIState.Idle:
                rb.linearDamping = dragNormal;
                // Если хочешь, чтобы Idle был "бесконечным" — можно ставить большое число
                stateTimer = (customTime > 0 ? customTime : 3f);
                break;

            case AIState.Patrol:
                rb.linearDamping = dragNormal;
                stateTimer = (customTime > 0 ? customTime : 9999f); // по сути безлимит
                break;

            case AIState.Windup:
                rb.linearDamping = dragNormal;
                stateTimer = (customTime > 0 ? customTime : windupTime);
                break;

            case AIState.Dash:
                // drag уже выставлен в PerformDash
                stateTimer = (customTime > 0 ? customTime : dashDuration);
                break;

            case AIState.Recover:
                rb.linearDamping = dragNormal;
                stateTimer = (customTime > 0 ? customTime : recoveryTime);
                break;
        }
    }
}
