using System;
using Addressables.Facade;
using Audio.Facade;
using Shaker;
using Cysharp.Threading.Tasks;
using LifeCycle.Facade;
using Logger.Facade;
using Pool.Facade;
using TapRunner.Application;
using TapRunner.Facade;
using UnityEngine;

namespace TapRunner.Presentation
{
    /// <summary>Loads Tap Runner 3D content via Addressables; wires pool, HUD, camera follow (Chapter 11).</summary>
    public sealed class TapRunnerGameplayRoot : MonoBehaviour
    {
        private ITapRunnerGameTickService _tick;
        private ITapRunnerFacade _facade;
        private ILoggerFacade _logger;
        private TapRunnerTuningConfig _tuning;
        private IAddressablesFacade _addressables;
        private IPoolFacade _pool;
        private ILifeCycleFacade _lifeCycle;
        private IAudioFacade _audio;
        private IShakerFacade _shaker;

        private TapRunnerObstacleCoordinator _coordinator;
        private GameObject _obstacleTemplate;
        private TapRunnerCameraFollow _camFollow;
        private TapRunnerRunFeedback _runFeedback;

        public void Initialize(
            ITapRunnerGameTickService tick,
            ITapRunnerFacade facade,
            ILoggerFacade logger,
            TapRunnerTuningConfig tuning,
            IAddressablesFacade addressables,
            IPoolFacade pool,
            ILifeCycleFacade lifeCycle,
            IAudioFacade audio = null,
            IShakerFacade shaker = null)
        {
            _tick = tick;
            _facade = facade;
            _logger = logger;
            _tuning = tuning ?? TapRunnerTuningConfig.CreateRuntimeDefault();
            _addressables = addressables;
            _pool = pool;
            _lifeCycle = lifeCycle;
            _audio = audio;
            _shaker = shaker;

            BuildWorldAsync().Forget();
        }

