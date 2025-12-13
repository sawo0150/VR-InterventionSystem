using UnityEngine;
using VRInterventionSystem.Audio;

namespace VRInterventionSystem.Audio
{
    /// <summary>
    /// Attach this component to rolling boulder prefabs to play collision sounds.
    /// Plays a looping sound when the boulder collides with objects.
    /// Automatically creates and configures an AudioSource component.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RollingBoulderSound : MonoBehaviour
    {
        [Header("Sound Settings")]
        [Tooltip("Play sound when colliding with objects")]
        [SerializeField] private bool playOnCollision = true;

        [Tooltip("Minimum collision velocity to trigger sound (prevents sound on tiny bumps)")]
        [Range(0f, 10f)]
        [SerializeField] private float minCollisionVelocity = 0.5f;

        private AudioSource audioSource;
        private float lastCollisionTime = 0f;
        private bool isColliding = false;

        void Start()
        {
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

            audioSource.clip = config.boulderRollingLoop;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.volume = config.boulderRollingVolume;
            audioSource.spatialBlend = config.boulderSpatialBlend;
            audioSource.maxDistance = config.boulderMaxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
        }

        /// <summary>
        /// Called when boulder enters collision with another object
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            if (!playOnCollision || audioSource == null) return;

            // Check if collision velocity is strong enough
            float collisionVelocity = collision.relativeVelocity.magnitude;
            if (collisionVelocity < minCollisionVelocity) return;

            // Check cooldown to prevent sound spam
            var config = SoundManager.Instance?.GetAudioConfig();
            float cooldown = config != null ? config.boulderCollisionCooldown : 0.2f;

            if (Time.time - lastCollisionTime < cooldown) return;

            lastCollisionTime = Time.time;
            isColliding = true;

            // Start playing the rolling sound
            if (!audioSource.isPlaying && audioSource.clip != null)
            {
                audioSource.Play();
            }
        }

        /// <summary>
        /// Called while boulder continues to collide with objects
        /// </summary>
        private void OnCollisionStay(Collision collision)
        {
            // Keep the sound playing while colliding
            isColliding = true;

            if (audioSource != null && !audioSource.isPlaying && audioSource.clip != null)
            {
                audioSource.Play();
            }
        }

        /// <summary>
        /// Called when boulder stops colliding with an object
        /// </summary>
        private void OnCollisionExit(Collision collision)
        {
            isColliding = false;

            // Stop sound after a short delay (allows for continuous rolling sound)
            Invoke(nameof(CheckAndStopSound), 0.1f);
        }

        /// <summary>
        /// Checks if boulder is still colliding and stops sound if not
        /// </summary>
        private void CheckAndStopSound()
        {
            if (!isColliding && audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }

        /// <summary>
        /// Clean up on destroy
        /// </summary>
        private void OnDestroy()
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }
}
