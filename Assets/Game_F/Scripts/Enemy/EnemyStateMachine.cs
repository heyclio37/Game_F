public class EnemyStateMachine
{
    private IEnemyState currentState;

    public IEnemyState CurrentState => currentState;

    public void ChangeState(IEnemyState newState, EnemyAI enemy)
    {
        currentState?.Exit(enemy);
        currentState = newState;
        currentState?.Enter(enemy);
    }

    public void Tick(EnemyAI enemy)
    {
        currentState?.Execute(enemy);
    }
}