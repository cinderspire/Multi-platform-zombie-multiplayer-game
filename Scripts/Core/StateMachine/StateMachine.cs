using UnityEngine;

namespace DeadFrontier.Core
{
    /// <summary>
    /// Generic state machine for managing state transitions
    /// </summary>
    public class StateMachine
    {
        private IState currentState;
        private IState previousState;

        public IState CurrentState => currentState;
        public IState PreviousState => previousState;

        /// <summary>
        /// Changes to a new state
        /// </summary>
        public void ChangeState(IState newState)
        {
            if (newState == null)
            {
                Debug.LogWarning("[StateMachine] Attempted to change to null state");
                return;
            }

            if (currentState == newState)
            {
                return; // Already in this state
            }

            previousState = currentState;
            currentState?.Exit();
            currentState = newState;
            currentState.Enter();
        }

        /// <summary>
        /// Updates the current state
        /// </summary>
        public void Update()
        {
            currentState?.Update();
        }

        /// <summary>
        /// Returns to the previous state
        /// </summary>
        public void RevertToPreviousState()
        {
            if (previousState != null)
            {
                ChangeState(previousState);
            }
            else
            {
                Debug.LogWarning("[StateMachine] No previous state to revert to");
            }
        }

        /// <summary>
        /// Checks if currently in a specific state
        /// </summary>
        public bool IsInState<T>() where T : IState
        {
            return currentState is T;
        }

        /// <summary>
        /// Gets the current state as a specific type
        /// </summary>
        public T GetCurrentState<T>() where T : IState
        {
            return (T)currentState;
        }
    }
}
