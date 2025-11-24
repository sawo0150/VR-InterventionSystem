using UnityEngine;
using UnityEngine.InputSystem;

public class ActionMapSwitcher : MonoBehaviour
{
    [Header("Input Action Asset")]
    [Tooltip("현재 사용 중인 Input Action 에셋을 연결하세요.")]
    public InputActionAsset inputAsset;

    [Header("Map Names")]
    [Tooltip("평소에 켜져있는 기본 맵 이름들 (예: XRI LeftHand Locomotion 등)")]
    public string[] defaultMaps = new string[] { "XRI LeftHand Locomotion", "XRI RightHand Locomotion", "XRI Default" };

    [Tooltip("특정 구역에서만 켤 맵 이름 (예: SpecialZoneMap)")]
    public string specialMap = "SpecialZoneMap";

    // 플레이어(XR Origin)를 감지하기 위한 태그 설정이 있다면 더 좋습니다.
    private void OnTriggerEnter(Collider other)
    {
        // 충돌한 대상이 플레이어인지 확인 (XR Origin에는 보통 "Player" 태그를 답니다)
        if (other.CompareTag("Player"))
        {
            SwitchToSpecialMode();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SwitchToDefaultMode();
        }
    }

    private void SwitchToSpecialMode()
    {
        Debug.Log("특정 구역 진입: 특수 조작 모드 활성화");

        // 1. 기존 기본 맵들을 끕니다. (이동/회전 불가)
        foreach (var mapName in defaultMaps)
        {
            var map = inputAsset.FindActionMap(mapName);
            if (map != null) map.Disable();
        }

        // 2. 특수 맵을 켭니다. (오른쪽 스틱만 작동)
        var special = inputAsset.FindActionMap(specialMap);
        if (special != null) special.Enable();
    }

    private void SwitchToDefaultMode()
    {
        Debug.Log("구역 이탈: 기본 조작 모드 복귀");

        // 1. 특수 맵을 끕니다.
        var special = inputAsset.FindActionMap(specialMap);
        if (special != null) special.Disable();

        // 2. 기본 맵들을 다시 켭니다.
        foreach (var mapName in defaultMaps)
        {
            var map = inputAsset.FindActionMap(mapName);
            if (map != null) map.Enable();
        }
    }
}