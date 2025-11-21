using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

namespace Project
{
    public class TutorialUIController : MonoBehaviour
    {
        [SerializeField] private GameObject tutorialCanvas; 
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private List<GameObject> contentPages;

        [Header("Buttons")]
        [SerializeField] private Button prevButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TextMeshProUGUI nextButtonText;
        
        private int currentIndex = 0;
        private Action onTutorialFinished;

        private void Awake()
        {
            if (nextButtonText == null && nextButton != null)
                nextButtonText = nextButton.GetComponentInChildren<TextMeshProUGUI>();
        }

        private void Start()
        {
            CheckAssignments();
            InitializeListeners();
        }
        
        private void CheckAssignments()
        {
            if (tutorialCanvas == null) MyDebug.LogWarning($"[{GetType().Name}] tutorialCanvas is missing!");
            if (prevButton == null)     MyDebug.LogWarning($"[{GetType().Name}] prevButton is missing!");
            if (nextButton == null)     MyDebug.LogWarning($"[{GetType().Name}] nextButton is missing!");
            if (skipButton == null)     MyDebug.LogWarning($"[{GetType().Name}] skipButton is missing!");
            if (nextButtonText == null) MyDebug.LogWarning($"[{GetType().Name}] nextButtonText is missing!");
            
            if (contentPages == null || contentPages.Count == 0)
                MyDebug.LogWarning($"[{GetType().Name}] contentPages list is empty!");
        }
        
        private void InitializeListeners()
        {
            prevButton.onClick.AddListener(OnPrevClick);
            nextButton.onClick.AddListener(OnNextClick);
            skipButton.onClick.AddListener(OnSkipClick);
        }

        public void BeginTutorial(Action onComplete)
        {
            if (contentPages == null || contentPages.Count == 0)
            {
                MyDebug.LogWarning($"[{GetType().Name}] Tutorial pages are not found");
                onComplete?.Invoke();
                return;
            }
            
            this.onTutorialFinished = onComplete;
            currentIndex = 0;

            tutorialCanvas.SetActive(true);
            
            UpdateUI();
            
            MyDebug.Log($"[{GetType().Name}] Tutorial Started");
        }
        
        private void EndTutorial()
        {
            Hide();
            onTutorialFinished?.Invoke();
            MyDebug.Log($"[{GetType().Name}] Tutorial Finished");
        }
        

        private void UpdateUI()
        {
            // Update content
            for (var i = 0; i < contentPages.Count; i++)
            {
                if (contentPages[i] != null) 
                    contentPages[i].SetActive(i == currentIndex);
            }
            
            // Update button states
            prevButton.gameObject.SetActive(currentIndex > 0);
            
            var isLast = (currentIndex == contentPages.Count - 1);
            
            if (isLast)
            {
                nextButtonText.text = "Get Started";
                skipButton.gameObject.SetActive(false);
            }
            else
            {
                nextButtonText.text = "Next >";
                skipButton.gameObject.SetActive(true);
            }
        }
        
        private void OnNextClick()
        {
            if (currentIndex >= contentPages.Count - 1)
            {
                EndTutorial();
                return;
            }
            currentIndex++;
            UpdateUI();
        }

        private void OnPrevClick()
        {
            if (currentIndex > 0)
            {
                currentIndex--;
                UpdateUI();
            }
        }

        private void OnSkipClick()
        {
            EndTutorial();
        }
        
        public void Hide()
        {
            tutorialCanvas.SetActive(false);
        }
    }
}