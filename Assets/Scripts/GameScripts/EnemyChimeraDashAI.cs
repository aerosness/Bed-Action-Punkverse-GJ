using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyChimeraDashAI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Dash Settings")]
    public float windupTime = 0.4f;      // ����� ����� ������
    public float dashForce = 60f;        // ���� �����
    public float dashDuration = 0.6f;    // ��� ����� ������������ �����
    public float recoveryTime = 0.8f;    // ������� ����� �������
    public float maxSpeed = 20f;         // ������������ ��������

    [Header("Smoothing")]
    public float dragDuringDash = 0.5f;  // ������� ����������
    public float dragNormal = 2f;        // ������� ������

    private Rigidbody rb;
    private float stateTimer = 0f;

    private enum AIState { Idle, Windup, Dash, Recover }
    private AIState state = AIState.Idle;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = dragNormal;
    }

    private void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;

        SwitchState(AIState.Windup);
    }

    private void FixedUpdate()
    {
        stateTimer -= Time.fixedDeltaTime;

        switch (state)
        {
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

    private void WindupBehavior()
    {
        // ������������� chimera �� ������
        Vector3 dir = (player.position - transform.position).normalized;
        Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.fixedDeltaTime * 6f);

        if (stateTimer <= 0f)
        {
            PerformDash();
        }
    }

    private void DashBehavior()
    {
        // ����������� ��������
        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;

        if (stateTimer <= 0f)
        {
            SwitchState(AIState.Recover);
        }
    }

    private void RecoverBehavior()
    {
        if (stateTimer <= 0)
        {
            SwitchState(AIState.Windup);
        }
    }

    private void PerformDash()
    {
        Vector3 dir = (player.position - transform.position).normalized;

        // ������� drag (��������� ����� ���� ������)
        rb.linearDamping = dragDuringDash;

        // ��������� ������� �����
        rb.AddForce(dir * dashForce, ForceMode.VelocityChange);

        SwitchState(AIState.Dash, dashDuration);
    }

    private void SwitchState(AIState newState, float customTime = -1f)
    {
        state = newState;

        switch (newState)
        {
            case AIState.Windup:
                rb.linearDamping = dragNormal;
                stateTimer = (customTime > 0 ? customTime : windupTime);
                break;

            case AIState.Dash:
                // ����� �������� ��� ������ PerformDash
                stateTimer = customTime;
                break;

            case AIState.Recover:
                rb.linearDamping = dragNormal;
                stateTimer = (customTime > 0 ? customTime : recoveryTime);
                break;
        }
    }
}
