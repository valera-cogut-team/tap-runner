using LifeCycle.Facade;
using TapRunner.Application;
using UnityEngine;

namespace TapRunner.Presentation
{
    /// <summary>Camera follow via ILateUpdateHandler (Chapter 14 — avoid per-component Update spread).</summary>
    public sealed class TapRunnerCameraFollow : MonoBehaviour, ILateUpdateHandler
    {
        private Transform _cameraTransform;
        private Transform _target;
        private TapRunnerTuningConfig _tuning;
        private Vector3 _velocity;

        public void Initialize(Transform cameraTransform, Transform target, TapRunnerTuningConfig tuning)
        {
            _cameraTransform = cameraTransform;
            _target = target;
            _tuning = tuning ?? TapRunnerTuningConfig.CreateRuntimeDefault();
            _velocity = Vector3.zero;
        }

        public void OnLateUpdate(float deltaTime)
        {
            if (_cameraTransform == null || _target == null || _tuning == null)
                return;

            var t = _target.position;
            var desired = new Vector3(
                _tuning.laneX,
                t.y + _tuning.cameraHeight,
                t.z + _tuning.cameraFollowDistanceZ);

            _cameraTransform.position = Vector3.SmoothDamp(
                _cameraTransform.position,
                desired,
                ref _velocity,
                1f / Mathf.Max(0.01f, _tuning.cameraSmooth),
                Mathf.Infinity,
                deltaTime);

            _cameraTransform.LookAt(t + Vector3.up * 0.85f);
        }
    }
}
