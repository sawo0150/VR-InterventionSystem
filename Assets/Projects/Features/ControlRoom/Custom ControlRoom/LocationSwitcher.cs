using UnityEngine;
using UnityEngine.InputSystem;


namespace RealRobot{
    public class LocationSwitcher : MonoBehaviour
    {
        [Header("타겟 설정")]
        public Transform xrOrigin;
        public Camera mainCamera;    // XR Origin 자식에 있는 Main Camera
        
        [Header("미리 정의된 장소들")]
        public Transform location0;
        public Transform location1;  // 장소 1
        public Transform location2;  // 장소 2

        [Header("Input Action Asset")]
        public InputActionAsset inputAsset;

        [Header("Map 이름 설정")]
        public string globalMapName = "Global"; // 항상 켜져있어야 하는 맵
        public string[] mapXNames = new string[] { "XRI Right Locomotion", "XRI Right Interaction" }; // 장소 1용 맵
        public string[] mapYNames = new string[] { "MOD_ScreenRoom" }; // 장소 2용 맵

        // ★ 시작 시 기본 설정 (Global 맵 켜기 등)
        private void Start()
        {
            // 1. Global 맵 활성화
            var globalMap = inputAsset.FindActionMap(globalMapName);
            if (globalMap != null) globalMap.Enable();

            // 2. 시작 시 장소 1 상태로 초기화 (원한다면 유지)
            GoToLocation1();
        }

        // ========================================================================
        // ★ ActionTrigger의 On Triggered()에 넣을 함수들 (Public)
        // ========================================================================

        public void GoToLocation0()
        {
            Debug.Log(">>> 장소 0로 이동 명령 수신");
            Teleport(location0);
            SwitchInputMaps(disableMaps: mapYNames, enableMaps: mapXNames);
        }

        public void GoToLocation1()
        {
            Debug.Log(">>> 장소 1로 이동 명령 수신");
            Teleport(location1);
            SwitchInputMaps(disableMaps: mapXNames, enableMaps: mapYNames);
        }

        public void GoToLocation2()
        {
            Debug.Log(">>> 장소 2로 이동 명령 수신");
            Teleport(location2);
            SwitchInputMaps(disableMaps: mapXNames, enableMaps: mapYNames);
        }

        // 기능 3: (고급) 인스펙터에서 Transform을 직접 넣어서 이동만 하고 싶을 때
        // Map 변경 없이 위치만 옮기고 싶다면 이 함수를 연결하고 인자를 넣으면 됨
        public void TeleportOnly(Transform target)
        {
            Teleport(target);
        }

        // ========================================================================
        // 내부 로직 (Private) - 복잡한 처리는 숨김
        // ========================================================================

        private void Teleport(Transform target)
        {
            if (xrOrigin == null || target == null || mainCamera == null) return;

            // 1. CharacterController 잠시 끄기
            CharacterController cc = xrOrigin.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // 2. 높이와 회전 맞추기
            xrOrigin.position = target.position;

            // 회전 보정
            float rotationDiff = target.rotation.eulerAngles.y - mainCamera.transform.rotation.eulerAngles.y;
            xrOrigin.Rotate(0, rotationDiff, 0);

            // 3. 룸스케일 오프셋 보정
            Vector3 cameraOffset = mainCamera.transform.position - xrOrigin.position;
            cameraOffset.y = 0;
            xrOrigin.position -= cameraOffset;

            // 4. 물리 동기화 및 CC 켜기
            Physics.SyncTransforms();
            if (cc != null) cc.enabled = true;
        }

        private void SwitchInputMaps(string[] disableMaps, string[] enableMaps)
        {
            foreach (string mapName in disableMaps)
            {
                var map = inputAsset.FindActionMap(mapName);
                if (map != null) map.Disable();
            }
            foreach (string mapName in enableMaps)
            {
                var map = inputAsset.FindActionMap(mapName);
                if (map != null) map.Enable();
            }
        }
    }
}