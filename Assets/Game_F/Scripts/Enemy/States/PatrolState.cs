using UnityEngine;

public class PatrolState : IEnemyState
{
    private float waitTimer;

    public void Enter(EnemyAI enemy)
    {
        waitTimer = 0f;
        enemy.Agent.isStopped = false;
        if (enemy.PatrolPoints.Length > 0)
            enemy.Agent.SetDestination(enemy.PatrolPoints[enemy.CurrentPatrolIndex].position);
    }

    public void Execute(EnemyAI enemy)
    {
        if (enemy.PatrolPoints.Length == 0) return;

        Transform player = enemy.DetectPlayer();
        if (player != null)
        {
            enemy.ChangeToChase(player);
            return;
        }

        if (!enemy.Agent.pathPending && enemy.Agent.remainingDistance < 0.5f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= enemy.WaitTime)
            {
                enemy.CurrentPatrolIndex = (enemy.CurrentPatrolIndex + 1) % enemy.PatrolPoints.Length;
                enemy.Agent.SetDestination(enemy.PatrolPoints[enemy.CurrentPatrolIndex].position);
                waitTimer = 0f;
            }
        }
    }

    public void Exit(EnemyAI enemy)
    {
        waitTimer = 0f;
    }
}