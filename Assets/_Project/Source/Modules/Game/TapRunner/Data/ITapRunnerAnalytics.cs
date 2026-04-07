namespace TapRunner.Data
{
    /// <summary>Extension point for Unity Gaming Services / custom pipelines (Chapter 11).</summary>
    public interface ITapRunnerAnalytics
    {
        void LogRunStart(int gamesPlayedBefore);
        void LogRunEnd(int score, int bestScore, bool isNewBest);
        void LogSessionStart();
    }
}
