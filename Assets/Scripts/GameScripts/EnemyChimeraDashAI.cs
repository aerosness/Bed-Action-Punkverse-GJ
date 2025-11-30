using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class EnemyChimeraDashAI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    // Автонаход контроллера игрока
    private SimpleController_ZeroG playerController;

    [Header("Detection")]
    [Tooltip("Радиус, в котором химера агрится на игрока.")]
    public float detectionRadius = 30f;

    [Tooltip("Радиус, после которого химера считает, что игрок ушёл (немного больше detection, чтобы не дёргалось).")]
    public float loseTargetRadius = 40f;

    [Header("State Options")]
    [Tooltip("Включать ли состояние Idle вообще.")]
    public bool enableIdleState = true;

    [Tooltip("Начинать ли в Idle, а не с патруля.")]
    public bool startInIdle = false;

    [Tooltip("Сколько секунд висеть в Idle, если не агримся.")]
    public float idleDuration = 3f;

    [Header("Patrol (BlackHole-style)")]
    [Tooltip("Центр, вокруг которого химера патрулирует. Если не задан, берётся её стартовая позиция.")]
    public Transform patrolCenter;
    [Tooltip("Минимальный радиус патруля.")]
    public float patrolInnerRadius = 5f;
    [Tooltip("Максимальный радиус патруля.")]
    public float patrolOuterRadius = 15f;
    [Tooltip("Скорость патрулирования.")]
    public float patrolSpeed = 5f;
    [Tooltip("Интервал смены цели патруля (секунд).")]
    public Vector2 patrolTargetChangeInterval = new Vector2(2f, 5f);
    [Tooltip("Насколько близко подлететь к цели, чтобы выбрать новую.")]
    public float patrolTargetReachDistance = 0.5f;

    [Header("Rotation")]
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
    public float dragNormal = 0.2f;      // обычный демпфинг

    [Header("Audio")]
    [Tooltip("Звук подготовки к рывку (windup).")]
    public AudioClip windupClip;

    [Tooltip("Громкость звука подготовки.")]
    [Range(0f, 1f)] public float windupVolume = 1f;

    [Tooltip("Звук самого рывка (dash).")]
    public AudioClip dashClip;

    [Tooltip("Громкость звука рывка.")]
    [Range(0f, 1f)] public float dashVolume = 1f;

    [Tooltip("Случайный разброс питча (0 = выключен).")]
    [Range(0f, 0.5f)] public float randomPitchJitter = 0.1f;

    [Header("Player Interaction")]
    public UnityEvent onPlayerKilled;

    private Rigidbody rb;
    private AudioSource audioSource;

    private float stateTimer = 0f;

    private bool playerInAggro = false;
    private Vector3 initialPosition;

    // патрульные цели
    private Vector3 patrolTarget;
    private float nextPatrolTargetTime;

    private enum AIState { Idle, Patrol, Windup, Dash, Recover }
    [SerializeField] private AIState state = AIState.Idle;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = dragNormal;

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f; // 3D звук

        initialPosition = transform.position;

        if (patrolCenter == null)
            patrolCenter = transform; // центр патруля по умолчанию = стартовая позиция
    }

    private void Start()
    {
        // автонахождение игрока / контроллера
        if (player == null)
        {
            playerController = FindObjectOfType<SimpleController_ZeroG>();
            if (playerController != null)
            {
                player = playerController.transform;
            }
            else
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null)
                {
                    player = p.transform;
                    playerController = p.GetComponent<SimpleController_ZeroG>();
                }
            }
        }
        else
        {
            playerController = player.GetComponent<SimpleController_ZeroG>();
        }

        if (player == null)
        {
            Debug.LogWarning($"{name}: EnemyChimeraDashAI не нашёл игрока ни по SimpleController_ZeroG, ни по тегу Player.");
        }

        if (enableIdleState && startInIdle)
            SwitchState(AIState.Idle);
        else
            SwitchState(AIState.Patrol);
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
            case AIState.Idle:   IdleBehavior();   break;
            case AIState.Patrol: PatrolBehavior(); break;
            case AIState.Windup: WindupBehavior(); break;
            case AIState.Dash:   DashBehavior();   break;
            case AIState.Recover:RecoverBehavior();break;
        }
    }

    // ---------- AGGRO / DEAGGRO ----------
    private void UpdateAggro(float distToPlayer)
    {
        if (!playerInAggro && distToPlayer <= detectionRadius)
        {
            playerInAggro = true;
            if (state == AIState.Idle || state == AIState.Patrol)
                SwitchState(AIState.Windup);
        }
        else if (playerInAggro && distToPlayer >= loseTargetRadius)
        {
            playerInAggro = false;

            if (state != AIState.Dash && state != AIState.Windup)
                GoBackToNonAggroState();
        }
    }

    private void GoBackToNonAggroState()
    {
        if (enableIdleState)
            SwitchState(AIState.Idle);
        else
            SwitchState(AIState.Patrol);
    }

    // ---------- IDLE ----------
    private void IdleBehavior()
    {
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 3f);

        if (stateTimer <= 0f)
            SwitchState(AIState.Patrol);
    }

    // ---------- PATROL ----------
    private void PatrolBehavior()
    {
        if (playerInAggro)
            return;

        float dt = Time.fixedDeltaTime;

        transform.position = Vector3.MoveTowards(
            transform.position,
            patrolTarget,
            patrolSpeed * dt
        );

        Vector3 toTarget = patrolTarget - transform.position;
        if (toTarget.sqrMagnitude > 0.0001f)
        {
            Vector3 dir = toTarget.normalized;
            Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                look,
                dt * lookRotateSpeed
            );
        }

        if (Time.time >= nextPatrolTargetTime ||
            Vector3.Distance(transform.position, patrolTarget) < patrolTargetReachDistance)
        {
            PickNewPatrolTarget();
        }

        float distFromCenter = Vector3.Distance(patrolCenter.position, transform.position);
        if (distFromCenter > patrolOuterRadius * 1.5f)
        {
            Vector3 dir = (transform.position - patrolCenter.position).normalized;
            transform.position = patrolCenter.position + dir * patrolOuterRadius;
            PickNewPatrolTarget();
        }
    }

    private void PickNewPatrolTarget()
    {
        if (patrolCenter == null)
        {
            patrolTarget = transform.position;
            return;
        }

        Vector3 dir = Random.onUnitSphere;
        dir.Normalize();

        float r = Random.Range(patrolInnerRadius, patrolOuterRadius);
        patrolTarget = patrolCenter.position + dir * r;

        float t = Random.Range(patrolTargetChangeInterval.x, patrolTargetChangeInterval.y);
        nextPatrolTargetTime = Time.time + t;
    }

    // ---------- WINDUP ----------
    private void WindupBehavior()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            look,
            Time.fixedDeltaTime * 6f
        );

        rb.linearVelocity = Vector3.Lerp(
            rb.linearVelocity,
            Vector3.zero,
            Time.fixedDeltaTime * 4f
        );

        if (stateTimer <= 0f)
            PerformDash();
    }

    // ---------- DASH ----------
    private void DashBehavior()
    {
        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;

        if (stateTimer <= 0f)
            SwitchState(AIState.Recover);
    }

    // ---------- RECOVER ----------
    private void RecoverBehavior()
    {
        if (stateTimer <= 0f)
        {
            if (playerInAggro)
                SwitchState(AIState.Windup);
            else
                GoBackToNonAggroState();
        }
    }

    // ---------- Выполнение рывка ----------
    private void PerformDash()
    {
        if (player == null)
            return;

        Vector3 dir = (player.position - transform.position).normalized;

        rb.linearDamping = dragDuringDash;
        rb.AddForce(dir * dashForce, ForceMode.VelocityChange);

        // звук прыжка
        PlayClip(dashClip, dashVolume);

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
                stateTimer = (customTime > 0 ? customTime : idleDuration);
                break;

            case AIState.Patrol:
                rb.linearDamping = dragNormal;
                stateTimer = (customTime > 0 ? customTime : 9999f);
                PickNewPatrolTarget();
                break;

            case AIState.Windup:
                rb.linearDamping = dragNormal;
                stateTimer = (customTime > 0 ? customTime : windupTime);

                // звук подготовки
                PlayClip(windupClip, windupVolume);
                break;

            case AIState.Dash:
                stateTimer = (customTime > 0 ? customTime : dashDuration);
                break;

            case AIState.Recover:
                rb.linearDamping = dragNormal;
                stateTimer = (customTime > 0 ? customTime : recoveryTime);
                break;
        }
    }

    // ---------- АУДИО-ХЕЛПЕР ----------
    private void PlayClip(AudioClip clip, float volume)
    {
        if (clip == null || audioSource == null || volume <= 0f)
            return;

        float basePitch = 1f;
        float jitter = Random.Range(-randomPitchJitter, randomPitchJitter);
        audioSource.pitch = basePitch + jitter;

        audioSource.PlayOneShot(clip, volume);
    }

    // ---------- КОЛЛИЗИИ: смерть игрока ----------
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
            player.GetComponent<DeathMenu>().Die();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            player.GetComponent<DeathMenu>().Die();
    }
}
