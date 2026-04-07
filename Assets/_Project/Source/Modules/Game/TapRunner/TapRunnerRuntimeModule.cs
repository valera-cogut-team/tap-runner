using Addressables.Facade;
using Audio.Facade;
using Core;
using Input.Facade;
using LifeCycle.Facade;
using Logger.Facade;
using Pool.Facade;
using Shaker;
using TapRunner.Application;
using TapRunner.Facade;
using TapRunner.Presentation;
using UnityEngine;
using Zenject;

namespace TapRunner
{
    /// <summary>
    /// Tap Runner — Unity runtime integration: frame tick + 3D/HUD presentation under one lifecycle.
    /// Kept as a single <see cref="IModule"/> so <see cref="Disable"/> always stops the tick before tearing down scene objects
    /// (splitting tick and world into separate <c>IModule</c> types breaks safe shutdown under <see cref="ModuleManager.ShutdownAll"/> ordering).
    /// For reuse, depend on <see cref="ITapRunnerGameTickService"/> and <see cref="TapRunnerGameplayRoot"/> from other projects — not on this module type.
    /// </summary>
    public sealed class TapRunnerRuntimeModule : IModule
    {
        public string Name => "TapRunnerRuntime";
        public string Version => "1.0.0";
        public string[] Dependencies => new[]
        {
            "Logger", "Input", "LifeCycle", "Addressables", "Pool", "Audio", "Shaker", "TapRunnerCore"
        };

        public bool IsEnabled { get; private set; }

        private IModuleContext _context;
        private ITapRunnerGameTickService _tick;
        private GameObject _root;

        public void Initialize(IModuleContext context)
        {
            _context = context;
        }

        public void Enable()
        {
            if (IsEnabled)
                return;

            if (_context == null)
                return;

            var logger = _context.GetModuleFacade<ILoggerFacade>();
            var tuning = _context.Container.Resolve<TapRunnerTuningConfig>();
            var facade = _context.Container.Resolve<ITapRunnerFacade>();
            var input = _context.Container.Resolve<IInputFacade>();
            var addressables = _context.GetModuleFacade<IAddressablesFacade>();
            var pool = _context.GetModuleFacade<IPoolFacade>();
            var lifeCycle = _context.GetModuleFacade<ILifeCycleFacade>();
            var audio = _context.GetModuleFacade<IAudioFacade>();
            var shaker = _context.GetModuleFacade<IShakerFacade>();

            if (tuning == null || facade == null || input == null || addressables == null || pool == null || lifeCycle == null)
            {
                logger?.LogError("[TapRunnerRuntime] Missing dependencies — run TapRunnerCoreModule.PrepareContentAsync first.");
                return;
            }

            IsEnabled = true;

            _tick = new TapRunnerGameTickService(input, facade);
            _context.Container.Bind<ITapRunnerGameTickService>().FromInstance(_tick).AsSingle();
            lifeCycle.RegisterUpdateHandler(_tick);

            _root = new GameObject("TapRunnerWorld");
            Object.DontDestroyOnLoad(_root);
            var driver = _root.AddComponent<TapRunnerGameplayRoot>();
            driver.Initialize(_tick, facade, logger, tuning, addressables, pool, lifeCycle, audio, shaker);

            logger?.LogInfo("TapRunnerRuntimeModule enabled");
        }

        public void Disable()
        {
            if (!IsEnabled)
                return;

            IsEnabled = false;

            var lifeCycle = _context?.GetModuleFacade<ILifeCycleFacade>();
            if (_tick != null && lifeCycle != null)
            {
                lifeCycle.UnregisterUpdateHandler(_tick);
                _tick.Teardown();
            }

            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }

            _tick = null;
        }

        public void Shutdown()
        {
            Disable();
            _context = null;
        }
    }
}
