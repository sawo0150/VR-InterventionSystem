using UnityEngine;

namespace VRInterventionSystem.Audio
{
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "VR Intervention/Audio Config")]
    public class AudioConfig : ScriptableObject
    {
        [Header("Engine Sounds")]
        [Tooltip("Looping engine sound clip")]
        public AudioClip engineLoop;
        [Range(0f, 1f)]
        public float engineBaseVolume = 0.5f;
        [Tooltip("Minimum pitch when robot is idle/slow")]
        public float engineMinPitch = 0.8f;
        [Tooltip("Maximum pitch when robot is at max speed")]
        public float engineMaxPitch = 1.5f;
        [Tooltip("How quickly the engine sound responds to speed changes")]
        public float enginePitchSmoothTime = 0.3f;

        [Header("Collision Sounds")]
        [Tooltip("Sound when robot hits a deer")]
        public AudioClip deerCollisionSound;
        [Tooltip("Sound when robot hits a rolling stone")]
        public AudioClip stoneCollisionSound;
        [Range(0f, 1f)]
        public float collisionVolume = 0.7f;

        [Header("Deer Ambient Sounds")]
        [Tooltip("Looping ambient sound for deer (plays while Event 1 is active)")]
        public AudioClip deerAmbientLoop;
        [Range(0f, 1f)]
        public float deerAmbientVolume = 0.4f;
        [Tooltip("3D spatial blend for deer sounds (1 = full 3D)")]
        [Range(0f, 1f)]
        public float deerSpatialBlend = 1f;
        [Tooltip("Max distance for deer sounds")]
        public float deerMaxDistance = 30f;

        [Header("UI Alert Sounds")]
        [Tooltip("Sound for general alerts (event activation, etc)")]
        public AudioClip alertSound;
        [Tooltip("Sound for warnings (boundary violations, etc)")]
        public AudioClip warningSound;
        [Tooltip("Sound for errors")]
        public AudioClip errorSound;
        [Tooltip("Sound for hints/help messages")]
        public AudioClip hintSound;
        [Tooltip("Sound for status messages")]
        public AudioClip statusSound;
        [Range(0f, 1f)]
        public float uiSoundVolume = 0.6f;

        [Header("UI Interaction Sounds")]
        [Tooltip("Sound when a button is clicked/pressed")]
        public AudioClip buttonClickSound;
        [Tooltip("Sound when hovering over interactive UI elements")]
        public AudioClip buttonHoverSound;
        [Tooltip("Sound when event trigger button is clicked (Event 1, 2, 3 buttons)")]
        public AudioClip eventTriggerButtonSound;
        [Range(0f, 1f)]
        public float buttonSoundVolume = 0.5f;
        [Range(0f, 1f)]
        public float eventButtonVolume = 0.7f;

        [Header("Completion Sounds")]
        [Tooltip("Sound when delivery is completed successfully")]
        public AudioClip deliveryCompleteSound;
        [Range(0f, 1f)]
        public float completionVolume = 0.8f;

        [Header("Spatial Audio Settings")]
        [Tooltip("3D spatial blend for engine sounds (1 = full 3D)")]
        [Range(0f, 1f)]
        public float engineSpatialBlend = 1f;
        [Tooltip("Max distance for 3D engine sounds")]
        public float engineMaxDistance = 50f;
        [Tooltip("2D blend for UI sounds (0 = full 2D)")]
        [Range(0f, 1f)]
        public float uiSpatialBlend = 0f;
    }
}
