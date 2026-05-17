using UnityEngine;

public class EnemyStatusBillboard : MonoBehaviour
{
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private Vector3 offset = new Vector3(0, 2.2f, 0);
    [SerializeField] private EnemyAI enemyAI;

    [Header("Icons")]
    [SerializeField] private Sprite patrolIcon;
    [SerializeField] private Sprite chaseIcon;
    [SerializeField] private Sprite searchIcon;
    [SerializeField] private Sprite stunnedIcon;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (enemyAI == null) return;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        transform.position = enemyAI.transform.position + offset;
        transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);

        UpdateIcon();
    }

    private void UpdateIcon()
    {
        string state = enemyAI.CurrentStateName;

        Sprite target = state switch
        {
            "PatrolState" => patrolIcon,
            "ChaseState" => chaseIcon,
            "SearchState" => searchIcon,
            "StunnedState" => stunnedIcon,
            _ => null
        };

        if (iconRenderer.sprite != target)
            iconRenderer.sprite = target;

        iconRenderer.color = new Color(1, 1, 1, state == "PatrolState" ? 0.3f : 1f);
    }
}