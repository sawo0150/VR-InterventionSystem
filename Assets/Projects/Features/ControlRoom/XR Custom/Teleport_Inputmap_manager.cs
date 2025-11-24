using UnityEngine;
using UnityEngine.InputSystem;

public class LocationSwitcher : MonoBehaviour
{
    [Header("타겟 설정")]
    public Transform xrOrigin;   
    public Camera mainCamera;    // ★ [중요] XR Origin 자식에 있는 Main Camera를 연결하세요!
    
    public Transform location1;  // 장소 1
    public Transform location2;  // 장소 2

    [Header("Input Action Asset")]
    public InputActionAsset inputAsset;

    [Header("Map 그룹 설정")]
    public string[] mapXNames = new string[] { "XRI Right Locomotion", "XRI Right Interaction" };
    public string[] mapYNames = new string[] { "MOD_ScreenRoom" };

    [Header("버튼 입력")]
    public InputActionReference buttonA_ToLoc2; 
    public InputActionReference buttonB_ToLoc1; 

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

    // ★★★ 가장 중요한 수정 부분 ★★★
    private void Teleport(Transform target)
    {
        if (xrOrigin == null || target == null || mainCamera == null) return;

        // 1. CharacterController 잠시 끄기 (물리 충돌 방지)
        CharacterController cc = xrOrigin.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // 2. 높이와 회전 맞추기
        // 일단 Origin을 타겟 위치로 보냅니다.
        xrOrigin.position = target.position;
        
        // 회전도 타겟(의자/스크린 방향)과 일치시킵니다.
        // (플레이어의 시선이 아니라 몸통 방향을 돌립니다)
        float rotationDiff = target.rotation.eulerAngles.y - mainCamera.transform.rotation.eulerAngles.y;
        xrOrigin.Rotate(0, rotationDiff, 0);

        // 3. [핵심] 룸스케일 오프셋 보정 (Head Position Compensation)
        // 현재 카메라가 Origin 중심에서 얼마나 떨어져 있는지 계산
        Vector3 cameraOffset = mainCamera.transform.position - xrOrigin.position;
        
        // 높이(Y) 차이는 무시하고 수평 거리만 계산 (바닥 기준 이동이므로)
        cameraOffset.y = 0; 

        // 그 차이만큼 Origin을 반대(뒤)로 당겨줍니다.
        xrOrigin.position -= cameraOffset;

        // 4. 물리 동기화 및 CC 다시 켜기
        Physics.SyncTransforms(); // 즉시 물리 위치 갱신
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