using UnityEngine;

public class LockXROriginPosition : MonoBehaviour
{
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;   // 시작 위치 저장
    }

    void LateUpdate()
    {
        // 회전은 허용하고 위치만 고정
        transform.position = startPos;
    }
}