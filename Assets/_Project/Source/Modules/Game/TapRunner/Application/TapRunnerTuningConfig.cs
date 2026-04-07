using UnityEngine;

namespace TapRunner.Application
{
    /// <summary>Designer-tunable parameters for the Chapter 11 Tap Runner sample.</summary>
    [CreateAssetMenu(fileName = "TapRunnerTuning", menuName = "TapRunner/Tuning Config")]
    public sealed class TapRunnerTuningConfig : ScriptableObject
    {
        [Header("Movement")]
        [Min(0.1f)] public float forwardSpeed = 8f;
        [Min(0.5f)] public float jumpVelocity = 9.5f;
        [Min(1f)] public float gravity = 22f;
        public float laneX = 0f;
        [Min(-10f)] public float groundY = 0f;

        [Header("Player spawn")]
        public float startZ = 0f;
        [Min(0.5f)] public float playerColliderRadius = 0.35f;
        [Min(0.1f)] public float playerColliderHeight = 1.1f;

        [Header("Obstacles")]
        [Min(1f)] public float obstacleSpawnAhead = 52f;
        [Min(2f)] public float obstacleRecycleBehind = 8f;
        [Min(2f)] public float obstacleSpacingMin = 9f;
        [Min(2f)] public float obstacleSpacingMax = 16f;
        [Min(4f)] public float firstObstacleAhead = 14f;
        [Min(0.05f)] public float obstacleHalfExtentsY = 0.55f;
        [Min(0.05f)] public float obstacleHalfExtentsX = 0.45f;
        [Min(0.2f)] public float obstacleHalfExtentsZ = 0.45f;

        [Header("Camera")]
        [Min(0.5f)] public float cameraFollowDistanceZ = -7f;
        [Min(0.5f)] public float cameraHeight = 3.2f;
        [Min(0.01f)] public float cameraSmooth = 10f;

        [Header("Feedback (Chapter 11 polish — no external WAV required)")]
        public bool enableJumpSound = true;
        [Range(0f, 1f)] public float jumpSfxVolume = 0.35f;
        public bool enableHitSound = true;
        [Range(0f, 1f)] public float hitSfxVolume = 0.5f;
        public bool enableCameraShakeOnHit = true;
        [Min(0f)] public float hitShakeStrength = 1.1f;

        [Header("Addressables")]
        public string playerPrefabAddress = TapRunnerAddressKeys.PlayerPrefab;
        public string obstaclePrefabAddress = TapRunnerAddressKeys.ObstaclePrefab;

        public static TapRunnerTuningConfig CreateRuntimeDefault()
        {
            var c = CreateInstance<TapRunnerTuningConfig>();
            c.hideFlags = HideFlags.HideAndDontSave;
            return c;
        }
    }
}
