using System;
using System.Collections;
using System.Collections.Generic; // ◀◀ List, Queue 등을 사용하기 위해 추가
using System.Runtime.Serialization;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI; // [+] UI 컴포넌트 사용을 위해 필수 추가
// 1. NativeWebSocket 대신 websocket-sharp 네임스페이스 사용
using WebSocketSharp;
using TMPro; // ◀◀ [중요] TextMeshPro를 사용하기 위해 반드시 추가해야 합니다!


/*
 * WebRTCReceiver_controlRoom.cs (WebSocketSharp 버전)
 * 1. WebSocketSharp를 사용해 시그널링 서버에 접속.
 * 2. 서버에서 "offer" 메시지를 받으면 WebRTC 연결 절차 시작.
 * 3. "answer" 및 "candidate" 메시지를 서버를 통해 Jetson에 전송.
 * 4. Jetson의 비디오 트랙을 받아 3D Plane(스크린)에 렌더링.
 *
 * JSON 메시지용 타입들(SignalingMessage, IceCandidateMessage, ControlMessage)은
 * 이미 다른 파일(WebRTCReceiver.cs)에서 전역으로 정의되어 있으므로
 * 이 파일에서는 **정의하지 않고 가져다만 사용**한다.
 */

// [+] ROS와 통신하기 위한 신규 데이터 포맷 정의 (파일 하단 혹은 상단에 위치)
// -------------------------------------------------------------------------
[System.Serializable]
public class RobotStatusMessage
{
    public string type;
    public bool error;
    public string mode; // "AUTO" or "VR"
}

[System.Serializable]
public class RobotMapMessage
{
    public string type;
    public string format;
    public string data; // Base64 String
}

[System.Serializable]
public class RobotModeCommand
{
    public string type = "mode";
    public string mode;
}
// -------------------------------------------------------------------------
public class WebRTCReceiver_controlRoom : MonoBehaviour
{
    [Header("Receiver Screen")]
    public Renderer targetRenderer;   // Plane/Quad의 MeshRenderer를 Inspector에서 연결

    // [+] UI Targets (Inspector에서 연결)
    // -------------------------------------------------
    [Header("Robot Data UI (New)")]
    public RawImage mapDisplay;      // 로봇이 보내는 지도 이미지 표시
    public TMP_Text statusText;       // [스크린샷의 Overall Status 연결용]
    public TMP_Text errorText;        // [스크린샷의 Error Message 연결용]
    public Image errorPanel;          // (선택) 에러시 배경 색상을 바꾸고 싶다면 연결 (Status Container 등)
    // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

    [Header("Signaling")]
    //public string signalingServerUrl = "ws://192.168.0.29:8080"; // ◀◀◀ 본인 서버 IP로 변경!
    //public string signalingServerUrl = "ws://192.168.247.247:8080"; // ◀◀◀ 본인 서버 IP로 변경!
    public string signalingServerUrl = "ws://127.0.0.1:8080"; // ◀◀◀ 본인 서버 IP로 변경!

    [Header("Remote Control")]
    public bool enableControl = true;

    private RTCPeerConnection pc;
    
    // 🔹 WebRTC가 직접 주는 텍스처 (GPU용, 디버그/바인딩용)
    private Texture _remoteTexture;
    public Texture RemoteTexture => _remoteTexture;
    public bool HasRemoteFrame => _remoteTexture != null;


    // 2. WebSocket 객체 변경
    private WebSocket ws;

    // 3. Unity 메인 스레드에서 실행할 작업을 담아둘 큐
    // websocket-sharp는 메시지 수신을 별도 스레드에서 처리하므로,
    // Unity API (WebRTC, UI 등)를 건드리기 위해선 메인 스레드로 작업을 전달해야 함
    private readonly Queue<Action> _mainThreadActions = new Queue<Action>();

