using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;

namespace RealRobot{
    // 이 스크립트는 이제 MonoBehaviour입니다. 게임 오브젝트에 직접 붙일 수 있습니다.
    public class ActionTrigger : MonoBehaviour
    {
        [Header("설명 (에디터 확인용)")]
        public string description = "트리거 이름";

        [Header("조건 설정 (OR 로직)")]
        [Tooltip("UI 버튼을 연결하세요 (선택)")]
        public Button uiButton;

        [Tooltip("Input System 액션을 연결하세요 (선택)")]
        public InputActionReference inputAction;

        [Header("실행할 함수")]
        [Tooltip("조건 만족 시 실행될 함수들")]
        public UnityEvent onTriggered;

        // 매니저가 이 함수를 호출해야 리스너가 켜지도록 설계 (원하시면 Start에서 바로 켜도 됩니다)
        public void ActivateTrigger()
        {
            // 1. UI 버튼 리스너 등록
            if (uiButton != null)
            {
                uiButton.onClick.AddListener(Execute);
            }

            // 2. Input Action 리스너 등록
            if (inputAction != null)
            {
                inputAction.action.Enable();
                inputAction.action.performed += ctx => Execute();
            }
        }

        // 조건 만족 시 실행되는 함수
        private void Execute()
        {
            Debug.Log($"[{description}] 조건이 만족되어 이벤트를 실행합니다.");
            onTriggered.Invoke();
        }

        // 오브젝트가 꺼지거나 파괴될 때 안전하게 연결 해제
        private void OnDisable()
        {
            if (uiButton != null) uiButton.onClick.RemoveListener(Execute);
            if (inputAction != null) inputAction.action.performed -= ctx => Execute();
        }
    }
}