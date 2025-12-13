using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VRInterventionSystem.Audio
{
    /// <summary>
    /// Attach this component to Event Trigger buttons (Event 1, 2, 3 buttons in minimap/monitoring scene)
    /// to play a special event trigger sound instead of regular button click sound.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIEventTriggerButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [Header("Sound Settings")]
        [Tooltip("Play special event trigger sound when clicked")]
        [SerializeField] private bool playEventTriggerSound = true;

        [Tooltip("Play sound when hovering over button")]
        [SerializeField] private bool playHoverSound = true;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            // Subscribe to button click event
            if (button != null && playEventTriggerSound)
            {
                button.onClick.AddListener(OnButtonClick);
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from button click event
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClick);
            }
        }

        /// <summary>
        /// Called when button is clicked (via Unity UI Button)
        /// </summary>
        private void OnButtonClick()
        {
            if (playEventTriggerSound && button.interactable)
            {
                PlayEventTriggerSound();
            }
        }

        /// <summary>
        /// Called when pointer enters button area (IPointerEnterHandler)
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (playHoverSound && button != null && button.interactable)
            {
                PlayHoverSound();
            }
        }

        /// <summary>
        /// Called when pointer clicks button (IPointerClickHandler)
        /// Backup for click detection
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            // Click sound is already handled by button.onClick
            // This is here as a backup and for compatibility
        }

        /// <summary>
        /// Play event trigger button sound
        /// </summary>
        private void PlayEventTriggerSound()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayEventTriggerButtonSound();
            }
        }

        /// <summary>
        /// Play button hover sound
        /// </summary>
        private void PlayHoverSound()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayButtonHoverSound();
            }
        }
    }
}
