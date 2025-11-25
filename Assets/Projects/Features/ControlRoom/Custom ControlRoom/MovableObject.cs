using UnityEngine;

// 축 선택을 위한 열거형 (공용)
namespace RealRobot{
    public enum MoveAxis
    {
        X_Axis,
        Y_Axis,
        Z_Axis
    }

    public class MovableObject : MonoBehaviour
    {
        [Header("개별 설정")]
        [Tooltip("이 오브젝트가 움직일 축을 선택하세요")]
        public MoveAxis axis;          // 축 선택

        [Tooltip("이 오브젝트의 이동 속도")]
        public float speed = 5.0f;     // 속도

        // 이동 상태 확인용 변수
        private bool shouldMove = false;

        // 매니저가 이 함수를 호출하면 이동을 시작함
        public void BeginMove()
        {
            shouldMove = true;
        }

        void Update()
        {
            if (!shouldMove) return;

            // 선택된 축에 따라 방향 설정
            Vector3 direction = Vector3.zero;

            switch (axis)
            {
                case MoveAxis.X_Axis:
                    direction = Vector3.right;   // (1, 0, 0)
                    break;
                case MoveAxis.Y_Axis:
                    direction = Vector3.up;      // (0, 1, 0)
                    break;
                case MoveAxis.Z_Axis:
                    direction = Vector3.forward; // (0, 0, 1)
                    break;
            }

            // 자기 자신(transform)을 이동시킴
            transform.Translate(direction * speed * Time.deltaTime);
        }
    }
}