    void Start()
    {
        // WebRTC 비동기 업데이트 시작 (필수)
        StartCoroutine(WebRTC.Update());

        // 4. WebSocketSharp 스타일로 WebSocket 객체 생성 및 이벤트 핸들러 등록
        ws = new WebSocket(signalingServerUrl);

        ws.OnOpen += (sender, e) =>
        {
            // OnOpen 스레드에서 바로 보내도 됨 (race 줄이기)
            Debug.Log("[WebSocket] 연결 성공! Jetson의 'offer'를 기다립니다...");
            ws.Send("{\"role\":\"receiver\"}"); // 역할 등록 (필수)
        };

        ws.OnError += (sender, e) =>
        {
            _mainThreadActions.Enqueue(() => {
                Debug.LogError("[WebSocket] 에러: " + e.Message);
            });
        };

        ws.OnClose += (sender, e) =>
        {
            _mainThreadActions.Enqueue(() => {
                Debug.Log("[WebSocket] 연결 종료.");
            });
        };

        // 5. WebSocket 메시지 수신 (핵심 로직)
        ws.OnMessage += (sender, e) =>
        {
            string jsonMsg = e.Data;

            // 임시로 'type' 필드만 파싱
            var baseMsg = JsonUtility.FromJson<SignalingMessage>(jsonMsg);

            // 중요: 수신된 메시지 처리를 메인 스레드에서 하도록 큐에 추가
            _mainThreadActions.Enqueue(() => {
                switch (baseMsg.type)
                {
                    case "offer":
                        Debug.Log("[WebSocket] 'offer' 수신.");
                        StartCoroutine(OnReceiveOffer(baseMsg.sdp));
                        break;
                    case "candidate":
                        Debug.Log("[WebSocket] 'candidate' 수신.");
                        var candMsg = JsonUtility.FromJson<IceCandidateMessage>(jsonMsg);
                        OnReceiveCandidate(candMsg);
                        break;
                    // [+] 신규 기능: 로봇 상태 수신
                    case "status":
                        var statusMsg = JsonUtility.FromJson<RobotStatusMessage>(jsonMsg);
                        UpdateRobotStatus(statusMsg);
                        break;

                    // [+] 신규 기능: 지도 이미지 수신
                    case "localization_image":
                        var mapMsg = JsonUtility.FromJson<RobotMapMessage>(jsonMsg);
                        UpdateRobotMap(mapMsg);
                        break;
                }
            });
        };

        // WebSocket 연결 시도
        ws.ConnectAsync();
    }

    void Update()
    {
        // 6. 매 프레임, 메인 스레드 큐에 쌓인 작업들을 실행
        while (_mainThreadActions.Count > 0)
        {
            _mainThreadActions.Dequeue().Invoke();
        }

    }

    private void OnDestroy()
    {
        // 앱 종료 시 자원 해제
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Close();
        }
        ws = null;

        
        pc?.Dispose();
        pc = null;

        // 캐시된 프레임 정리

