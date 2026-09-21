using System;

namespace Farm.Core
{
    /// <summary>
    /// Lightweight, zero-allocation state machine for agent behaviors.
    /// Manages state transitions and dispatches Tick calls.
    /// </summary>
    public sealed class StateMachine<TContext>
    {
        private readonly TContext _context;

        public IState<TContext> CurrentState { get; private set; }

        public event Action<IState<TContext>, IState<TContext>> StateChanged;

        public StateMachine(TContext context, IState<TContext> initialState = null)
        {
            _context = context;
            if (initialState != null)
            {
                ChangeState(initialState);
            }
        }

        public void ChangeState(IState<TContext> nextState)
        {
            if (ReferenceEquals(CurrentState, nextState) || nextState == null) return;

            IState<TContext> previousState = CurrentState;
            CurrentState?.Exit(_context);
            CurrentState = nextState;
            CurrentState.Enter(_context);

            StateChanged?.Invoke(previousState, nextState);
        }

        public void Tick()
        {
            CurrentState?.Tick(_context);
        }
    }
}
