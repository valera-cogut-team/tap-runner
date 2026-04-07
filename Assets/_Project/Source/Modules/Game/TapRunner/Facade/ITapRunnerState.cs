using TapRunner.Domain;
using UnityEngine;

namespace TapRunner.Facade
{
    public interface ITapRunnerState
    {
        TapRunnerGamePhase Phase { get; }
        float Distance { get; }
        int Score { get; }
        int BestScore { get; }
        Vector3 PlayerWorldPosition { get; }
        float PlayerZ { get; }
    }
}
