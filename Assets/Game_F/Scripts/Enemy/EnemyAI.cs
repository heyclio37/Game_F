using FishNet.Object;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : NetworkBehaviour
{
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float waitTime = 2f;
    [SerializeField] private float sightRange = 15f;
    [SerializeField] private float sightAngle = 180f;
    [SerializeField] private float hearingRange = 10f;
    [SerializeField] private float stunDuration = 5f;
    [SerializeField] private float catchDistance = 1.5f;
    [SerializeField] private LayerMask obstacleMask;

    public NavMeshAgent Agent { get; private set; }
    public Transform[] PatrolPoints => patrolPoints;
    public float WaitTime => waitTime;
    public float SightRange => sightRange;
    public float SightAngle => sightAngle;
    public float HearingRange => hearingRange;
    public float StunDuration => stunDuration;
    public float CatchDistance => catchDistance;

    private EnemyStateMachine stateMachine;
    private PatrolState patrolState;
    private ChaseState chaseState;
    private SearchState searchState;
    private StunnedState stunnedState;

    private int currentPatrolIndex;
    public Transform CurrentTarget { get; set; }
    public Vector3 LastKnownPosition { get; set; }

    private float lastNoiseTime;
    private float noiseCooldown = 1f;
    private Vector3 lastNoisePosition;

    public int CurrentPatrolIndex
    {
        get => currentPatrolIndex;
        set => currentPatrolIndex = value;
    }

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        stateMachine = new EnemyStateMachine();
        patrolState = new PatrolState();
        chaseState = new ChaseState();
        searchState = new SearchState();
        stunnedState = new StunnedState();
        stateMachine.ChangeState(patrolState, this);

        NoiseSystem.OnNoiseMade += OnNoiseHeard;
    }

    private void OnDestroy()
    {
        NoiseSystem.OnNoiseMade -= OnNoiseHeard;
    }

    private void Update()
    {
        if (!IsServerStarted) return;
        stateMachine.Tick(this);
        Debug.DrawRay(transform.position, transform.forward * sightRange, Color.crimson);
        Debug.Log(stateMachine.CurrentState);
    }

    public Transform DetectPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, sightRange);
        foreach (var hit in hits)
        {
            PlayerRefs player = hit.GetComponentInParent<PlayerRefs>();
            if (player == null) continue;

            PlayerCaptureState captureState = player.GetComponent<PlayerCaptureState>();
            if (captureState != null && captureState.IsCaptured) continue;

            Vector3 playerPosition = player.transform.position;
            Vector3 directionToPlayer = (playerPosition - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToPlayer);

            if (angle > sightAngle / 2f) continue;

            float distance = Vector3.Distance(transform.position, playerPosition);
            if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer, distance, obstacleMask))
                continue;

            return player.transform;
        }

        return null;
    }

    public void OnNoiseHeard(Vector3 noisePosition)
    {
        if (stateMachine.CurrentState is StunnedState) return;
        if (stateMachine.CurrentState is ChaseState) return;

        float timeSinceLastNoise = Time.time - lastNoiseTime;
        if (timeSinceLastNoise < noiseCooldown)
        {
            float oldDistance = Vector3.Distance(transform.position, lastNoisePosition);
            float newDistance = Vector3.Distance(transform.position, noisePosition);
            if (newDistance >= oldDistance) return;
        }

        lastNoiseTime = Time.time;
        lastNoisePosition = noisePosition;
        ChangeToSearch(noisePosition);
    }
    
    [Server]
    public void TryCatchPlayer(Transform target)
    {
        Debug.Log($"[Enemy] TryCatchPlayer target={target?.name}");
        if (GameManager.Instance == null)
        {
            Debug.LogError("[Enemy] GameManager.Instance is null");
            return;
        }
        PlayerCaptureState captureState = target.GetComponent<PlayerCaptureState>();
        if (captureState == null)
        {
            Debug.LogError("[Enemy] target has no PlayerCaptureState");
            return;
        }
        GameManager.Instance.OnPlayerCaught(captureState);
    }

    public void ChangeToChase(Transform target)
    {
        CurrentTarget = target;
        stateMachine.ChangeState(chaseState, this);
    }

    public void ChangeToSearch(Vector3 position)
    {
        LastKnownPosition = position;
        stateMachine.ChangeState(searchState, this);
    }

    public void ChangeToPatrol()
    {
        stateMachine.ChangeState(patrolState, this);
    }

    public void ChangeToStunned()
    {
        stateMachine.ChangeState(stunnedState, this);
    }
}