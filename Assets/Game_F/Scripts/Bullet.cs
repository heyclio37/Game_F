using FishNet.Object;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float bulletRadius;

    private Vector3 velocity;

    private NetworkObject ownerGunNetObj;

    private NetworkObject stuckTarget;
    private Vector3 hitNormal;
    private Vector3 hitPoint;

    private bool isStuck;

    public void Initialize(Vector3 direction, TaserGun owner)
    {
        velocity = direction.normalized * speed;


        if (IsServerStarted && owner != null)
            ownerGunNetObj = owner.NetworkObject;
    }

    private void Update()
    {

        if (IsServerStarted && !isStuck)
        {
            float step = velocity.magnitude * Time.deltaTime;

            if (Physics.SphereCast(
                    transform.position,
                    bulletRadius,
                    velocity.normalized,
                    out RaycastHit hit,
                    step + bulletRadius,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
            {
                OnHit(hit);
                return;
            }

            transform.position += velocity * Time.deltaTime;
        }

        if (isStuck)
        {
            StickVisual();
        }
    }

    [Server]
    private void OnHit(RaycastHit hit)
    {
        isStuck = true;

        hitPoint = hit.point;
        hitNormal = hit.normal;
        stuckTarget = hit.collider.GetComponentInParent<NetworkObject>();

        EnemyAI enemy = hit.collider.GetComponentInParent<EnemyAI>();
        if (enemy != null)
        {
            enemy.ChangeToStunned();
        }

        StuckObserversRpc(stuckTarget, hit.point, hit.normal);
    }

    private void StickVisual()
    {
        if (stuckTarget != null)
        {
            Collider col = stuckTarget.GetComponentInChildren<Collider>();

            Vector3 closest = col != null
                ? col.ClosestPoint(transform.position)
                : stuckTarget.transform.position;

            Vector3 dir = transform.position - closest;

            if (dir.sqrMagnitude < 0.0001f)
                dir = hitNormal;
            else
                dir.Normalize();

            transform.position = closest + dir * bulletRadius;
        }
        else
        {
            transform.position = hitPoint + hitNormal * bulletRadius;
        }
    }

    [ObserversRpc]
    private void StuckObserversRpc(NetworkObject target, Vector3 point, Vector3 normal)
    {
        stuckTarget = target;
        hitPoint = point;
        hitNormal = normal;
        isStuck = true;
    }

    public TaserGun GetOwnerGun()
    {
        if (ownerGunNetObj == null)
            return null;

        return ownerGunNetObj.GetComponent<TaserGun>();
    }
}