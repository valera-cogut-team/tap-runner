using System;
using TapRunner.Application;
using TapRunner.Data;
using TapRunner.Domain;
using Storage.Facade;
using UniRx;
using UnityEngine;

namespace TapRunner.Facade
{
    public sealed class TapRunnerFacade : ITapRunnerFacade, IDisposable
    {
        private readonly TapRunnerGameState _state;
        private readonly TapRunnerTuningConfig _tuning;
        private readonly IStorageFacade _storage;
        private readonly ITapRunnerAnalytics _analytics;
        private readonly ITapRunnerMonetization _monetization;

        private readonly Subject<Unit> _jumpPulse = new Subject<Unit>();
        private bool _disposed;

        private readonly ReactiveProperty<TapRunnerGamePhase> _phaseRx;
        private readonly ReactiveProperty<float> _distanceRx;
        private readonly ReactiveProperty<int> _scoreRx;
        private readonly ReactiveProperty<int> _bestScoreRx;
        private readonly ReactiveProperty<Vector3> _playerPosRx;

        public TapRunnerFacade(
            TapRunnerGameState state,
            TapRunnerTuningConfig tuning,
            IStorageFacade storage,
            ITapRunnerAnalytics analytics,
            ITapRunnerMonetization monetization)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _tuning = tuning ?? TapRunnerTuningConfig.CreateRuntimeDefault();
            _storage = storage;
            _analytics = analytics;
            _monetization = monetization;

            _phaseRx = new ReactiveProperty<TapRunnerGamePhase>(_state.Phase);
            _distanceRx = new ReactiveProperty<float>(_state.Distance);
            _scoreRx = new ReactiveProperty<int>(_state.Score);
            _bestScoreRx = new ReactiveProperty<int>(_state.BestScore);
            _playerPosRx = new ReactiveProperty<Vector3>(ComputeWorldPosition());
        }

        public IObservable<Unit> JumpPulse => _jumpPulse;

        public IReadOnlyReactiveProperty<TapRunnerGamePhase> PhaseRx => _phaseRx;
        public IReadOnlyReactiveProperty<float> DistanceRx => _distanceRx;
        public IReadOnlyReactiveProperty<int> ScoreRx => _scoreRx;
        public IReadOnlyReactiveProperty<int> BestScoreRx => _bestScoreRx;
        public IReadOnlyReactiveProperty<Vector3> PlayerPositionRx => _playerPosRx;

        public TapRunnerGamePhase Phase => _state.Phase;
        public float Distance => _state.Distance;
        public int Score => _state.Score;
        public int BestScore => _state.BestScore;
        public Vector3 PlayerWorldPosition => ComputeWorldPosition();
        public float PlayerZ => _state.PlayerZ;

        public void BeginRunFromReady()
        {
            if (_state.Phase != TapRunnerGamePhase.Ready)
                return;
            _state.Phase = TapRunnerGamePhase.Running;
            _state.ForwardSpeed = _tuning.forwardSpeed;
            _analytics?.LogRunStart(_state.GamesPlayed);
            PushPhase();
        }

        public void RequestJump()
        {
            if (_state.Phase != TapRunnerGamePhase.Running)
                return;
            if (!_state.IsGrounded)
                return;
            _state.VelocityY = _tuning.jumpVelocity;
            _state.IsGrounded = false;
            _jumpPulse.OnNext(Unit.Default);
        }

        public void TickSimulation(float deltaTime)
        {
            if (_state.Phase != TapRunnerGamePhase.Running)
                return;

            var dt = Mathf.Max(0f, deltaTime);
            _state.PlayerZ += _state.ForwardSpeed * dt;
            _state.VelocityY -= _tuning.gravity * dt;
            _state.PlayerY += _state.VelocityY * dt;

            var gy = _tuning.groundY;
            if (_state.PlayerY <= gy && _state.VelocityY <= 0f)
            {
                _state.PlayerY = gy;
                _state.VelocityY = 0f;
                _state.IsGrounded = true;
            }

            _state.Distance = Mathf.Max(0f, _state.PlayerZ - _tuning.startZ);
            _state.Score = Mathf.FloorToInt(_state.Distance);

            _playerPosRx.Value = ComputeWorldPosition();
            _distanceRx.Value = _state.Distance;
            _scoreRx.Value = _state.Score;
        }

        public void NotifyPlayerHitObstacle()
        {
            if (_state.Phase != TapRunnerGamePhase.Running)
                return;

            _state.Phase = TapRunnerGamePhase.GameOver;
            _state.ForwardSpeed = 0f;
            PushPhase();

            var prevBest = _state.BestScore;
            if (_state.Score > _state.BestScore)
            {
                _state.BestScore = _state.Score;
                if (_storage != null)
                {
                    _storage.SetInt(TapRunnerPersistenceKeys.BestScore, _state.BestScore);
                    _storage.Save();
                }
            }

            _state.GamesPlayed = Math.Max(0, _state.GamesPlayed) + 1;
            if (_storage != null)
            {
                _storage.SetInt(TapRunnerPersistenceKeys.GamesPlayed, _state.GamesPlayed);
                _storage.Save();
            }

            var newBest = _state.BestScore > prevBest;
            if (newBest)
                _bestScoreRx.Value = _state.BestScore;
            _analytics?.LogRunEnd(_state.Score, _state.BestScore, newBest);
            _monetization?.PrepareBetweenRunsInterstitial();
        }

        public void RestartRun()
        {
            _monetization?.MaybeShowBetweenRunsInterstitial();

            _state.Phase = TapRunnerGamePhase.Running;
            _state.PlayerZ = _tuning.startZ;
            _state.PlayerY = _tuning.groundY;
            _state.VelocityY = 0f;
            _state.ForwardSpeed = _tuning.forwardSpeed;
            _state.Distance = 0f;
            _state.Score = 0;
            _state.IsGrounded = true;

            _phaseRx.Value = _state.Phase;
            _distanceRx.Value = _state.Distance;
            _scoreRx.Value = _state.Score;
            _bestScoreRx.Value = _state.BestScore;
            _playerPosRx.Value = ComputeWorldPosition();

            _analytics?.LogRunStart(_state.GamesPlayed);
        }

        public void BootstrapResetToReady()
        {
            _state.Phase = TapRunnerGamePhase.Ready;
            _state.PlayerZ = _tuning.startZ;
            _state.PlayerY = _tuning.groundY;
            _state.VelocityY = 0f;
            _state.ForwardSpeed = 0f;
            _state.Distance = 0f;
            _state.Score = 0;
            _state.IsGrounded = true;
            PushAll();
        }

        private Vector3 ComputeWorldPosition()
        {
            return new Vector3(_tuning.laneX, _state.PlayerY, _state.PlayerZ);
        }

        private void PushPhase()
        {
            _phaseRx.Value = _state.Phase;
        }

        private void PushAll()
        {
            _phaseRx.Value = _state.Phase;
            _distanceRx.Value = _state.Distance;
            _scoreRx.Value = _state.Score;
            _bestScoreRx.Value = _state.BestScore;
            _playerPosRx.Value = ComputeWorldPosition();
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _jumpPulse?.Dispose();
        }
    }
}
