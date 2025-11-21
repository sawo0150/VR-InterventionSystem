using UnityEngine;

public class WASDRobotInput : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("References")]
    public WebRTCReceiver receiver;   // Hierarchy의 WebRTCReceiver를 드래그해서 연결

    [Header("Velocity")]
    public float maxLinear = 1.0f;    // m/s 처럼 생각
    public float maxAngular = 1.0f;   // rad/s 비슷한 개념

    [Header("Network")]
    public float sendInterval = 0.05f; // 20Hz 정도로 전송

    private float _nextSendTime;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (receiver == null) return;

        if (Time.time < _nextSendTime) return;

        float linear = 0f;
        float angular = 0f;

        // W/S : 전진 / 후진
        if (Input.GetKey(KeyCode.W)) linear += 1f;
        if (Input.GetKey(KeyCode.S)) linear -= 1f;

        // A/D : 좌회전 / 우회전
        if (Input.GetKey(KeyCode.A)) angular += 1f;
        if (Input.GetKey(KeyCode.D)) angular -= 1f;

        // 아무 키도 안 누르면 0,0 → 정지 명령
        linear *= maxLinear;
        angular *= maxAngular;

        receiver.SendControl(linear, angular);

        _nextSendTime = Time.time + sendInterval;
    }
}
