using Logger.Facade;

namespace TapRunner.Data
{
    public sealed class TapRunnerAnalyticsNull : ITapRunnerAnalytics
    {
        private readonly ILoggerFacade _logger;

        public TapRunnerAnalyticsNull(ILoggerFacade logger)
        {
            _logger = logger;
        }

        public void LogRunStart(int gamesPlayedBefore)
        {
            _logger?.LogInfo($"[TapRunner][Analytics] run_start gamesPlayed={gamesPlayedBefore}");
        }

        public void LogRunEnd(int score, int bestScore, bool isNewBest)
        {
            _logger?.LogInfo($"[TapRunner][Analytics] run_end score={score} best={bestScore} newBest={isNewBest}");
        }

        public void LogSessionStart()
        {
            _logger?.LogInfo("[TapRunner][Analytics] session_start");
        }
    }
}
