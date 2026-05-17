public class EnemyStateMachine
{
    private IEnemyState currentState;

    public IEnemyState CurrentState => currentState;

    public void ChangeState(IEnemyState newState, EnemyAI enemy)
    {
        currentState?.Exit(enemy);
        currentState = newState;
        currentState?.Enter(enemy);

        if (newState != null)
            enemy.SetStateName(newState.GetType().Name);
    }

    public void Tick(EnemyAI enemy)
    {
        currentState?.Execute(enemy);
    }
}