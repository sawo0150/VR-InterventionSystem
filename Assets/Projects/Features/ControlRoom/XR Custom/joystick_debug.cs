using UnityEngine;
using UnityEngine.InputSystem;

public class VRJoystickDebugLog : MonoBehaviour
{
    [Header("--- Settings ---")]
    [Tooltip("체크하면 로그만 출력하고, 체크 해제하면 실제 로봇으로 명령을 보냅니다.")]
    public bool isDebugMode = true;

    [Tooltip("실제 통신을 담당하는 WebRTC Receiver 스크립트를 연결하세요.")]
    public WebRTCReceiver_controlRoom webRTCReceiver;

    [Header("Input Action")]
    [Tooltip("MOD_ScreenRoom / RightStickMove 액션을 연결하세요")]
    public InputActionReference moveAction;

    [Header("Velocity Settings")]
    public float maxLinear = 0.5f;    // 최대 직진 속도 (안전을 위해 0.5로 낮춤)
    public float maxAngular = 1.0f;   // 최대 회전 속도

    [Header("Optimization")]
    public float sendInterval = 0.1f; // 통신/로그 주기 (0.1초 = 10Hz)
    private float _nextSendTime;

    // [+] 액션의 이전 상태를 기억하기 위한 변수 추가
    private bool _wasActionEnabled = false;

    private void OnEnable()
    {
        // 스크립트가 켜질 때 액션도 같이 활성화 (원하지 않으면 주석 처리)
        if (moveAction != null) moveAction.action.Enable();
    }
    // [+] 스크립트가 꺼지거나 파괴될 때 안전하게 AUTO 모드로 복귀
    private void OnDisable()
    {
        if (!isDebugMode && webRTCReceiver != null)
        {
            webRTCReceiver.SetOperationMode("AUTO");
        }
    }

    void Update()
    {
        // 1. 현재 액션이 켜져 있는지 확인 (LocationSwitcher 등에 의해 관리됨)
        bool isCurrentEnabled = (moveAction != null && moveAction.action.enabled);

        // [+] 상태가 변했을 때만 실행 (Edge Detection)
        if (isCurrentEnabled != _wasActionEnabled)
        {
            if (isCurrentEnabled)
            {
                // [Case 1] OFF -> ON : VR 모드로 전환 요청
                Debug.Log("[VRJoystick] Input Enabled -> Switching Robot to VR Mode");
                if (!isDebugMode && webRTCReceiver != null)
                {
                    webRTCReceiver.SetOperationMode("VR");
                }
            }
            else
            {
                // [Case 2] ON -> OFF : AUTO 모드로 복귀 요청 (안전장치)
                Debug.Log("[VRJoystick] Input Disabled -> Switching Robot to AUTO Mode");
                if (!isDebugMode && webRTCReceiver != null)
                {
                    webRTCReceiver.SetOperationMode("AUTO");
                    webRTCReceiver.SendControl(0, 0); // 정지 명령까지 보내면 더 안전함
                }
            }

            // 현재 상태 저장
            _wasActionEnabled = isCurrentEnabled;
        }

        // 2. 안전 장치: 액션이 비활성화 상태라면 여기서 함수 종료 (기존 로직)
        if (!isCurrentEnabled)
            return;

        // 3. 조이스틱 입력값 읽기
        Vector2 input = moveAction.action.ReadValue<Vector2>();

        // 4. 로봇 속도 계산
        float linear = input.y * maxLinear;       // Y축 = 전진/후진
        float angular = -input.x * maxAngular;    // X축 = 회전 (좌우 반전 - ROS 좌표계 대응)

        // 5. 모드에 따른 동작 분기
        if (isDebugMode)
        {
            // [디버그 모드] 콘솔에 로그만 출력
            // 값이 거의 0일 때는 로그 스팸 방지 (선택 사항)
            if (input.magnitude > 0.01f)
            {
                Debug.Log($"[Debug] Joystick: {input} | CMD -> Linear: {linear:F2}, Angular: {angular:F2}");
            }
        }
        else
        {
            // [실제 제어 모드] WebRTC를 통해 로봇으로 전송
            if (webRTCReceiver != null)
            {
                // 입력이 없어도(0,0) 정지 신호를 보내야 하므로 조건 없이 전송
                webRTCReceiver.SendControl(linear, angular);

                // (선택) 전송 중임을 알리는 로그 (필요 시 주석 해제)
                // Debug.Log($"[Sending] Linear: {linear:F2}, Angular: {angular:F2}");
            }
            else
            {
                Debug.LogWarning("WebRTCReceiver가 연결되지 않았습니다! Inspector를 확인하세요.");
            }
        }

        _nextSendTime = Time.time + sendInterval;
    }
}