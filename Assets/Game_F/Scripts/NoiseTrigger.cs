using UnityEngine;

public class NoiseTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerRefs>() != null)
        {
            NoiseSystem.MakeNoise(transform.position);
        }
    }
}