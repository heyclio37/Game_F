using System;
using UnityEngine;

public static class NoiseSystem
{
    public static event Action<Vector3> OnNoiseMade;

    public static void MakeNoise(Vector3 position)
    {
        OnNoiseMade?.Invoke(position);
    }
}