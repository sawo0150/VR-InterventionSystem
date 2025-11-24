using UnityEngine;
using UnityEngine.InputSystem;

public class VRJoystickDebugLog : MonoBehaviour
{
    [Header("Input Action")]
    [Tooltip("MOD_ScreenRoom / RightStickMove 액션을 연결하세요")]
    public InputActionReference moveAction;

    [Header("Velocity Settings")]
    public float maxLinear = 1.0f;    // 최대 직진 속도
    public float maxAngular = 1.0f;   // 최대 회전 속도

    [Header("Debug")]
    public float logInterval = 0.1f; // 로그가 너무 빨리 올라가지 않게 조절 (0.1초)

    private float _nextLogTime;

    private void OnEnable()
    {
        // 액션이 연결되어 있다면 활성화
        if (moveAction != null) moveAction.action.Enable();
    }

    void Update()
    {
        if (moveAction == null) return;

        // 로그 출력 주기 체크
        if (Time.time < _nextLogTime) return;

        // 1. 조이스틱 입력값 읽기
        Vector2 input = moveAction.action.ReadValue<Vector2>();

        // 2. 로봇 제어 로직 (이전 코드와 동일하게 계산)
        float linear = input.y * maxLinear;
        float angular = -input.x * maxAngular; // 좌우 반전 로직 유지

        // 3. 값이 있을 때만 로그 출력 (0,0 일 때 콘솔 도배 방지하려면 주석 해제)
        // if (input.magnitude < 0.01f) return; 

        // 4. 콘솔창에 출력
        Debug.Log($"[Robot Debug] Joystick: {input} | Linear(전진): {linear:F2} | Angular(회전): {angular:F2}");

        _nextLogTime = Time.time + logInterval;
    }
}