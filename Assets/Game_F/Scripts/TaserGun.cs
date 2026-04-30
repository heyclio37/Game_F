using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class TaserGun : NetworkBehaviour
{
    [SerializeField] private Transform shootPoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private LayerMask obstacleMask = Physics.DefaultRaycastLayers;
    [SerializeField] private float checkDistance;

    private readonly SyncVar<bool> hasBullet = new(true);
    private GameObject currentBullet;

    public bool HasBullet => hasBullet.Value;

    [Server]
    public bool TryShoot(Vector3 aimDirection)
    {
        if (!hasBullet.Value) return false;

        if (Physics.Raycast(shootPoint.position, aimDirection, checkDistance, obstacleMask,
                QueryTriggerInteraction.Ignore))
            return false;

        hasBullet.Value = false;

        currentBullet = Instantiate(bulletPrefab, shootPoint.position, Quaternion.LookRotation(aimDirection));
        ServerManager.Spawn(currentBullet);

        Bullet bullet = currentBullet.GetComponent<Bullet>();
        bullet.Initialize(aimDirection, this);

        return true;
    }

    [Server]
    public void OnBulletStuck(GameObject bullet)
    {
        currentBullet = bullet;
    }

    [Server]
    public void RetrieveBullet()
    {
        currentBullet = null;
        hasBullet.Value = true;
    }
}