# VR-InterventionSystem

자동화된 시스템(자율주행 배달로봇) 운영 중 발생하는 예외 상황에 사용자가 개입할 수 있는 VR 원격 관제 시스템입니다.

---

## 🛠️ 개발 환경 (Environment)

* **Unity Version:** `6000.2.9f1`
* **Target Platform:** Meta Quest (VR)
* **Core Packages:**
    * `Unity AI Navigation` (NavMesh)
    * `Meta XR SDK`
    * `Unity Input System`

---

## 📋 프로젝트 개요 (Overview)

본 프로젝트는 다수의 자율주행 로봇이 시나리오를 수행하는 중, 예외 상황(Error Case)이 발생했을 때 사용자가 VR을 통해 1인칭으로 개입하여 원격 조종으로 문제를 해결하고 다시 자율주행 모드로 복귀시키는 시뮬레이션입니다.

---

## 📁 폴더 구조 (Folder Convention)

프로젝트의 확장성과 모듈화를 위해 **기능별(Feature-based) 하이브리드 컨벤션**을 따릅니다.
```

Assets/
│
├── [External_asset]/
│   \# Asset Store 등 외부에서 받은 에셋을 관리합니다. (수정 절대 금지)
│   \# 예: Demo City Scene, Meta Quest SDK 등
│
├── [Projects]/
│   \# 우리 팀이 직접 제작하는 모든 핵심 로직과 에셋이 위치합니다.
│   │
│   ├── Core/
│   │   \# 게임의 핵심 시스템 및 공용 스크립트
│   │   \# 예: GameManager, SceneLoader, StateMachineBase.cs 등
│   │
│   ├── Features/
│   │   \# 프로젝트의 주요 기능 모듈
│   │   │
│   │   ├── 🤖 Robot/
│   │   │   \# 배달 로봇 관련 모든 리소스
│   │   │   ├── Scripts/ (RobotController.cs, States/, Movement.cs...)
│   │   │   ├── Prefabs/ (DeliveryRobot.prefab)
│   │   │   ├── Materials/
│   │   │   └── Models/
│   │   │
│   │   ├── 👓 VR\_Controller/
│   │   │   \# VR 사용자(관제사) 관련 리소스
│   │   │   ├── Scripts/ (VR\_Input.cs, TeleportController.cs...)
│   │   │   └── Prefabs/ (VR\_Rig.prefab)
│   │   │
│   │   ├── 🌍 World/
│   │   │   \# 상호작용 가능한 월드 환경 요소
│   │   │   ├── Scripts/ (TrafficLight.cs, TrafficLightTrigger.cs...)
│   │   │   └── Prefabs/ (TrafficLight.prefab)
│   │   │
│   │   └── 🖥️ UI/
│   │       \# VR UI, 로봇 상태창 등 관련 리소스
│   │       ├── Scripts/
│   │       ├── Prefabs/
│   │       └── Fonts/
│   │
│   └── SharedAssets/
│       \# 여러 모듈에서 공통으로 사용하는 리소스 (코드가 아닌 에셋)
│       ├── Materials/ (Common\_Metal.mat)
│       ├── Textures/ (Noise.png)
│       └── Fonts/
│
├── [Scenes]/
│   \# 모든 씬(.unity) 파일을 관리합니다.
│   ├── Main.unity
│   ├── Test\_RobotNavigation.unity
│   └── Test\_VR\_Intervention.unity
│
└── Editor/
\# Unity 에디터 확장 스크립트 (커스텀 인스펙터 등)

```


### **주요 원칙**

1.  **[External] 수정 금지:** 외부 에셋은 업데이트 시 덮어써야 하므로 절대 직접 수정하지 않습니다.
2.  **기능별 모듈화:** 새로운 기능(예: '드론')을 추가할 때는 `[Project]/Features/` 안에 `Drone/` 폴더를 새로 생성하고, 관련된 모든 리소스를 그 안에 구성합니다.
3.  **공용 리소스 분리:** 2개 이상의 모듈에서 공통으로 사용되는 스크립트는 `[Project]/Core/`에, 에셋(재질, 텍스처 등)은 `[Project]/SharedAssets/`에 배치합니다.

---

## 🚀 시작하기 (Getting Started)

1.  본 레포지토리를 Clone 받습니다.
2.  Unity Hub에서 **Unity `6000.2.9f1`** 버전으로 프로젝트를 엽니다.
3.  Unity가 `Packages/manifest.json` 파일을 읽어들여 필요한 모든 패키지를 자동으로 설치할 때까지 기다립니다.
4.  `[Scenes]` 폴더의 테스트 씬을 열어 실행합니다.