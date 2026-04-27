using UnityEngine;

public class StunnedState : IEnemyState
{
    private float stunTimer;

    public void Enter(EnemyAI enemy)
    {
        stunTimer = 0f;
        enemy.Agent.isStopped = true;
    }

    public void Execute(EnemyAI enemy)
    {
        stunTimer += Time.deltaTime;

        if (stunTimer >= enemy.StunDuration)
        {
            enemy.ChangeToPatrol();
        }
    }

    public void Exit(EnemyAI enemy)
    {
        stunTimer = 0f;
        enemy.Agent.isStopped = false;
    }
}