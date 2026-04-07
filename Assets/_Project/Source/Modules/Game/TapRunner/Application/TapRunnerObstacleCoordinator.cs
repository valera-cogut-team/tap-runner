using System.Collections.Generic;
using Logger.Facade;
using Pool.Application;
using Pool.Facade;
using TapRunner.Domain;
using TapRunner.Facade;
using TapRunner.Presentation;
using UnityEngine;

namespace TapRunner.Application
{
    public sealed class TapRunnerObstacleCoordinator
    {
        private readonly TapRunnerTuningConfig _tuning;
        private readonly ITapRunnerFacade _facade;
        private readonly ILoggerFacade _logger;
        private readonly Transform _parent;
        private IObjectPool<PooledObstacleHandle> _pool;
        private GameObject _template;

        private readonly List<ActiveSlot> _active = new List<ActiveSlot>(32);
        private float _nextSpawnZ;
        private bool _initialized;

        private struct ActiveSlot
        {
            public PooledObstacleHandle Handle;
            public float Z;
        }

        public TapRunnerObstacleCoordinator(
            TapRunnerTuningConfig tuning,
            ITapRunnerFacade facade,
            ILoggerFacade logger,
            Transform parent)
        {
            _tuning = tuning ?? TapRunnerTuningConfig.CreateRuntimeDefault();
            _facade = facade;
            _logger = logger;
            _parent = parent;
        }

        public void InitializePool(IPoolFacade poolFacade, GameObject obstacleTemplate)
        {
            if (_initialized)
                return;
            _template = obstacleTemplate;
            if (poolFacade == null || _template == null)
            {
                _logger?.LogError("[TapRunner] Obstacle coordinator: pool or template is null.");
                return;
            }

            if (poolFacade.GetPool<PooledObstacleHandle>(TapRunnerPoolIds.Obstacle) == null)
            {
                poolFacade.CreatePool(
                    TapRunnerPoolIds.Obstacle,
                    () => CreateHandle(),
                    6,
                    32);
            }

            _pool = poolFacade.GetPool<PooledObstacleHandle>(TapRunnerPoolIds.Obstacle);
            _initialized = _pool != null;
            ResetSpawnCursor(_facade.PlayerZ);
        }

        public void ResetForNewRun(float playerZ)
        {
            ReturnAllActive();
            ResetSpawnCursor(playerZ);
        }

        public void Teardown()
        {
            ReturnAllActive();
            _initialized = false;
            _pool = null;
            _template = null;
        }

        public void Tick(float playerZ, TapRunnerGamePhase phase)
        {
            if (!_initialized || _pool == null || phase != TapRunnerGamePhase.Running)
                return;

            for (var i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i].Z < playerZ - _tuning.obstacleRecycleBehind)
                {
                    var h = _active[i].Handle;
                    _pool.Return(h);
                    _active.RemoveAt(i);
                }
            }

            var horizon = playerZ + _tuning.obstacleSpawnAhead;
            while (_nextSpawnZ < horizon && _active.Count < 31)
            {
                SpawnOne(_nextSpawnZ);
                var gap = UnityEngine.Random.Range(_tuning.obstacleSpacingMin, _tuning.obstacleSpacingMax);
                _nextSpawnZ += gap;
            }
        }

        private void ResetSpawnCursor(float playerZ)
        {
            _nextSpawnZ = playerZ + _tuning.firstObstacleAhead;
        }

        private void ReturnAllActive()
        {
            if (_pool == null)
            {
                _active.Clear();
                return;
            }

            for (var i = 0; i < _active.Count; i++)
                _pool.Return(_active[i].Handle);
            _active.Clear();
        }

        private void SpawnOne(float zWorld)
        {
            var h = _pool.Get();
            if (h?.View == null)
                return;

            var t = h.View.transform;
            t.SetParent(_parent, worldPositionStays: false);
            var y = _tuning.groundY + _tuning.obstacleHalfExtentsY;
            t.localPosition = new Vector3(_tuning.laneX, y, zWorld);
            t.localRotation = Quaternion.identity;
            h.View.gameObject.SetActive(true);
            _active.Add(new ActiveSlot { Handle = h, Z = zWorld });
        }

        private PooledObstacleHandle CreateHandle()
        {
            var go = UnityEngine.Object.Instantiate(_template, _parent, false);
            go.name = "Obstacle (pooled)";
            go.SetActive(false);
            var view = go.GetComponent<TapRunnerObstacleView>();
            if (view == null)
                view = go.AddComponent<TapRunnerObstacleView>();
            return new PooledObstacleHandle(view);
        }
    }
}
