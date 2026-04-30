using UnityEngine;

public class ChaseState : IEnemyState
{
    private float loseTargetTimer;
    private float loseTargetDuration = 3f;

    public void Enter(EnemyAI enemy)
    {
        loseTargetTimer = 0f;
        enemy.Agent.isStopped = false;
    }

    public void Execute(EnemyAI enemy)
    {
        if (enemy.CurrentTarget == null)
        {
            enemy.ChangeToSearch(enemy.LastKnownPosition);
            return;
        }

        float distanceToTarget = Vector3.Distance(enemy.transform.position, enemy.CurrentTarget.position);

        if (distanceToTarget <= enemy.CatchDistance)
        {
            Debug.Log($"[ChaseState] Within catch distance, catching {enemy.CurrentTarget.name}");
            enemy.TryCatchPlayer(enemy.CurrentTarget);
            enemy.CurrentTarget = null;
            enemy.ChangeToPatrol();
            return;
        }

        Transform player = enemy.DetectPlayer();
        if (player != null)
        {
            loseTargetTimer = 0f;
        }
        else
        {
            loseTargetTimer += Time.deltaTime;

            if (loseTargetTimer >= loseTargetDuration)
            {
                enemy.ChangeToSearch(enemy.LastKnownPosition);
                return;
            }
        }

        enemy.LastKnownPosition = enemy.CurrentTarget.position;
        enemy.Agent.SetDestination(enemy.CurrentTarget.position);
    }

    public void Exit(EnemyAI enemy)
    {
        loseTargetTimer = 0f;
    }
}