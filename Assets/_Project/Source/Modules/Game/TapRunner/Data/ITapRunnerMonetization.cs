namespace TapRunner.Data
{
    /// <summary>Interstitial / rewarded / no-ads hook surface (stub in this repo; wire real SDK per store).</summary>
    public interface ITapRunnerMonetization
    {
        bool AdsRemoved { get; }
        void PrepareBetweenRunsInterstitial();
        void MaybeShowBetweenRunsInterstitial();
    }
}