        _remoteTexture = null;
    }
    // WebSocket으로 메시지 전송 (JSON 직렬화)
    private void SendWebSocketMessage<T>(T msg)
    {
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            string json = JsonUtility.ToJson(msg);
            // Debug.Log("[WebSocket] 메시지 발신: " + json);
            ws.Send(json); // SendAsync 대신 Send 사용
        }
    }

    // [+] 신규 기능 구현부: UI 업데이트 및 모드 제어
    // ========================================================================
    // ▼▼▼ [수정된 함수] TMP 텍스트 업데이트 로직 ▼▼▼
    private void UpdateRobotStatus(RobotStatusMessage msg)
    {
        // 1. 모드 상태 표시 ("Overall Status")
        if (statusText != null)
        {
            statusText.text = $"[Status]\n{msg.mode}"; // 예: [Status] VR
        }

        // 2. 에러 메시지 표시 ("Error Message")
        if (errorText != null)
        {
            if (msg.error)
            {
                errorText.text = "SYSTEM ERROR DETECTED";
                errorText.color = Color.red; // 에러나면 빨간색
            }
            else
            {
                errorText.text = "System Normal";
                errorText.color = Color.white; // 정상이면 흰색 (또는 원하는 색)
            }
        }

        // 3. (옵션) 패널 색상 변경
        if (errorPanel != null)
        {
            errorPanel.color = msg.error ? new Color(1, 0, 0, 0.3f) : new Color(0, 0, 0, 0.5f);
        }
    }
    // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲
    // 2. 지도 이미지 디코딩 및 표시 (Map)
    private void UpdateRobotMap(RobotMapMessage msg)
    {
        if (mapDisplay == null || string.IsNullOrEmpty(msg.data)) return;

        try
        {
            // Base64 -> Byte Array
            byte[] imageBytes = System.Convert.FromBase64String(msg.data);

            // Texture 생성 (크기는 LoadImage가 덮어씌우므로 임의 설정)
            Texture2D tex = new Texture2D(2, 2);

            // 이미지 로드 (JPG/PNG 디코딩)
            if (tex.LoadImage(imageBytes))
            {
                // UI RawImage에 적용
                mapDisplay.texture = tex;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Map Decode Error] {ex.Message}");
        }
    }

    // 3. 모드 변경 요청 송신 (UI 버튼 연결용 Public 함수)
    // 버튼 OnClick 이벤트에 연결하여 "AUTO" 또는 "VR" 문자열을 인자로 넘기세요.
    public void SetOperationMode(string modeName)
    {
        var msg = new RobotModeCommand { mode = modeName };
        SendWebSocketMessage(msg);
        Debug.Log($"[Command] 모드 변경 요청 전송: {modeName}");
    }
    // ========================================================================

    // 외부 스크립트에서 로봇 제어 명령을 보낼 때 사용하는 헬퍼
    public void SendControl(float linear, float angular)
    {
        if (!enableControl) return;
        if (ws == null || ws.ReadyState != WebSocketState.Open) return;

        var msg = new ControlMessage
        {
            // type = "control" 기본값 유지
            linear = linear,
            angular = angular
        };
        string json = JsonUtility.ToJson(msg);
        ws.Send(json);
        // Debug.Log($"[Control] sent: {json}");
    }

    //
    // --- (이하 WebRTC 관련 로직은 이전과 동일) ---
    //

    private RTCConfiguration GetSelectedSdpSemantics()
    {
        RTCConfiguration config = default;
        config.iceServers = new[] {
        new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } }
    };
        return config;
    }

    private IEnumerator OnReceiveOffer(string sdp)
    {
        var config = GetSelectedSdpSemantics();

        pc = new RTCPeerConnection(ref config);

        pc.OnIceCandidate = (candidate) =>
        {
            Debug.Log("[WebRTC] Unity ICE 후보 생성됨. Jetson으로 전송.");
            SendWebSocketMessage(new IceCandidateMessage(candidate));
        };

        pc.OnTrack = (e) =>
        {
            if (e.Track.Kind == TrackKind.Video)
            {
                Debug.Log("[WebRTC] 비디오 트랙 수신!");
                var videoTrack = (VideoStreamTrack)e.Track;

                videoTrack.OnVideoReceived += (texture) =>
                {
                                     
                    // WebRTC가 이후에도 이 텍스처를 계속 업데이트하므로
                    // 레퍼런스만 잡아두기만 하면 됨 (GPU 전용).
                    Debug.Log($"[WebRTC] OnVideoReceived first frame. tex={texture} {texture.width}x{texture.height}");
                    _remoteTexture = texture;

                    // 디버그용 Quad에는 원본 텍스처 그대로 사용
                    if (targetRenderer != null)
                        targetRenderer.material.mainTexture = texture;
                };
            }
        };


        var offer = new RTCSessionDescription { type = RTCSdpType.Offer, sdp = sdp };
        var opSetRemote = pc.SetRemoteDescription(ref offer);
        yield return opSetRemote;
        if (opSetRemote.IsError) { Debug.LogError($"[WebRTC] SetRemoteDescription 실패: {opSetRemote.Error.message}"); yield break; }

        Debug.Log("[WebRTC] SetRemoteDescription(offer) 성공.");

        var opCreateAnswer = pc.CreateAnswer();
        yield return opCreateAnswer;
        if (opCreateAnswer.IsError) { Debug.LogError($"[WebRTC] CreateAnswer 실패: {opCreateAnswer.Error.message}"); yield break; }

        var answer = opCreateAnswer.Desc;

        var opSetLocal = pc.SetLocalDescription(ref answer);
        yield return opSetLocal;
        if (opSetLocal.IsError) { Debug.LogError($"[WebRTC] SetLocalDescription(answer) 실패: {opSetLocal.Error.message}"); yield break; }

        Debug.Log("[WebRTC] SetLocalDescription(answer) 성공.");

        Debug.Log("[WebRTC] 'answer' 생성 완료. Jetson으로 전송.");
        SendWebSocketMessage(new SignalingMessage { type = "answer", sdp = answer.sdp });
    }

    private void OnReceiveCandidate(IceCandidateMessage msg)
    {
        if (pc == null) return;

        // 1) Init 객체에 값 채우기
        var candidateInit = new RTCIceCandidateInit
        {
            candidate = msg.candidate,
            sdpMid = msg.sdpMid,
            sdpMLineIndex = msg.sdpMLineIndex
        };

        // 2) 이 Init으로 RTCIceCandidate 생성
        var candidate = new RTCIceCandidate(candidateInit);

        // 3) 패키지 버전에 따라 호출 방식
        // 최신 버전(2.3.3+/3.x)은:
        pc.AddIceCandidate(candidate);

        // 만약 에러 메시지가
        // "인수 1을 'ref' 한정자로 전달해야 합니다" 라고 뜨면,
        // pc.AddIceCandidate(ref candidate);  // ← 이렇게 바꾸면 됩니다 (옛 버전)
    }
    
}