using TapRunner.Domain;

namespace TapRunner.Application
{
    public sealed class TapRunnerGameState
    {
        public TapRunnerGamePhase Phase = TapRunnerGamePhase.Ready;
        public float PlayerY;
        public float PlayerZ;
        public float VelocityY;
        public float ForwardSpeed;
        public float Distance;
        public int Score;
        public int BestScore;
        public int GamesPlayed;
        public bool IsGrounded = true;
    }
}
