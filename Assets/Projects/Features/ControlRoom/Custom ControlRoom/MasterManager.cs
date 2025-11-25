using UnityEngine;
using System.Collections.Generic;

namespace RealRobot
{
    public class MasterManager : MonoBehaviour
    {
        [Header("1. 미니맵 제어 매니저")]
        public ObjectMoveManager moveManager;

        [Header("2. 위치 제어 매니저 (새로 추가됨)")]
        public LocationSwitcher locationSwitcher; // ★ 인스펙터에서 LocationSwitcher 오브젝트를 여기에 넣으세요!

        [Header("3. 제어할 트리거 리스트")]
        public List<ActionTrigger> triggers;

        void Start()
        {
            // ------------------------------------------------------------
            // 기능 0: 게임 시작 시 초기 위치로 이동 (요청하신 기능)
            // ------------------------------------------------------------
            if (locationSwitcher != null)
            {
                // 시작하자마자 장소 1로 이동시킵니다.
                locationSwitcher.GoToLocation0();
                Debug.Log("MasterManager: 초기 위치(Location 0)로 설정을 완료했습니다.");
            }
            else
            {
                Debug.LogWarning("LocationSwitcher가 연결되지 않아 초기 위치 이동을 실패했습니다.");
            }

            // ------------------------------------------------------------
            // 기능 1: 미니맵 오브젝트 이동 로직 시작
            // ------------------------------------------------------------
            if (moveManager != null)
            {
                moveManager.StartMovementSequence();
            }

            // ------------------------------------------------------------
            // 기능 2: 등록된 모든 트리거 활성화 (리스너 등록)
            // ------------------------------------------------------------
            // 이 루프가 돌아야 버튼/키 입력이 비로소 작동하기 시작합니다.
            foreach (var trigger in triggers)
            {
                if (trigger != null)
                {
                    trigger.ActivateTrigger(); 
                }
            }
        }
    }
}