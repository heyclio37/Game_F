using UnityEngine;
public class SpawnPointsHolder : MonoBehaviour
{
    public static SpawnPointsHolder Instance { get; private set; }
    [SerializeField] private Transform[] spawnPoints;
    public Transform[] SpawnPoints => spawnPoints;

    private void Awake() => Instance = this;
}
