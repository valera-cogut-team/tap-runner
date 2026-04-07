using Audio.Facade;
using Shaker;
using TapRunner.Application;
using TapRunner.Domain;
using TapRunner.Facade;
using UniRx;
using UnityEngine;

namespace TapRunner.Presentation
{
    /// <summary>Jump / game-over feedback via Core Audio + Shaker (Chapter 11 — polish without asset dependencies).</summary>
    public sealed class TapRunnerRunFeedback : MonoBehaviour
    {
        private static AudioClip _jumpClip;
        private static AudioClip _hitClip;

        private ITapRunnerFacade _facade;
        private IAudioFacade _audio;
        private IShakerFacade _shaker;
        private TapRunnerTuningConfig _tuning;
        private CompositeDisposable _dsp;
        private TapRunnerGamePhase _prevPhase = TapRunnerGamePhase.Ready;

        public void Initialize(
            ITapRunnerFacade facade,
            IAudioFacade audio,
            IShakerFacade shaker,
            Transform cameraTransform,
            TapRunnerTuningConfig tuning)
        {
            _facade = facade;
            _audio = audio;
            _shaker = shaker;
            _tuning = tuning ?? TapRunnerTuningConfig.CreateRuntimeDefault();

            _shaker?.SetTarget(cameraTransform);

            _dsp?.Dispose();
            _dsp = new CompositeDisposable();

            if (_facade == null)
                return;

            _facade.JumpPulse.Subscribe(_ => PlayJump()).AddTo(_dsp);

            _facade.PhaseRx.Subscribe(p =>
            {
                if (_prevPhase == TapRunnerGamePhase.Running && p == TapRunnerGamePhase.GameOver)
                    PlayGameOver();
                _prevPhase = p;
            }).AddTo(_dsp);
        }

        private void OnDestroy()
        {
            _dsp?.Dispose();
            _dsp = null;
            _shaker?.SetTarget(null);
        }

        private void PlayJump()
        {
            if (_audio == null || _tuning == null || !_tuning.enableJumpSound)
                return;
            EnsureClips();
            _audio.PlaySound2D(_jumpClip, _tuning.jumpSfxVolume);
        }

        private void PlayGameOver()
        {
            if (_tuning == null)
                return;
            if (_audio != null && _tuning.enableHitSound)
            {
                EnsureClips();
                _audio.PlaySound2D(_hitClip, _tuning.hitSfxVolume);
            }

            if (_shaker != null && _tuning.enableCameraShakeOnHit)
                _shaker.AddImpulse(_tuning.hitShakeStrength);
        }

        private static void EnsureClips()
        {
            if (_jumpClip == null)
                _jumpClip = BuildToneClip("TapRunnerJump", 520f, 0.06f, 0.28f);
            if (_hitClip == null)
                _hitClip = BuildNoiseBurstClip("TapRunnerHit", 0.14f, 0.35f);
        }

        private static AudioClip BuildToneClip(string name, float freqHz, float duration, float gain)
        {
            var sampleRate = 24000;
            var n = Mathf.Max(64, Mathf.CeilToInt(sampleRate * duration));
            var data = new float[n];
            for (var i = 0; i < n; i++)
            {
                var t = i / (float)sampleRate;
                var env = Mathf.Exp(-t * 28f);
                data[i] = Mathf.Sin(2f * Mathf.PI * freqHz * t) * env * gain;
            }

            var clip = AudioClip.Create(name, n, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildNoiseBurstClip(string name, float duration, float gain)
        {
            var sampleRate = 24000;
            var n = Mathf.Max(64, Mathf.CeilToInt(sampleRate * duration));
            var data = new float[n];
            var rng = new System.Random(4242);
            for (var i = 0; i < n; i++)
            {
                var t = i / (float)n;
                var env = Mathf.Sin(Mathf.PI * t);
                data[i] = ((float)rng.NextDouble() * 2f - 1f) * env * gain;
            }

            var clip = AudioClip.Create(name, n, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
