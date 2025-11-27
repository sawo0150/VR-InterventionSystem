using UnityEngine;
using UnityEngine.UI;

namespace Project
{
    /// <summary>
    /// Creates a blinking visual effect on a UI button.
    /// Automatically starts blinking when the button is enabled and stops when disabled.
    /// Attach this to any Button GameObject to add blinking functionality.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class BlinkingButton : MonoBehaviour
    {
        [Header("Blink Settings")]
        [Tooltip("The Image or RawImage component to apply the blink effect to (leave empty to auto-detect)")]
        [SerializeField] private Graphic targetGraphic;

        [Tooltip("Blink effect type")]
        [SerializeField] private BlinkType blinkType = BlinkType.Alpha;

        [Header("Alpha Blink Settings")]
        [Tooltip("Minimum alpha value during blink")]
        [Range(0f, 1f)]
        [SerializeField] private float minAlpha = 0.3f;

        [Tooltip("Maximum alpha value during blink")]
        [Range(0f, 1f)]
        [SerializeField] private float maxAlpha = 1f;

        [Header("Color Blink Settings")]
        [Tooltip("First color for color blink")]
        [SerializeField] private Color color1 = Color.white;

        [Tooltip("Second color for color blink")]
        [SerializeField] private Color color2 = Color.yellow;

        [Header("Timing")]
        [Tooltip("Speed of the blink effect (cycles per second)")]
        [Range(0.5f, 10f)]
        [SerializeField] private float blinkSpeed = 2f;

        [Tooltip("Use smooth sine wave instead of linear interpolation")]
        [SerializeField] private bool useSmoothBlink = true;

        // Runtime state
        private Button button;
        private Color originalColor;
        private float originalAlpha;
        private float blinkTimer = 0f;
        private bool isBlinking = false;

        public enum BlinkType
        {
            Alpha,      // Fade in/out
            Color,      // Alternate between two colors
            Both        // Both alpha and color
        }

        private void Awake()
        {
            button = GetComponent<Button>();

            // Auto-detect Graphic component (Image or RawImage) if not assigned
            if (targetGraphic == null)
            {
                // Try Image first, then RawImage
                targetGraphic = GetComponent<Image>();
                if (targetGraphic == null)
                {
                    targetGraphic = GetComponent<RawImage>();
                }
            }

            if (targetGraphic != null)
            {
                originalColor = targetGraphic.color;
                originalAlpha = originalColor.a;
            }
        }

        private void OnEnable()
        {
            // Start blinking when button is enabled
            StartBlinking();
        }

        private void OnDisable()
        {
            // Stop blinking and reset to original appearance
            StopBlinking();
        }

        private void Update()
        {
            if (!isBlinking || targetGraphic == null) return;

            // Update blink timer
            blinkTimer += Time.deltaTime * blinkSpeed;

            // Calculate blink progress (0 to 1 and back)
            float progress = Mathf.PingPong(blinkTimer, 1f);

            // Apply smooth curve if enabled
            if (useSmoothBlink)
            {
                progress = Mathf.Sin(progress * Mathf.PI); // Sine wave for smooth pulse
            }

            // Apply blink effect based on type
            switch (blinkType)
            {
                case BlinkType.Alpha:
                    ApplyAlphaBlink(progress);
                    break;

                case BlinkType.Color:
                    ApplyColorBlink(progress);
                    break;

                case BlinkType.Both:
                    ApplyAlphaBlink(progress);
                    ApplyColorBlink(progress);
                    break;
            }
        }

        /// <summary>
        /// Apply alpha blinking effect
        /// </summary>
        private void ApplyAlphaBlink(float progress)
        {
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, progress);
            Color currentColor = targetGraphic.color;
            currentColor.a = alpha;
            targetGraphic.color = currentColor;
        }

        /// <summary>
        /// Apply color blinking effect
        /// </summary>
        private void ApplyColorBlink(float progress)
        {
            Color blendedColor = Color.Lerp(color1, color2, progress);

            // Preserve alpha if not using Both mode
            if (blinkType == BlinkType.Color)
            {
                blendedColor.a = targetGraphic.color.a;
            }

            targetGraphic.color = blendedColor;
        }

        /// <summary>
        /// Start the blinking effect
        /// </summary>
        public void StartBlinking()
        {
            isBlinking = true;
            blinkTimer = 0f;
        }

        /// <summary>
        /// Stop the blinking effect and restore original appearance
        /// </summary>
        public void StopBlinking()
        {
            isBlinking = false;

            if (targetGraphic != null)
            {
                targetGraphic.color = originalColor;
            }
        }

        /// <summary>
        /// Change the blink speed at runtime
        /// </summary>
        public void SetBlinkSpeed(float speed)
        {
            blinkSpeed = Mathf.Max(0.1f, speed);
        }

        /// <summary>
        /// Change the blink type at runtime
        /// </summary>
        public void SetBlinkType(BlinkType type)
        {
            blinkType = type;
        }
    }
}
