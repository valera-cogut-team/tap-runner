using Input.Domain;
using Input.Facade;
using LifeCycle.Facade;
using TapRunner.Domain;
using TapRunner.Facade;
using UnityEngine;

namespace TapRunner.Application
{
    /// <summary>Single per-frame gameplay pump for Tap Runner (Chapter 8 / 14 — central IUpdateHandler).</summary>
    public sealed class TapRunnerGameTickService : ITapRunnerGameTickService
    {
        private readonly IInputFacade _input;
        private readonly ITapRunnerFacade _facade;
        private TapRunnerObstacleCoordinator _obstacles;
        private Transform _playerView;

        public TapRunnerGameTickService(
            IInputFacade input,
            ITapRunnerFacade facade)
        {
            _input = input;
            _facade = facade;
        }

        public void AttachWorld(TapRunnerObstacleCoordinator obstacles, Transform playerView)
        {
            _obstacles = obstacles;
            _playerView = playerView;
        }

        public void Teardown()
        {
            _obstacles?.Teardown();
            _obstacles = null;
            _playerView = null;
        }

        public void OnUpdate(float deltaTime)
        {
            if (_facade == null)
                return;

            var jumpDown = PollJumpDown();
            if (jumpDown)
            {
                if (_facade.Phase == TapRunnerGamePhase.GameOver)
                {
                    _facade.RestartRun();
                    _obstacles?.ResetForNewRun(_facade.PlayerZ);
                }
                else if (_facade.Phase == TapRunnerGamePhase.Running)
                {
                    _facade.RequestJump();
                }
            }

            _facade.TickSimulation(deltaTime);

            if (_playerView != null)
                _playerView.position = _facade.PlayerWorldPosition;

            _obstacles?.Tick(_facade.PlayerZ, _facade.Phase);
        }

        private bool PollJumpDown()
        {
            if (_input.GetButtonDown("Fire1"))
                return true;

            if (_input.TouchCount > 0)
            {
                _input.GetTouch(0, out _, out _, out var phase);
                return phase == InputTouchPhase.Began;
            }

            return false;
        }
    }
}
