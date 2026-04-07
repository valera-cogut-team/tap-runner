using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Addressables;
using Core;
using Storage;
using Input;
using LifeCycle;
using Logger;
using Logger.Facade;
using Pool;
using StateMachine;
using Timer;
using Audio;
using Effects;
using Shaker;
using ScreenRouter;
using UIPopup;
using UIRoot;
using SplashScreen;
using GameScreen;
using AppFlow;
using AppFlow.Facade;
using TapRunner;
using Zenject;

namespace Bootstrap
{
    public sealed class AppBootstrap
    {
        private readonly DiContainer _container;
        private readonly bool _enableDebugLogs;

        private ModuleManager _moduleManager;
        private IModuleContext _moduleContext;

        public IModuleContext ModuleContext => _moduleContext;
        public ModuleManager ModuleManager => _moduleManager;

        public AppBootstrap(DiContainer container, bool enableDebugLogs)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _enableDebugLogs = enableDebugLogs;
        }

        public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            await InitializeCoreInfrastructureAsync(cancellationToken);
            await InitializeCoreModulesAsync(cancellationToken);
            await InitializeGameModulesAsync(cancellationToken);
            await InitializeUIModulesAsync(cancellationToken);
            await FinalizeInitializationAsync(cancellationToken);
        }

        private async UniTask InitializeCoreInfrastructureAsync(CancellationToken cancellationToken)
        {
            _moduleManager = new ModuleManager(_container);
            _moduleContext = _moduleManager.Context;

            var loggerModule = new LoggerModule();
            loggerModule.Initialize(_moduleContext);
            loggerModule.Enable();
            _moduleManager.RegisterModule(loggerModule);

            _container.Bind<ModuleManager>().FromInstance(_moduleManager).AsSingle();

            await UniTask.Yield(cancellationToken);
        }

        private async UniTask InitializeCoreModulesAsync(CancellationToken cancellationToken)
        {
            var logger = _moduleContext.GetModuleFacade<ILoggerFacade>();
            logger?.LogInfo(_enableDebugLogs
                ? "TapRunner: Initializing core modules..."
                : "TapRunner: Core modules...");

            var lifeCycle = new LifeCycleModule();
            lifeCycle.Initialize(_moduleContext);
            lifeCycle.Enable();
            _moduleManager.RegisterModule(lifeCycle);

            var input = new InputModule();
            input.Initialize(_moduleContext);
            input.Enable();
            _moduleManager.RegisterModule(input);

            var addressables = new AddressablesModule();
            addressables.Initialize(_moduleContext);
            addressables.Enable();
            _moduleManager.RegisterModule(addressables);

            var timer = new TimerModule();
            timer.Initialize(_moduleContext);
            timer.Enable();
            _moduleManager.RegisterModule(timer);

            var storage = new StorageModule();
            storage.Initialize(_moduleContext);
            storage.Enable();
            _moduleManager.RegisterModule(storage);

            var stateMachine = new StateMachineModule();
            stateMachine.Initialize(_moduleContext);
            stateMachine.Enable();
            _moduleManager.RegisterModule(stateMachine);

            var pool = new PoolModule();
            pool.Initialize(_moduleContext);
            pool.Enable();
            _moduleManager.RegisterModule(pool);

            var audio = new AudioModule();
            audio.Initialize(_moduleContext);
            audio.Enable();
            _moduleManager.RegisterModule(audio);

            var effects = new EffectsModule();
            effects.Initialize(_moduleContext);
            effects.Enable();
            _moduleManager.RegisterModule(effects);

            var shaker = new ShakerModule();
            shaker.Initialize(_moduleContext);
            shaker.Enable();
            _moduleManager.RegisterModule(shaker);

            await UniTask.Yield(cancellationToken);
        }

        private async UniTask InitializeGameModulesAsync(CancellationToken cancellationToken)
        {
            var logger = _moduleContext.GetModuleFacade<ILoggerFacade>();
            logger?.LogInfo("TapRunner: game modules (Core → Runtime)...");

            var tapCore = new TapRunnerCoreModule();
            tapCore.Initialize(_moduleContext);
            await tapCore.PrepareContentAsync(cancellationToken);
            tapCore.Enable();
            _moduleManager.RegisterModule(tapCore);

            var tapRuntime = new TapRunnerRuntimeModule();
            tapRuntime.Initialize(_moduleContext);
            tapRuntime.Enable();
            _moduleManager.RegisterModule(tapRuntime);

            await UniTask.Yield(cancellationToken);
        }

        private async UniTask InitializeUIModulesAsync(CancellationToken cancellationToken)
        {
            var logger = _moduleContext.GetModuleFacade<ILoggerFacade>();
            logger?.LogInfo("TapRunner: UI modules...");

            var uiRoot = new UIRootModule();
            uiRoot.Initialize(_moduleContext);
            uiRoot.Enable();
            _moduleManager.RegisterModule(uiRoot);

            var uiPopup = new UIPopupModule();
            uiPopup.Initialize(_moduleContext);
            uiPopup.Enable();
            _moduleManager.RegisterModule(uiPopup);

            var screenRouter = new ScreenRouterModule();
            screenRouter.Initialize(_moduleContext);
            screenRouter.Enable();
            _moduleManager.RegisterModule(screenRouter);

            var splashScreen = new SplashScreenModule();
            splashScreen.Initialize(_moduleContext);
            splashScreen.Enable();
            _moduleManager.RegisterModule(splashScreen);

            var gameScreen = new GameScreenModule();
            gameScreen.Initialize(_moduleContext);
            gameScreen.Enable();
            _moduleManager.RegisterModule(gameScreen);

            var appFlow = new AppFlowModule();
            appFlow.Initialize(_moduleContext);
            appFlow.Enable();
            _moduleManager.RegisterModule(appFlow);

            await UniTask.Yield(cancellationToken);
        }

        private async UniTask FinalizeInitializationAsync(CancellationToken cancellationToken)
        {
            var logger = _moduleContext.GetModuleFacade<ILoggerFacade>();
            logger?.LogInfo("TapRunner: Bootstrap finalized");

            try
            {
                var appFlow = _moduleContext.GetModuleFacade<IAppFlowFacade>();
                if (appFlow != null)
                {
                    await appFlow.StartAsync(cancellationToken);
                    logger?.LogInfo("TapRunner: AppFlow started");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"TapRunner: Failed to start AppFlow: {ex.Message}", ex);
            }

            await UniTask.Yield(cancellationToken);
        }

        public void Shutdown()
        {
            _moduleContext = null;
            _moduleManager = null;
        }
    }
}
