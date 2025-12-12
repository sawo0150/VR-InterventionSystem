using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VRInterventionSystem.Audio
{
    /// <summary>
    /// Attach this component to any UI Button to automatically play sounds on click and hover.
    /// Works with both regular Unity UI buttons and XR interactable buttons.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [Header("Sound Settings")]
        [Tooltip("Play sound when button is clicked")]
        [SerializeField] private bool playClickSound = true;

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
            if (button != null && playClickSound)
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
            if (playClickSound && button.interactable)
            {
                PlayClickSound();
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
        /// Play button click sound
        /// </summary>
        private void PlayClickSound()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayButtonClickSound();
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
