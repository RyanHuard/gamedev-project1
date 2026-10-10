using System.Collections.Generic;

namespace HollowTournament.Core
{
    /// <summary>
    /// Minimal reusable FSM. States are POCOs so they can be unit tested
    /// without touching MonoBehaviour/Animator at all.
    /// </summary>
    public class StateMachine<TOwner>
    {
        public IState<TOwner> Current { get; private set; }
        public IState<TOwner> Previous { get; private set; }

        private readonly TOwner _owner;
        private readonly Dictionary<System.Type, IState<TOwner>> _states = new();

        public StateMachine(TOwner owner)
        {
            _owner = owner;
        }

        public void Register(IState<TOwner> state)
        {
            _states[state.GetType()] = state;
        }

        public T Get<T>() where T : IState<TOwner>
        {
            return (T)_states[typeof(T)];
        }

        public void ChangeState<T>() where T : IState<TOwner>
        {
            ChangeState(_states[typeof(T)]);
        }

        public void ChangeState(IState<TOwner> next)
        {
            if (next == null || next == Current) return;
            Current?.Exit(_owner);
            Previous = Current;
            Current = next;
            Current.Enter(_owner);
        }

        public void Tick(float deltaTime)
        {
            Current?.Tick(_owner, deltaTime);
        }
    }

    public interface IState<TOwner>
    {
        void Enter(TOwner owner);
        void Tick(TOwner owner, float deltaTime);
        void Exit(TOwner owner);
    }
}