        private async UniTaskVoid BuildWorldAsync()
        {
            if (_addressables == null || _facade == null || _tick == null)
            {
                _logger?.LogError("[TapRunner] Gameplay root: missing dependencies.");
                return;
            }

            _facade.BootstrapResetToReady();

            EnsureDefaultWorldLighting();

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(transform, false);
            ground.transform.localPosition = new Vector3(_tuning.laneX, _tuning.groundY - 0.05f, 120f);
            ground.transform.localScale = new Vector3(8f, 1f, 40f);

            GameObject playerGo;
            try
            {
                var playerPrefab = await _addressables.LoadPrefabAsync(_tuning.playerPrefabAddress);
                if (playerPrefab != null)
                    playerGo = Instantiate(playerPrefab, transform);
                else
                    playerGo = BuildFallbackPlayer();

                try
                {
                    await _addressables.ReleaseAssetAsync(_tuning.playerPrefabAddress);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"[TapRunner] Release player prefab: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"[TapRunner] Player prefab load failed, using fallback: {ex.Message}");
                playerGo = BuildFallbackPlayer();
            }

            playerGo.name = "Player";
            playerGo.transform.position = _facade.PlayerWorldPosition;

            EnsurePlayerCollisions(playerGo);

            GameObject obstacleSource;
            try
            {
                obstacleSource = await _addressables.LoadPrefabAsync(_tuning.obstaclePrefabAddress);
                if (obstacleSource == null)
                    obstacleSource = BuildFallbackObstaclePrefab();
                else
                    obstacleSource = Instantiate(obstacleSource);

                try
                {
                    await _addressables.ReleaseAssetAsync(_tuning.obstaclePrefabAddress);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"[TapRunner] Release obstacle prefab: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"[TapRunner] Obstacle prefab load failed, using fallback: {ex.Message}");
                obstacleSource = BuildFallbackObstaclePrefab();
            }

            obstacleSource.name = "ObstacleTemplate";
            obstacleSource.SetActive(false);
            obstacleSource.transform.SetParent(transform, false);
            _obstacleTemplate = obstacleSource;

            var obstacleParent = new GameObject("Obstacles").transform;
            obstacleParent.SetParent(transform, false);
            obstacleParent.localPosition = Vector3.zero;

            _coordinator = new TapRunnerObstacleCoordinator(_tuning, _facade, _logger, obstacleParent);
            _coordinator.InitializePool(_pool, _obstacleTemplate);

            _tick.AttachWorld(_coordinator, playerGo.transform);

            var camTr = Camera.main != null ? Camera.main.transform : CreateRuntimeCamera().transform;
            _camFollow = camTr.gameObject.GetComponent<TapRunnerCameraFollow>();
            if (_camFollow == null)
                _camFollow = camTr.gameObject.AddComponent<TapRunnerCameraFollow>();
            _camFollow.Initialize(camTr, playerGo.transform, _tuning);
            _lifeCycle?.RegisterLateUpdateHandler(_camFollow);

            var hudGo = new GameObject("HudRoot");
            hudGo.transform.SetParent(transform, false);
            var hud = hudGo.AddComponent<TapRunnerHud>();
            hud.Initialize(_facade);

            var feedbackGo = new GameObject("RunFeedback");
            feedbackGo.transform.SetParent(transform, false);
            _runFeedback = feedbackGo.AddComponent<TapRunnerRunFeedback>();
            _runFeedback.Initialize(_facade, _audio, _shaker, camTr, _tuning);

            var hit = playerGo.GetComponent<TapRunnerPlayerHitProxy>();
            if (hit == null)
                hit = playerGo.AddComponent<TapRunnerPlayerHitProxy>();
            hit.Initialize(_facade);

            _facade.BeginRunFromReady();
        }

        private void OnDestroy()
        {
            if (_lifeCycle != null && _camFollow != null)
            {
                _lifeCycle.UnregisterLateUpdateHandler(_camFollow);
                Destroy(_camFollow);
                _camFollow = null;
            }

            if (_obstacleTemplate != null)
            {
                Destroy(_obstacleTemplate);
                _obstacleTemplate = null;
            }
        }

        private GameObject BuildFallbackPlayer()
        {
            var go = new GameObject("Player_Fallback");
            go.transform.SetParent(transform, false);
            var vis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            vis.name = "Capsule";
            vis.transform.SetParent(go.transform, false);
            vis.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            Destroy(vis.GetComponent<Collider>());

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var cap = go.AddComponent<CapsuleCollider>();
            cap.isTrigger = true;
            cap.radius = _tuning.playerColliderRadius;
            cap.height = _tuning.playerColliderHeight;
            cap.center = new Vector3(0f, _tuning.playerColliderHeight * 0.5f, 0f);

            return go;
        }

        private GameObject BuildFallbackObstaclePrefab()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "ObstacleBlock";
            go.transform.localScale = new Vector3(
                _tuning.obstacleHalfExtentsX * 2f,
                _tuning.obstacleHalfExtentsY * 2f,
                _tuning.obstacleHalfExtentsZ * 2f);

            var col = go.GetComponent<BoxCollider>();
            if (col != null)
                col.isTrigger = true;

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            if (go.GetComponent<TapRunnerObstacleView>() == null)
                go.AddComponent<TapRunnerObstacleView>();

            return go;
        }

        private void EnsurePlayerCollisions(GameObject playerGo)
        {
            var rb = playerGo.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = playerGo.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            else
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            var trigger = playerGo.GetComponentInChildren<CapsuleCollider>();
            if (trigger == null)
                trigger = playerGo.AddComponent<CapsuleCollider>();
            trigger.isTrigger = true;
            trigger.radius = _tuning.playerColliderRadius;
            trigger.height = _tuning.playerColliderHeight;
            trigger.center = new Vector3(0f, _tuning.playerColliderHeight * 0.5f, 0f);
        }

        private Camera CreateRuntimeCamera()
        {
            var camGo = new GameObject("TapRunnerCamera");
            camGo.transform.SetParent(transform, false);
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            return cam;
        }

        /// <summary>
        /// BootstrapScene has no lights; URP still needs a directional light for readable primitives.
        /// </summary>
        private void EnsureDefaultWorldLighting()
        {
            if (FindObjectsByType<Light>(FindObjectsSortMode.None).Length > 0)
                return;

            var lightGo = new GameObject("TapRunnerSun");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.98f, 0.95f);
            light.intensity = 1.05f;
            light.shadows = LightShadows.Soft;
        }
    }
}
