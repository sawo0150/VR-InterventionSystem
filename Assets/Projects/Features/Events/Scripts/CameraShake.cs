using UnityEngine;
using System.Collections;

namespace Project.Event
{
    public class CameraShake : MonoBehaviour
    {
        [SerializeField] private float duration = 0.3f;
        [SerializeField] private float magnitude = 0.5f;
        [SerializeField] private float frequency = 2f;

        private Vector3 originalPos;
        private Coroutine shakingCoroutine;

        // Cache the original local position
        private void Start()
        {
            originalPos = transform.localPosition;
        }
        
        // Public method to shake camera; callable by events
        public void TriggerShake()
        {
            // If a shake is already active, stop it and reset to original position
            if (shakingCoroutine != null)
            {
                StopCoroutine(shakingCoroutine);
                transform.localPosition = originalPos;
            }
        
            // Start a new shake coroutine and store its reference
            shakingCoroutine = StartCoroutine(Shake());
        }

        // Shaking method (@@ only x-axis)
        private IEnumerator Shake()
        {
            float elapsed = 0.0f;

            while (elapsed < duration)
            {
                // 1. calculate progress (0 to 1)
                float progress = elapsed / duration;

                // 2. Smooth fade-in and fade-out envelope
                // (progress, envelope): (0,0) -> (0.5, 1) -> (1, 0)
                float envelope = Mathf.Sin(progress * Mathf.PI);

                // 3. Calculate offset
                float angle = progress * frequency * (2f * Mathf.PI);
                float xOffset = Mathf.Sin(angle) * magnitude * envelope;

                // 4. Apply offset
                transform.localPosition = new Vector3(
                    originalPos.x + xOffset,
                    originalPos.y,
                    originalPos.z
                );

                elapsed += Time.deltaTime;
                
                // wait for the next frame
                yield return null;
            }

            // 5. Ensure final position and remove coroutine
            transform.localPosition = originalPos;
            shakingCoroutine = null;
        }
    }
}

