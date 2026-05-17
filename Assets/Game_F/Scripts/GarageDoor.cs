using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class GarageDoor : NetworkBehaviour
{
    [Header("Movement")]
    [Tooltip("Объект двери, который будет двигаться (можно сам этот объект)")]
    [SerializeField] private Transform doorTransform;

    [Tooltip("Локальная Y-позиция двери когда полностью закрыта")]
    [SerializeField] private float closedLocalY = 0f;

    [Tooltip("Локальная Y-позиция двери когда полностью открыта")]
    [SerializeField] private float openedLocalY = -3f;

    [Header("Speed")]
    [Tooltip("Скорость опускания (в единицах прогресса в секунду, 1.0 = полностью за 1 сек)")]
    [SerializeField] private float openSpeed = 0.3f;

    [Tooltip("Скорость подъёма обратно когда рычаги не нажаты")]
    [SerializeField] private float closeSpeed = 0f;

    [Header("Lever Count")]
    [Tooltip("Сколько рычагов должны быть активны одновременно для опускания")]
    [SerializeField] private int requiredActiveLevers = 2;

    private readonly SyncVar<float> openProgress = new(0f);
    private readonly HashSet<Lever> activeLevers = new();

    private bool networkReady = false;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        networkReady = true;
        ApplyDoorPosition(openProgress.Value);
        openProgress.OnChange += OnProgressChanged;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        networkReady = false;
        openProgress.OnChange -= OnProgressChanged;
    }

    private void OnProgressChanged(float oldValue, float newValue, bool asServer)
    {
        ApplyDoorPosition(newValue);
    }

    private void ApplyDoorPosition(float progress)
    {
        if (doorTransform == null) return;

        Vector3 pos = doorTransform.localPosition;
        pos.y = Mathf.Lerp(closedLocalY, openedLocalY, progress);
        doorTransform.localPosition = pos;
    }

    [Server]
    public void RegisterLeverActive(Lever lever)
    {
        activeLevers.Add(lever);
    }

    [Server]
    public void RegisterLeverInactive(Lever lever)
    {
        activeLevers.Remove(lever);
    }

    private void Update()
    {
        if (!networkReady) return;
        if (!IsServerStarted) return;

        bool allActive = activeLevers.Count >= requiredActiveLevers;

        if (allActive)
        {
            if (openProgress.Value < 1f)
            {
                float newValue = Mathf.Min(1f, openProgress.Value + openSpeed * Time.deltaTime);
                openProgress.Value = newValue;
            }
        }
        else
        {
            if (closeSpeed > 0f && openProgress.Value > 0f)
            {
                float newValue = Mathf.Max(0f, openProgress.Value - closeSpeed * Time.deltaTime);
                openProgress.Value = newValue;
            }
        }
    }
}