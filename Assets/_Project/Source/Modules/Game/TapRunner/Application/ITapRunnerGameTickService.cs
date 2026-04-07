using LifeCycle.Facade;
using UnityEngine;

namespace TapRunner.Application
{
    /// <summary>Simulation pump for Tap Runner — registered with <see cref="ILifeCycleFacade"/> by the simulation module.</summary>
    public interface ITapRunnerGameTickService : IUpdateHandler
    {
        void AttachWorld(TapRunnerObstacleCoordinator obstacles, Transform playerView);
        void Teardown();
    }
}
