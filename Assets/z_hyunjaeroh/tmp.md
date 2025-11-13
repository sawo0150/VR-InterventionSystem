



## LoaderScene

- GameManager (`GameManager.cs`)
  - 싱글톤 패턴 (GameManger.Instance)
  - 전체 흐름
    - 1 전체 씬 로드 (`LoadScenesSequence()`)
      - `Start()` 맨 마지막에 `StartCoroutine(LoadScenesSequence())` 와 같이 실행됨
      - `LoadSceneMode.Additive` 로 병렬 로딩
      - 일단 Simulation Scene 을 Active Scene 으로 설정 (조명이나 NavMesh 기준점이 되도록)
    - 2 플레이어를 MonitoringRoom 으로 이동 (`MovePlayerToMonitoringRoom()`)
      -  `LoadScenesSequence()` 맨 마지막에, 씬 로드 완료 후 실행됨
      - 해당 Target Anchor/Offset 의 자식으로 플레이어 오브젝트 이동 (`playerObject.SetParent()`)
      - (*** Target Anchor 는 Object 이름을 검색해서 찾는다 (MonitoringRoom Anchor Offset))
      - rotation, scale, position 0으로 초기화 (Target Offset 과 같아지도록)
  - Arguments   
    - `currentState`: 플레이어의 현재 상태 관리
      - MonitoringScene <-> SimulationScene 전환될 때 변경됨, 컨트롤러 버튼 입력 같은 거 체크용
      - 외부에서 읽으려면 CurrentState 사용 (GameManager.Instance.CurrentState)
      - Setter: GameManager.Instance.SetState(GameState.Controlling) 과 같이 사용
  - `enum GameState {Monitoring, Controlling}` 은 이 파일에 정의되어 있다.
- 플레이어 (`My Complete XR Origin (XR Rig)`)
  - XRI_Examples/Global/Prefabs 에서 프리팹 variant 를 Project 폴더로 복사해와서 설정 (원본 프리팹: XR Interaction Toolkit 의 XR Origin (XR Rig))
  - Locomotiom 에서 turn, move, gravity 제외하고 모두 해제
  - Locomotion inspector 에서, Locomotion Manager - Right Hand Turn Style 을 Snap 에서 Smooth 로 변경 
- `EventSystem`, `XR Interaction Manager`
  - XRI 사용하기 위한 오브젝트


## Monitoring Scene

- MonitoringSceneManager (`MonitoringSceneManager.cs`)
  - tmp
- UIController (`UIController.cs`)
  - tmp
- Tutorial Canvas
- Monitoring Canvas


## 기타

- `MyDebug.cs`: 디버깅 상황에만 로그 찍도록 작성된 스크립트
- `MyMemo.cs`: 오브젝트 inspector 에서 추가하고 싶은 설명이나 메모 있을 때 쓰는 스크립트
- ** 모든 스크립트는 namespace Project {} 에 감싸지도록 작성됨