using UnityEngine;
using TMPro;

namespace Project
{
    /// <summary>
    /// Base class for all UI message panel prefabs.
    /// Designers create custom panel prefabs that inherit from this class.
    /// Each panel type (Warning, Status, etc.) can have completely different visual designs.
    ///
    /// NOTE: The messageText field is optional. If not assigned, the script will automatically
    /// find the first TextMeshProUGUI component in the prefab's children.
    /// </summary>
    public class UIMessagePanel : MonoBehaviour
    {
        [Header("Panel References (Optional)")]
        [Tooltip("The main TextMeshPro component for displaying the message. Leave empty to auto-detect.")]
        [SerializeField] protected TextMeshProUGUI messageText;

        [Header("Animation Settings")]
        [Tooltip("Enable fade in/out animations")]
        [SerializeField] protected bool enableFadeAnimation = true;

        [Tooltip("Duration of fade in/out animations")]
        [SerializeField] protected float fadeDuration = 0.3f;

        protected CanvasGroup canvasGroup;
        protected bool isVisible = false;
        protected float fadeTimer = 0f;
        protected bool isFading = false;
        protected bool fadingIn = false;

        protected virtual void Awake()
        {
            // Auto-detect TextMeshPro component if not assigned
            if (messageText == null)
            {
                messageText = GetComponentInChildren<TextMeshProUGUI>();
            }

            // Get or add CanvasGroup for fading
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null && enableFadeAnimation)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Start hidden
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
            gameObject.SetActive(false);
        }

        protected virtual void Update()
        {
            if (!enableFadeAnimation || !isFading) return;

            fadeTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(fadeTimer / fadeDuration);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = fadingIn ? progress : (1f - progress);
            }

            if (progress >= 1f)
            {
                isFading = false;
                if (!fadingIn)
                {
                    // Fade out complete - hide the panel
                    gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Show the panel with a message
        /// If message is empty or null, the panel's existing text is used
        /// </summary>
        public virtual void Show(string message)
        {
            // Only update text if a message is provided
            if (!string.IsNullOrEmpty(message) && messageText != null)
            {
                messageText.text = message;
            }

            gameObject.SetActive(true);
            isVisible = true;

            if (enableFadeAnimation && canvasGroup != null)
            {
                isFading = true;
                fadingIn = true;
                fadeTimer = 0f;
            }
            else if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        /// <summary>
        /// Hide the panel
        /// </summary>
        public virtual void Hide()
        {
            isVisible = false;

            if (enableFadeAnimation && canvasGroup != null)
            {
                isFading = true;
                fadingIn = false;
                fadeTimer = 0f;
            }
            else
            {
                gameObject.SetActive(false);
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }
            }
        }

        /// <summary>
        /// Update the message text without hiding/showing
        /// </summary>
        public virtual void UpdateText(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }
        }

        /// <summary>
        /// Check if the panel is currently visible
        /// </summary>
        public bool IsVisible()
        {
            return isVisible;
        }

        /// <summary>
        /// Get the message type this panel is designed for
        /// Override in derived classes if needed
        /// </summary>
        public virtual UIMessageType GetMessageType()
        {
            return UIMessageType.Warning; // Default
        }
    }
}
