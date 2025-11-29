using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SimpleController_ZeroG : MonoBehaviour
{
    [Header("Rotation (body)")]
    [Tooltip("Максимальная скорость поворота по горизонту (град/сек) при зажатой A/D")]
    public float yawMaxSpeed = 180f;
    
    [Tooltip("Максимальная скорость наклона вверх/вниз (град/сек) при зажатой W/S")]
    public float pitchMaxSpeed = 120f;

    [Tooltip("Ускорение набора/сброса скорости поворота (град/сек^2)")]
    public float yawAcceleration   = 720f;
    public float pitchAcceleration = 720f;

    [Header("Thrust")]
    public float thrustForce = 15f;
    [Range(0f, 1f)] public float damping = 0.05f;

    [Header("Input Actions")]
    public InputAction moveAction;   // Vector2: x = A/D, y = W/S
    public InputAction thrustAction; // Button
    public bool IsThrusting => thrustAction.IsPressed(); 

    private Rigidbody rb;

    // ориентация тела
    private float bodyYaw;
    private float bodyPitch;

    // угловые скорости
    private float yawVelocity;
    private float pitchVelocity;

    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity     = false;
        rb.linearDamping  = 0f;
        rb.angularDamping = 0f;
        rb.constraints    = RigidbodyConstraints.FreezeRotation;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void OnEnable()
    {
        moveAction.Enable();
        thrustAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        thrustAction.Disable();
    }

    private void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();

        HandleBodyRotation(moveInput);
        ApplyBodyRotation();

        // ESC — разблокировка курсора
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
            }
        }
    }

    private void FixedUpdate()
    {
        if (thrustAction.IsPressed() && thrustForce > 0f)
        {
            rb.AddForce(transform.forward * thrustForce, ForceMode.Acceleration);
        }

        if (damping > 0f)
        {
            rb.linearVelocity = Vector3.Lerp(
                rb.linearVelocity,
                Vector3.zero,
                damping * Time.fixedDeltaTime
            );
        }
    }

    /// <summary>
    /// Поворот тела с плавным ускорением/торможением по yaw/pitch.
    /// </summary>
    private void HandleBodyRotation(Vector2 move)
    {
        float dt = Time.deltaTime;

        // целевые скорости (в град/сек) от ввода
        float targetYawSpeed   = move.x * yawMaxSpeed;    // A/D
        float targetPitchSpeed = -move.y * pitchMaxSpeed; // W/S (W = наклон вперёд)

        // плавно подгоняем текущую скорость к целевой
        yawVelocity = Mathf.MoveTowards(
            yawVelocity,
            targetYawSpeed,
            yawAcceleration * dt
        );

        pitchVelocity = Mathf.MoveTowards(
            pitchVelocity,
            targetPitchSpeed,
            pitchAcceleration * dt
        );

        // интегрируем углы
        bodyYaw   += yawVelocity * dt;
        bodyPitch += pitchVelocity * dt;

        // нормализуем pitch в [-180;180] (чтобы не улететь в огромные значения)
        bodyPitch = Mathf.Repeat(bodyPitch + 180f, 360f) - 180f;
    }

    private void ApplyBodyRotation()
    {
        transform.rotation = Quaternion.Euler(bodyPitch, bodyYaw, 0f);
    }
}
