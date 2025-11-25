using UnityEngine;
using UnityEngine.InputSystem;

public class LocationSwitcher : MonoBehaviour
{
    [Header("타겟 설정")]
    public Transform xrOrigin;   
    public Camera mainCamera;    // ★ [중요] XR Origin 자식에 있는 Main Camera를 연결하세요!
    
    public Transform location1;  // 장소 1 (시작 위치)
    public Transform location2;  // 장소 2

    [Header("Input Action Asset")]
    public InputActionAsset inputAsset;

    [Header("Map 이름 설정")]
    [Tooltip("텔레포트 버튼(A/B)이 들어있는 맵 이름 (반드시 켜져야 함)")]
    public string globalMapName = "Global"; // ★ 이 변수가 빠져 있어서 추가했습니다!

    [Header("Map 그룹 설정")]
    public string[] mapXNames = new string[] { "XRI Right Locomotion", "XRI Right Interaction" };
    public string[] mapYNames = new string[] { "MOD_ScreenRoom" };

    [Header("버튼 입력")]
    public InputActionReference buttonA_ToLoc2; 
    public InputActionReference buttonB_ToLoc1; 

    // ★★★ 씬 시작 시 초기화 로직 ★★★
    private void Start()
    {
        // 1. Global 맵(텔레포트 버튼용)은 무조건 켭니다.
        var globalMap = inputAsset.FindActionMap(globalMapName);
        if (globalMap != null) globalMap.Enable();
        else Debug.LogWarning($"'{globalMapName}' 맵을 찾을 수 없습니다. Global 맵 이름을 확인하세요.");

        // 2. 시작 위치인 '장소 1' 상태로 강제 설정
        // (Y 그룹(스크린룸)은 끄고, X 그룹(이동)은 켭니다)
        Debug.Log("씬 시작: 장소 1(기본) 모드로 초기화합니다.");
        SwitchInputMaps(disableMaps: mapYNames, enableMaps: mapXNames);
    }
        
    private void OnEnable()
    {
        if (buttonA_ToLoc2 != null)
        {
            buttonA_ToLoc2.action.Enable();
            buttonA_ToLoc2.action.performed += OnButtonAPressed;
        }

        if (buttonB_ToLoc1 != null)
        {
            buttonB_ToLoc1.action.Enable();
            buttonB_ToLoc1.action.performed += OnButtonBPressed;
        }
    }

    private void OnDisable()
    {
        if (buttonA_ToLoc2 != null)
        {
            buttonA_ToLoc2.action.performed -= OnButtonAPressed;
            buttonA_ToLoc2.action.Disable();
        }

        if (buttonB_ToLoc1 != null)
        {
            buttonB_ToLoc1.action.performed -= OnButtonBPressed;
            buttonB_ToLoc1.action.Disable();
        }
    }

    private void OnButtonAPressed(InputAction.CallbackContext context)
    {
        Teleport(location2);
        SwitchInputMaps(disableMaps: mapXNames, enableMaps: mapYNames);
        Debug.Log(">>> 장소 2 이동");
    }

    private void OnButtonBPressed(InputAction.CallbackContext context)
    {
        Teleport(location1);
        SwitchInputMaps(disableMaps: mapYNames, enableMaps: mapXNames);
        Debug.Log(">>> 장소 1 이동");
    }

    // ★★★ 텔레포트 + 위치 보정 로직 ★★★
    private void Teleport(Transform target)
    {
        if (xrOrigin == null || target == null || mainCamera == null) return;

        // 1. CharacterController 잠시 끄기 (물리 충돌 방지)
        CharacterController cc = xrOrigin.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // 2. 높이와 회전 맞추기
        xrOrigin.position = target.position;
        
        // 회전도 타겟(의자/스크린 방향)과 일치시킵니다.
        float rotationDiff = target.rotation.eulerAngles.y - mainCamera.transform.rotation.eulerAngles.y;
        xrOrigin.Rotate(0, rotationDiff, 0);

        // 3. [핵심] 룸스케일 오프셋 보정 (Head Position Compensation)
        Vector3 cameraOffset = mainCamera.transform.position - xrOrigin.position;
        cameraOffset.y = 0; 
        xrOrigin.position -= cameraOffset;

        // 4. 물리 동기화 및 CC 다시 켜기
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