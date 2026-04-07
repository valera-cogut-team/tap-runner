using System;
using System.Threading;
using Addressables.Facade;
using Cysharp.Threading.Tasks;
using Core;
using Logger.Facade;
using Storage.Facade;
using TapRunner.Application;
using TapRunner.Data;
using TapRunner.Facade;
using UnityEngine;
using Zenject;

namespace TapRunner
{
    /// <summary>
    /// Tap Runner — domain + application wiring: tuning, persistence hydration, public facade.
    /// Single responsibility: game rules state and DI surface; no frame loop, no scene objects.
    /// </summary>
    public sealed class TapRunnerCoreModule : IModule
    {
        public string Name => "TapRunnerCore";
        public string Version => "1.0.0";
        public string[] Dependencies => new[] { "Logger", "Storage", "Addressables" };
        public bool IsEnabled { get; private set; }

        private IModuleContext _context;
        private TapRunnerGameState _state;
        private TapRunnerTuningConfig _tuning;
        private TapRunnerFacade _facade;
        private ITapRunnerAnalytics _analytics;
        private IAddressablesFacade _addressables;
        private bool _contentReady;

        public void Initialize(IModuleContext context)
        {
            _context = context;
        }

        public async UniTask PrepareContentAsync(CancellationToken cancellationToken = default)
        {
            if (_contentReady)
                return;

            if (_context == null)
                throw new InvalidOperationException("TapRunnerCoreModule.Initialize must run before PrepareContentAsync.");

            _addressables = _context.GetModuleFacade<IAddressablesFacade>()
                            ?? throw new InvalidOperationException("IAddressablesFacade is not registered.");

            var logger = _context.GetModuleFacade<ILoggerFacade>();

            try
            {
                _tuning = await _addressables.LoadAssetAsync<TapRunnerTuningConfig>(TapRunnerAddressKeys.Config)
                    .AttachExternalCancellation(cancellationToken);
            }
            catch (Exception ex)
            {
                logger?.LogError($"[TapRunner] Failed to load '{TapRunnerAddressKeys.Config}': {ex.Message}", ex);
                _tuning = TapRunnerTuningConfig.CreateRuntimeDefault();
            }

            if (_tuning == null)
                _tuning = TapRunnerTuningConfig.CreateRuntimeDefault();

            _context.Container.Bind<TapRunnerTuningConfig>().FromInstance(_tuning).AsSingle();

            _state = new TapRunnerGameState();
            var storage = _context.GetModuleFacade<IStorageFacade>();
            if (storage != null)
            {
                _state.BestScore = Mathf.Max(0, storage.GetInt(TapRunnerPersistenceKeys.BestScore, 0));
                _state.GamesPlayed = Mathf.Max(0, storage.GetInt(TapRunnerPersistenceKeys.GamesPlayed, 0));
            }

            _analytics = new TapRunnerAnalyticsNull(logger);
            var monetization = new TapRunnerMonetizationStub(storage);

            _facade = new TapRunnerFacade(_state, _tuning, storage, _analytics, monetization);
            _context.Container.Bind<ITapRunnerFacade>().FromInstance(_facade).AsSingle();
            _context.Container.Bind<ITapRunnerState>().FromInstance(_facade).AsSingle();

            _contentReady = true;
        }

        public void Enable()
        {
            if (IsEnabled)
                return;

            if (!_contentReady || _facade == null)
            {
                _context?.GetModuleFacade<ILoggerFacade>()?.LogError("[TapRunnerCore] Enable called before PrepareContentAsync.");
                return;
            }

            IsEnabled = true;
            _analytics?.LogSessionStart();
            _context?.GetModuleFacade<ILoggerFacade>()?.LogInfo("TapRunnerCoreModule enabled");
        }

        public void Disable()
        {
            if (!IsEnabled)
                return;

            IsEnabled = false;
            _context?.GetModuleFacade<ILoggerFacade>()?.LogInfo("TapRunnerCoreModule disabled");
        }

        public void Shutdown()
        {
            Disable();

            if (_addressables != null && _contentReady)
                _addressables.ReleaseAssetAsync(TapRunnerAddressKeys.Config).Forget();

            if (_facade is IDisposable d)
                d.Dispose();

            _facade = null;
            _state = null;
            _tuning = null;
            _contentReady = false;
            _addressables = null;
            _analytics = null;
        }
    }
}
