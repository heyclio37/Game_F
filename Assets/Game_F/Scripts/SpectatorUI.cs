using TMPro;
using UnityEngine;

public class SpectatorUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text targetNameText;

    private SpectatorView trackedSpectator;
    private float checkTimer;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        if (GameResultUI.Instance != null && GameResultUI.Instance.IsShown)
        {
            if (panel != null && panel.activeSelf)
                panel.SetActive(false);
            return;
        }

        checkTimer -= Time.deltaTime;
        if (checkTimer > 0f) return;
        checkTimer = 0.5f;

        if (trackedSpectator != null && trackedSpectator.IsActive) return;

        SpectatorView[] all = FindObjectsByType<SpectatorView>(FindObjectsSortMode.None);
        foreach (var s in all)
        {
            if (s.IsActive)
            {
                AttachTo(s);
                return;
            }
        }

        if (panel != null && panel.activeSelf)
            panel.SetActive(false);
    }

    private void AttachTo(SpectatorView spectator)
    {
        if (trackedSpectator != null)
            trackedSpectator.OnTargetChanged -= UpdateText;

        trackedSpectator = spectator;
        trackedSpectator.OnTargetChanged += UpdateText;

        if (panel != null) panel.SetActive(true);
        UpdateText(trackedSpectator.CurrentTargetName);
    }

    private void UpdateText(string name)
    {
        if (targetNameText == null) return;

        if (string.IsNullOrEmpty(name))
            targetNameText.text = "Wait...";
        else
            targetNameText.text = $"Playing: {name}";
    }

    private void OnDestroy()
    {
        if (trackedSpectator != null)
            trackedSpectator.OnTargetChanged -= UpdateText;
    }
}