namespace Farm.Core
{
    /// <summary>Represents a single state in a state machine with lifecycle hooks.</summary>
    public interface IState<TContext>
    {
        void Enter(TContext context);
        void Tick(TContext context);
        void Exit(TContext context);
    }
}
