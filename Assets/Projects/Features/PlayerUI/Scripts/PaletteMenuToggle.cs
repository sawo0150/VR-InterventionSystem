using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

namespace Project
{
    public class AnimatedWristMenu : MonoBehaviour
    {
        [Header("Configuration")]
        public GameObject menuPanel;
        public InputActionProperty toggleInput;

        [Header("Animation Settings")]
        public float animationDuration = 0.2f; // How fast it opens (0.2s is snappy)
        public Vector3 targetScale = new Vector3(0.001f, 0.001f, 0.001f); // The size you set in the Inspector earlier

        private CanvasGroup canvasGroup;
        private bool isMenuOpen = false;
        private Coroutine currentAnimation;

        void Start()
        {
            // Get the CanvasGroup and prepare initial state
            canvasGroup = menuPanel.GetComponent<CanvasGroup>();
            
            // Ensure menu starts closed, invisible, and tiny
            menuPanel.transform.localScale = Vector3.zero;
            canvasGroup.alpha = 0;
            menuPanel.SetActive(false);
        }

        void Update()
        {
            if (toggleInput.action != null && toggleInput.action.WasPressedThisFrame())
            {
                if (isMenuOpen)
                    CloseMenu();
                else
                    OpenMenu();
            }
        }

        void OpenMenu()
        {
            isMenuOpen = true;
            menuPanel.SetActive(true); // Turn object on first
            
            // Stop any currently running closing animation so they don't fight
            if (currentAnimation != null) StopCoroutine(currentAnimation);
            
            // Start opening animation
            currentAnimation = StartCoroutine(AnimateMenu(0, 1, Vector3.zero, targetScale));
        }

        void CloseMenu()
        {
            isMenuOpen = false;
            
            if (currentAnimation != null) StopCoroutine(currentAnimation);
            
            // Start closing animation
            currentAnimation = StartCoroutine(AnimateMenu(1, 0, targetScale, Vector3.zero, true));
        }

        // A flexible Coroutine that handles both opening and closing
        IEnumerator AnimateMenu(float startAlpha, float endAlpha, Vector3 startScale, Vector3 endScale, bool disableOnFinish = false)
        {
            float elapsedTime = 0;

            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float percentage = elapsedTime / animationDuration;

                // Apply a "SmoothStep" curve for a more organic feel than linear
                float curvedPercentage = Mathf.SmoothStep(0, 1, percentage);

                // Lerp (Linear Interpolate) the Alpha and Scale
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, curvedPercentage);
                menuPanel.transform.localScale = Vector3.Lerp(startScale, endScale, curvedPercentage);

                yield return null; // Wait for the next frame
            }

            // Ensure we hit the exact final values
            canvasGroup.alpha = endAlpha;
            menuPanel.transform.localScale = endScale;

            // If we are closing, finally disable the game object
            if (disableOnFinish)
            {
                menuPanel.SetActive(false);
            }
        }
    }
}