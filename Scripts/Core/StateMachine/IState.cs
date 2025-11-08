namespace DeadFrontier.Core
{
    /// <summary>
    /// Interface for state machine states
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// Called when entering the state
        /// </summary>
        void Enter();

        /// <summary>
        /// Called every frame while in this state
        /// </summary>
        void Update();

        /// <summary>
        /// Called when exiting the state
        /// </summary>
        void Exit();
    }
}
