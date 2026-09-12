using System;
using UnityEngine;

namespace Gunbound.Core
{
    /// <summary>
    /// Persistent Singleton Audio Manager for Gunbound Mobile.
    /// Manages sound effect playback for shooting, charging, explosions, and orbital Thor beams.
    /// Provides procedural synth audio fallback generation if AudioClips are not assigned in the Inspector.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Clips")]
        [SerializeField] private AudioClip _fireClip;
        [SerializeField] private AudioClip _fireClipShot1;
        [SerializeField] private AudioClip _fireClipShot2;
        [SerializeField] private AudioClip _fireClipSS;
        [SerializeField] private AudioClip _chargeClip;
        [SerializeField] private AudioClip _impactClip;
        [SerializeField] private AudioClip _thorBeamClip;
        [SerializeField] private AudioClip _clickClip;
        [SerializeField] private AudioClip _itemClip;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource _sfxAudioSource;
        [SerializeField] private AudioSource _chargeAudioSource;

        [Header("Volume Settings")]
        [Range(0f, 1f)] [SerializeField] private float _sfxVolume = 0.8f;
        [Range(0f, 1f)] [SerializeField] private float _chargeVolume = 0.6f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
            EnsureFallbackAudioClips();
        }

        private void InitializeAudioSources()
        {
            if (_sfxAudioSource == null)
            {
                _sfxAudioSource = gameObject.AddComponent<AudioSource>();
                _sfxAudioSource.playOnAwake = false;
            }

            if (_chargeAudioSource == null)
            {
                _chargeAudioSource = gameObject.AddComponent<AudioSource>();
                _chargeAudioSource.playOnAwake = false;
                _chargeAudioSource.loop = true;
            }
        }

        private void EnsureFallbackAudioClips()
        {
            if (_fireClip == null)
            {
                _fireClip = CreateSyntheticClip("Synth_Fire_Base", 0.15f, 440f, 110f, true);
            }
            if (_fireClipShot1 == null)
            {
                _fireClipShot1 = CreateSyntheticClip("Synth_Fire_Shot1", 0.15f, 480f, 120f, true);
            }
            if (_fireClipShot2 == null)
            {
                _fireClipShot2 = CreateSyntheticClip("Synth_Fire_Shot2", 0.22f, 320f, 80f, true);
            }
            if (_fireClipSS == null)
            {
                _fireClipSS = CreateSyntheticClip("Synth_Fire_SS", 0.35f, 220f, 40f, true);
            }
            if (_chargeClip == null)
            {
                _chargeClip = CreateSyntheticClip("Synth_Charge", 0.5f, 220f, 880f, false);
            }
            if (_impactClip == null)
            {
                _impactClip = CreateSyntheticClip("Synth_Impact", 0.3f, 150f, 40f, true);
            }
            if (_thorBeamClip == null)
            {
                _thorBeamClip = CreateSyntheticClip("Synth_ThorBeam", 0.4f, 880f, 220f, true);
            }
            if (_clickClip == null)
            {
                _clickClip = CreateSyntheticClip("Synth_Click", 0.05f, 1200f, 600f, false);
            }
            if (_itemClip == null)
            {
                _itemClip = CreateSyntheticClip("Synth_Item", 0.25f, 523.25f, 1046.5f, false);
            }
        }

        /// <summary>
        /// Plays projectile fire SFX based on specific weapon type.
        /// </summary>
        public void PlayFireSFX(Gunbound.Player.WeaponType weapon, Vector3 position = default)
        {
            AudioClip targetClip = _fireClip;
            if (weapon == Gunbound.Player.WeaponType.Shot1 && _fireClipShot1 != null) targetClip = _fireClipShot1;
            else if (weapon == Gunbound.Player.WeaponType.Shot2 && _fireClipShot2 != null) targetClip = _fireClipShot2;
            else if (weapon == Gunbound.Player.WeaponType.SS && _fireClipSS != null) targetClip = _fireClipSS;

            if (targetClip != null)
            {
                PlayClipAtPointOr2D(targetClip, position, _sfxVolume);
            }
        }

        /// <summary>
        /// Plays default projectile fire SFX.
        /// </summary>
        public void PlayFireSFX(Vector3 position = default)
        {
            PlayFireSFX(Gunbound.Player.WeaponType.Shot1, position);
        }

        /// <summary>
        /// Starts playing looping charge SFX.
        /// </summary>
        public void StartChargeSFX()
        {
            if (_chargeAudioSource == null || _chargeClip == null) return;
            if (_chargeAudioSource.isPlaying) return;

            _chargeAudioSource.clip = _chargeClip;
            _chargeAudioSource.volume = _chargeVolume;
            _chargeAudioSource.pitch = 1.0f;
            _chargeAudioSource.Play();
        }

        /// <summary>
        /// Updates pitch of charge SFX in real time based on power ratio (0.0 to 1.0).
        /// </summary>
        public void UpdateChargePitch(float powerRatio)
        {
            if (_chargeAudioSource != null && _chargeAudioSource.isPlaying)
            {
                _chargeAudioSource.pitch = Mathf.Lerp(0.9f, 1.8f, Mathf.Clamp01(powerRatio));
            }
        }

        /// <summary>
        /// Stops looping charge SFX and resets pitch.
        /// </summary>
        public void StopChargeSFX()
        {
            if (_chargeAudioSource != null && _chargeAudioSource.isPlaying)
            {
                _chargeAudioSource.Stop();
                _chargeAudioSource.pitch = 1.0f;
            }
        }

        /// <summary>
        /// Plays UI button click SFX.
        /// </summary>
        public void PlayClickSFX()
        {
            if (_clickClip == null) return;
            PlayClipAtPointOr2D(_clickClip, default, _sfxVolume * 0.7f);
        }

        /// <summary>
        /// Plays item activation chime SFX.
        /// </summary>
        public void PlayItemSFX()
        {
            if (_itemClip == null) return;
            PlayClipAtPointOr2D(_itemClip, default, _sfxVolume * 0.9f);
        }

        /// <summary>
        /// Plays impact/explosion SFX at specified position.
        /// </summary>
        public void PlayImpactSFX(Vector3 position = default)
        {
            if (_impactClip == null) return;
            PlayClipAtPointOr2D(_impactClip, position, _sfxVolume);
        }

        /// <summary>
        /// Plays Thor orbital beam strike SFX.
        /// </summary>
        public void PlayThorBeamSFX(Vector3 position = default)
        {
            if (_thorBeamClip == null) return;
            PlayClipAtPointOr2D(_thorBeamClip, position, _sfxVolume);
        }

        private void PlayClipAtPointOr2D(AudioClip clip, Vector3 position, float volume)
        {
            if (position != default && Camera.main != null)
            {
                AudioSource.PlayClipAtPoint(clip, position, volume);
            }
            else if (_sfxAudioSource != null)
            {
                _sfxAudioSource.PlayOneShot(clip, volume);
            }
        }

        /// <summary>
        /// Procedurally generates synthetic sound clips for immediate testing without asset files.
        /// </summary>
        private AudioClip CreateSyntheticClip(string name, float duration, float startFreq, float endFreq, bool addNoise)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            System.Random rand = new System.Random();

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float currentFreq = Mathf.Lerp(startFreq, endFreq, progress);
                float tone = Mathf.Sin(2f * Mathf.PI * currentFreq * t);
                float noise = addNoise ? ((float)rand.NextDouble() * 2f - 1f) * (1f - progress) : 0f;
                float envelope = Mathf.Sin(progress * Mathf.PI);

                samples[i] = Mathf.Clamp((tone * 0.7f + noise * 0.3f) * envelope, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
