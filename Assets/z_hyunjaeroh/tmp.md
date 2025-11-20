
## Logic Flows

- 초기화
  - `GameManager` 시작, MonitoringScene, SimulationScene 로드 (`LoadScenesSequence()`)
  - `SimulationSceneManager`에서 `GameManager.InitializeSimulationData()` 호출, 시뮬레이션 씬에 있는 로봇과 좌석(플레이어 이동시킬 Transform)을 `GameManger`에 등록
    - 게임오브젝트 명시적으로 켜기: `robotObject.SetActive(true)`
    - 제어 스크립트 끄기: `wheelController.enabled = false`
    - 로봇 상태 설정: `robotData.state = Auto`
  - `MonitoringSceneManager`에서
    - 버튼 리스너 연결: `InitializeButtons()`
    - UI 상태 초기화: `ResetUIState()`
    - 튜토리얼 시작: `BeginTutorial()` (`TutorialUIController`에서 관리)
    - 플레이어를 관제실 리스폰위치로 이동:
      - `GameManager.MovePlayer(respawnAnchor)`를 호출하여, MonitoringScene 에 연결된 리스폰 포인트로 플레이어 오브젝트 이동
    - 이제 플레이어에게 튜토리얼 패널이 보여지고, 튜토리얼이 끝난 경우 메인 모니터링 패널이 보여짐
- 이벤트(시나리오) 시작
    - Case: 관제실에서 `Event # Trigger`버튼 클릭
    - *Case 2 (TODO): 이전 이벤트가 끝나거나 아니면 로봇이 특정 위치에 도달한 경우?*
    - `StartGameEvent(int eventId)`를 호출하여, 해당 id를 찾아
      - eventState 변경 (`Standby` -> `Active`)
      - 로봇 상태 변경 (`Auto` -> `Manual`) 
- 로봇 탑승 및 조작
  - Case: 관제실 메인 모니터링 패널에서 아래쪽 로봇 카메라 화면 클릭 
  - `MonitoringSceneManager.OnRobotCamClicked(int robotId)`에서  `GameManager.BoardRobot(robotId)` 호출
  - `GameManager`에서,
    - 플레이어 상태 변경 (`Monitoring` -> `Controlling`)
    - *VR 기능 제어*
    - 로봇 태그 `Player`로 변경 (트리거 인식용)
    - 로봇 상태가 `Manual`인 경우,  `wheelController` 활성화 (Auto 인 경우 비활성화, 로봇 시점 따라가며 볼 수 만 있게)
    - `MovePlayer()`호출하여 로봇 좌석 위치로 플레이어 이동
- 관제실 복귀
  - Case1: 컨트롤러 버튼 (시뮬레이터에서 키보드 1 / 점프액션 / 키보드 esc) -> `HandleReturnInput()` -> `ReturnToMonitoring(ReturnFlag flag)`
    - 시나리오 중이었다면 `flag = interrupt` -> 일단 fail 과 동일하게 처리
    - 그냥 오토모드 관전 중이었다면 `flag = none` -> 플레이어 이동만 처리
    - ** `PlayerState`가 `Controlling`인 경우에만 발생 
  - *Case 2 (TODO): 시뮬레이션 씬에서 목표 지점에 도달* -> `ReturnToMonitoring(flag = completed)`
  - *Case 3 (TODO): 시뮬레이션 씬에서 어디 굴러떨어지거나 멀리 이동* -> `ReturnToMonitoring(flag = failed)`
  - `currentActiveScenarioData != null`: 이벤트를 수행하고 (성공하던 실패하던) 온 경우
    - 해당 `EventState` 변경 (Active -> Completed/Failed)
    - `robotState` Auto 로 변경
    - `wheelController` 끄기, 로봇 태그 `Untagged`로 변경
    - `currentActiveScenarioData = null`로 해제
  - `currentActiveScenarioData`가 없는 경우 (그냥 관전모드) / 또는 위에서 현재 시나리오 해제한 이후 공통 처리
    - *VR 기능 제어*
    - 플레이어 상태 변경 (`Controlling` -> `Monitoring`)
    - 플레이어를 관제실 리스폰 위치로 이동 (`MonitoringSceneManager.MovePlayerToRespawnAnchor()`호출)


    

## 기타
- `MyDebug.cs`: 디버깅 상황에만 로그 찍도록 작성된 스크립트
- `MyMemo.cs`: 오브젝트 inspector 에서 추가하고 싶은 설명이나 메모 있을 때 쓰는 스크립트
- ** 모든 스크립트는 namespace Project 내부에 작성됨
- 신로드 안될경우 Build Profile - Scene List 확인 (세 씬 순서대로 추가)
- LoaderScene 의 XR Origin 말고는 다 꺼놔야 따로 오류 X (작업할 때 다른 씬에서 사용한 XR Origin 이나 메인카메라는 꺼져 있어야 함)


```
    public enum PlayerState
    {
        MonitoringMode,
        ControllingMode,
    }

    public enum RobotState { 
        Auto, 
        Manual, 
    }

    public enum EventState
    {
        Standby, 
        Active, 
        Completed, // Resolved 
        Failed,
    }

    public enum ReturnFlag
    {
        None,
        Interrupt,
        Completed,
        Failed,
    }

    [System.Serializable]
    public class ScenarioData
    {
        public int id;
        [TextArea] public string description;
        public EventState eventState = EventState.Standby;
        public GameObject robotObject;
        public Transform seatAnchor;
        public RobotState robotState = RobotState.Auto;
        public RobotWheelController robotWheelController;
        
        public ScenarioData(int id, GameObject obj, Transform seatAnchor)
        {
            this.id = id;
            this.robotObject = obj;
            this.seatAnchor = seatAnchor;
            this.robotWheelController = obj.GetComponentInChildren<RobotWheelController>();
        }
    }
```