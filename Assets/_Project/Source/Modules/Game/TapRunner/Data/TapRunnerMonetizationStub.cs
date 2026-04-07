using TapRunner.Application;
using Storage.Facade;

namespace TapRunner.Data
{
    public sealed class TapRunnerMonetizationStub : ITapRunnerMonetization
    {
        private readonly IStorageFacade _storage;

        public TapRunnerMonetizationStub(IStorageFacade storage)
        {
            _storage = storage;
        }

        public bool AdsRemoved =>
            _storage != null && _storage.GetInt(TapRunnerPersistenceKeys.AdsRemoved, 0) != 0;

        public void PrepareBetweenRunsInterstitial()
        {
        }

        public void MaybeShowBetweenRunsInterstitial()
        {
            if (AdsRemoved)
                return;
            // No-op: replace with LevelPlay / AdMob etc.
        }
    }
}
