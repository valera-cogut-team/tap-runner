using System;
using TapRunner.Domain;
using UniRx;
using UnityEngine;

namespace TapRunner.Facade
{
    public interface ITapRunnerFacade : ITapRunnerState
    {
        /// <summary>Emitted when an accepted jump starts (presentation: SFX).</summary>
        IObservable<Unit> JumpPulse { get; }

        IReadOnlyReactiveProperty<TapRunnerGamePhase> PhaseRx { get; }
        IReadOnlyReactiveProperty<float> DistanceRx { get; }
        IReadOnlyReactiveProperty<int> ScoreRx { get; }
        IReadOnlyReactiveProperty<int> BestScoreRx { get; }
        IReadOnlyReactiveProperty<Vector3> PlayerPositionRx { get; }

        /// <summary>Reset run state to Ready (speed 0) before world/scene wiring completes.</summary>
        void BootstrapResetToReady();

        void BeginRunFromReady();
        void RequestJump();
        void TickSimulation(float deltaTime);
        void NotifyPlayerHitObstacle();
        void RestartRun();
    }
}
