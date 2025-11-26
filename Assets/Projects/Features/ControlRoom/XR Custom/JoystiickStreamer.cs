using UnityEngine;
using UnityEngine.InputSystem; // 필수 네임스페이스

public class VRJoystickRobotInput : MonoBehaviour
{
    [Header("References")]
    public WebRTCReceiver receiver;   // 통신 모듈 연결

    [Header("Input Action")]
    [Tooltip("MOD_ScreenRoom / RightStickMove 액션을 연결하세요")]
    public InputActionReference moveAction;

    [Header("Velocity Settings")]
    public float maxLinear = 1.0f;    // 최대 직진 속도 (m/s)
    public float maxAngular = 1.0f;   // 최대 회전 속도 (rad/s)

    [Header("Network")]
    public float sendInterval = 0.05f; // 전송 주기 (초)

    private float _nextSendTime;

    // 스크립트가 켜질 때 액션 활성화 (혹시 모르니 보험용)
    private void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
    }

    void Update()
    {
        if (receiver == null || moveAction == null) return;

        // 전송 주기 체크 (네트워크 부하 방지)
        if (Time.time < _nextSendTime) return;

        // 1. 조이스틱 입력값 읽기 (x, y 좌표)
        // 범위: -1.0 ~ 1.0
        Vector2 input = moveAction.action.ReadValue<Vector2>();

        // 2. 속도 계산
        // input.y (위/아래) -> 선속도 (Linear)
        float linear = input.y * maxLinear;

        // input.x (좌/우) -> 각속도 (Angular)
        // 기존 WASD 코드에서 'A'(좌)가 Angular를 더했으므로(Positive),
        // 조이스틱을 왼쪽(-X)으로 밀었을 때 양수(+)가 나오도록 -부호를 붙입니다.
        float angular = -input.x * maxAngular; 

        // 3. WebRTC로 전송
        // (입력이 없으면 0,0이 전송되어 로봇이 멈춥니다)
        receiver.SendControl(linear, angular);

        // 다음 전송 시간 갱신
        _nextSendTime = Time.time + sendInterval;
    }
}