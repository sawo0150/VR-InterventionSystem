using UnityEngine;
using Project;

namespace VRInterventionSystem.Audio
{
    public class SoundManager : MonoBehaviour
    {
        private static SoundManager _instance;
        public static SoundManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SoundManager>();
                    if (_instance == null)
                    {
                        Debug.LogWarning("SoundManager instance not found in scene. Audio will not play.");
                    }
                }
                return _instance;
            }
        }

        [Header("Configuration")]
        [SerializeField] private AudioConfig audioConfig;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource engineAudioSource;
        [SerializeField] private AudioSource uiAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        // Engine sound smoothing
        private float currentEnginePitch = 1f;
        private float enginePitchVelocity = 0f;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeAudioSources();
        }

        private void InitializeAudioSources()
        {
            if (audioConfig == null)
            {
                Debug.LogError("AudioConfig is not assigned to SoundManager!");
                return;
            }

            // Setup engine audio source
            if (engineAudioSource != null)
            {
                engineAudioSource.clip = audioConfig.engineLoop;
                engineAudioSource.loop = true;
                engineAudioSource.volume = audioConfig.engineBaseVolume;
                engineAudioSource.pitch = audioConfig.engineMinPitch;
                engineAudioSource.spatialBlend = audioConfig.engineSpatialBlend;
                engineAudioSource.maxDistance = audioConfig.engineMaxDistance;
                engineAudioSource.rolloffMode = AudioRolloffMode.Linear;
            }

            // Setup UI audio source
            if (uiAudioSource != null)
            {
                uiAudioSource.spatialBlend = audioConfig.uiSpatialBlend;
                uiAudioSource.volume = audioConfig.uiSoundVolume;
            }

            // Setup SFX audio source
            if (sfxAudioSource != null)
            {
                sfxAudioSource.volume = audioConfig.collisionVolume;
            }
        }

        /// <summary>
        /// Update engine sound based on robot speed (0 = stopped, 1 = max speed)
        /// </summary>
        public void UpdateEngineSound(float speedNormalized, bool isMoving)
        {
            if (engineAudioSource == null || audioConfig == null) return;

            // Start or stop the engine sound
            if (isMoving && !engineAudioSource.isPlaying)
            {
                engineAudioSource.Play();
            }
            else if (!isMoving && engineAudioSource.isPlaying)
            {
                engineAudioSource.Stop();
            }

            // Smoothly adjust pitch based on speed
            if (isMoving)
            {
                float targetPitch = Mathf.Lerp(
                    audioConfig.engineMinPitch,
                    audioConfig.engineMaxPitch,
                    speedNormalized
                );

                currentEnginePitch = Mathf.SmoothDamp(
                    currentEnginePitch,
                    targetPitch,
                    ref enginePitchVelocity,
                    audioConfig.enginePitchSmoothTime
                );

                engineAudioSource.pitch = currentEnginePitch;
            }
        }

        /// <summary>
        /// Play collision sound based on obstacle type
        /// </summary>
        public void PlayCollisionSound(ObstacleType obstacleType)
        {
            if (sfxAudioSource == null || audioConfig == null) return;

            AudioClip clip = obstacleType switch
            {
                ObstacleType.Deer => audioConfig.deerCollisionSound,
                ObstacleType.RollingStone => audioConfig.stoneCollisionSound,
                _ => null
            };

            if (clip != null)
            {
                sfxAudioSource.PlayOneShot(clip, audioConfig.collisionVolume);
            }
        }

        /// <summary>
        /// Play UI sound based on message type
        /// </summary>
        public void PlayUISound(UIMessageType messageType)
        {
            if (uiAudioSource == null || audioConfig == null) return;

            AudioClip clip = messageType switch
            {
                UIMessageType.Alert => audioConfig.alertSound,
                UIMessageType.Warning => audioConfig.warningSound,
                UIMessageType.Error => audioConfig.errorSound,
                UIMessageType.Hint => audioConfig.hintSound,
                UIMessageType.Status => audioConfig.statusSound,
                UIMessageType.DeerRespawn => audioConfig.errorSound,
                UIMessageType.StoneRespawn => audioConfig.errorSound,
                UIMessageType.ChildrenRespawn => audioConfig.errorSound,
                UIMessageType.ChildWarning => audioConfig.errorSound,
                UIMessageType.Delivery => audioConfig.deliveryCompleteSound,
                _ => null
            };

            if (clip != null)
            {
                uiAudioSource.PlayOneShot(clip, audioConfig.uiSoundVolume);
            }
        }

        /// <summary>
        /// Play delivery complete sound
        /// </summary>
        public void PlayDeliveryCompleteSound()
        {
            if (uiAudioSource == null || audioConfig == null) return;

            if (audioConfig.deliveryCompleteSound != null)
            {
                uiAudioSource.PlayOneShot(audioConfig.deliveryCompleteSound, audioConfig.completionVolume);
            }
        }

        /// <summary>
        /// Play general alert sound (for event activation, etc)
        /// </summary>
        public void PlayAlertSound()
        {
            if (uiAudioSource == null || audioConfig == null) return;

            if (audioConfig.alertSound != null)
            {
                uiAudioSource.PlayOneShot(audioConfig.alertSound, audioConfig.uiSoundVolume);
            }
        }

        /// <summary>
        /// Play button click sound
        /// </summary>
        public void PlayButtonClickSound()
        {
            if (uiAudioSource == null || audioConfig == null) return;

            if (audioConfig.buttonClickSound != null)
            {
                uiAudioSource.PlayOneShot(audioConfig.buttonClickSound, audioConfig.buttonSoundVolume);
            }
        }

        /// <summary>
        /// Play button hover sound
        /// </summary>
        public void PlayButtonHoverSound()
        {
            if (uiAudioSource == null || audioConfig == null) return;

            if (audioConfig.buttonHoverSound != null)
            {
                uiAudioSource.PlayOneShot(audioConfig.buttonHoverSound, audioConfig.buttonSoundVolume);
            }
        }

        /// <summary>
        /// Play event trigger button sound (for Event 1, 2, 3 buttons)
        /// </summary>
        public void PlayEventTriggerButtonSound()
        {
            if (uiAudioSource == null || audioConfig == null) return;

            if (audioConfig.eventTriggerButtonSound != null)
            {
                uiAudioSource.PlayOneShot(audioConfig.eventTriggerButtonSound, audioConfig.eventButtonVolume);
            }
        }

        /// <summary>
        /// Get the audio configuration (for external components to access settings)
        /// </summary>
        public AudioConfig GetAudioConfig()
        {
            return audioConfig;
        }

        /// <summary>
        /// Stop all sounds immediately
        /// </summary>
        public void StopAllSounds()
        {
            if (engineAudioSource != null && engineAudioSource.isPlaying)
            {
                engineAudioSource.Stop();
            }
            if (uiAudioSource != null && uiAudioSource.isPlaying)
            {
                uiAudioSource.Stop();
            }
            if (sfxAudioSource != null && sfxAudioSource.isPlaying)
            {
                sfxAudioSource.Stop();
            }
        }

        /// <summary>
        /// Attach engine audio source to the robot (for spatial 3D audio)
        /// Call this when the robot is instantiated
        /// </summary>
        public void AttachEngineSourceToRobot(Transform robotTransform)
        {
            if (engineAudioSource != null && robotTransform != null)
            {
                engineAudioSource.transform.SetParent(robotTransform);
                engineAudioSource.transform.localPosition = Vector3.zero;
            }
        }
    }
}
