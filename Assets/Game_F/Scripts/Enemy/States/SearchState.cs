using UnityEngine;

public class SearchState : IEnemyState
{
    private float searchTimer;
    private float searchDuration = 1f;
    private bool reachedDestination;

    public void Enter(EnemyAI enemy)
    {
        searchTimer = 0f;
        reachedDestination = false;
        enemy.Agent.isStopped = false;
        enemy.Agent.SetDestination(enemy.LastKnownPosition);
    }

    public void Execute(EnemyAI enemy)
    {
        Transform player = enemy.DetectPlayer();
        if (player != null)
        {
            enemy.ChangeToChase(player);
            return;
        }

        if (!reachedDestination)
        {
            if (!enemy.Agent.pathPending && enemy.Agent.remainingDistance < 0.5f)
                reachedDestination = true;
        }
        else
        {
            searchTimer += Time.deltaTime;
            if (searchTimer >= searchDuration)
                enemy.ChangeToPatrol();
        }
    }

    public void Exit(EnemyAI enemy)
    {
        searchTimer = 0f;
        reachedDestination = false;
    }
}