using UnityEngine;
using System.Collections.Generic;

namespace RealRobot{
    public class ObjectMoveManager : MonoBehaviour
    {
        [Header("--- 제어할 오브젝트들 ---")]
        [Tooltip("MovableObject 스크립트가 붙은 오브젝트들을 여기에 등록하세요.")]
        public List<MovableObject> objectsToControl; 

        private bool isSequenceStarted = false;

        // 외부(MasterManager 등)에서 호출하는 실행 함수
        public void StartMovementSequence()
        {
            if (!isSequenceStarted)
            {
                isSequenceStarted = true;
                Debug.Log("모든 오브젝트의 이동을 시작합니다.");

                // 리스트에 등록된 모든 MovableObject의 이동을 켭니다.
                foreach (var obj in objectsToControl)
                {
                    if (obj != null)
                    {
                        obj.BeginMove();
                    }
                }
            }
        }
    }
}