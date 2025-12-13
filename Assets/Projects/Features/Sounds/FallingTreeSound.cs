using UnityEngine;
using VRInterventionSystem.Audio;
using Project.Event;

namespace VRInterventionSystem.Audio
{
    /// <summary>
    /// Attach this component to falling tree objects (Event 2) to play falling and impact sounds.
    /// Works with the FallingDown component to detect when trees start falling.
    /// Plays a sound when the tree starts falling and another when it impacts the ground.
    /// Automatically creates and configures an AudioSource component.
    /// </summary>
    [RequireComponent(typeof(FallingDown))]
    public class FallingTreeSound : MonoBehaviour
    {
        [Header("Sound Settings")]
        [Tooltip("Play sound when tree starts falling")]
        [SerializeField] private bool playFallSound = true;

        [Tooltip("Play sound when tree impacts ground")]
        [SerializeField] private bool playImpactSound = true;

        private AudioSource audioSource;
        private FallingDown fallingDown;
        private bool hasFallen = false;
        private bool hasPlayedImpact = false;

        void Start()
        {
            // Get FallingDown component
            fallingDown = GetComponent<FallingDown>();

            // Get or create AudioSource component
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Configure AudioSource for 3D spatial audio
            InitializeAudioSource();
        }

        /// <summary>
        /// Initialize AudioSource with settings from AudioConfig
        /// </summary>
        private void InitializeAudioSource()
        {
            if (audioSource == null || SoundManager.Instance == null) return;

            var config = SoundManager.Instance.GetAudioConfig();
            if (config == null) return;

            // Configure for one-shot sounds (not looping)
            audioSource.loop = false;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = config.treeSpatialBlend;
            audioSource.maxDistance = config.treeMaxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
        }

        void Update()
        {
            // Check if tree has started falling
            if (!hasFallen && fallingDown != null)
            {
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    // Tree has started falling (isKinematic was set to false)
                    OnTreeStartFalling();
                    hasFallen = true;
                }
            }
        }

        /// <summary>
        /// Called when tree starts falling
        /// </summary>
        private void OnTreeStartFalling()
        {
            var config = SoundManager.Instance?.GetAudioConfig();
            if (config == null) return;

            // Play fall sound
            if (playFallSound && audioSource != null && config.treeFallSound != null)
            {
                audioSource.PlayOneShot(config.treeFallSound, config.treeFallVolume);

                if (Debug.isDebugBuild)
                {
                    Debug.Log($"[FallingTreeSound] {gameObject.name} started falling - playing fall sound");
                }
            }

            // Schedule impact sound to play after delay
            if (playImpactSound && !hasPlayedImpact && audioSource != null && config.treeImpactSound != null)
            {
                Invoke(nameof(PlayImpactSound), config.treeImpactDelay);
            }
        }

        /// <summary>
        /// Plays the impact sound after a delay
        /// </summary>
        private void PlayImpactSound()
        {
            if (hasPlayedImpact || audioSource == null) return;

            var config = SoundManager.Instance?.GetAudioConfig();
            if (config == null || config.treeImpactSound == null) return;

            // Play impact sound
            audioSource.PlayOneShot(config.treeImpactSound, config.treeImpactVolume);
            hasPlayedImpact = true;

            Debug.Log($"[FallingTreeSound] {gameObject.name} impacted - playing impact sound");
        }

        /// <summary>
        /// Public method to manually trigger fall sound (if needed)
        /// </summary>
        public void PlayFallSound()
        {
            if (hasFallen) return; // Already played
            OnTreeStartFalling();
            hasFallen = true;
        }

        /// <summary>
        /// Reset state (useful if tree is reset/respawned)
        /// </summary>
        public void ResetState()
        {
            hasFallen = false;
            hasPlayedImpact = false;
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }
}
