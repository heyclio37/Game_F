using TMPro;
using UnityEngine;

public class GameResultRow : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject youMarker; 

    public void Setup(GameResultEntry entry, bool isLocal)
    {
        if (nameText != null)
            nameText.text = entry.PlayerName + (isLocal ? " (You)" : "");

        if (statusText != null)
        {
            if (entry.Escaped)
            {
                statusText.text = "Escape";
                statusText.color = new Color(0.3f, 0.8f, 0.3f);
            }
            else if (entry.Captured)
            {
                statusText.text = "Caught";
                statusText.color = new Color(0.8f, 0.3f, 0.3f);
            }
            else
            {
                statusText.text = "—";
            }
        }

        if (youMarker != null)
            youMarker.SetActive(isLocal);
    }
}