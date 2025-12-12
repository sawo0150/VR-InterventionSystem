using UnityEngine;

public class ChildrenCrossingRoad : MonoBehaviour
{
    public enum MoveStyle
    {
        Walk,
        Run
    }

    [Header("Move Style")]
    public MoveStyle moveStyle = MoveStyle.Walk;

    [Header("Offsets from Boy (Local)")]
    public Vector3 offsetA = new Vector3(-2f, 0f, 0f);
    public Vector3 offsetB = new Vector3(2f, 0f, 0f);

    [Header("Move Settings (auto set)")]
    [Range(0.1f, 20f)]
    public float speed = 3f;

    private Vector3 pointAWorld;
    private Vector3 pointBWorld;
    private Vector3 currentTarget;

    private Animator animator;

    private void Reset()
    {
        offsetA = new Vector3(-2f, 0f, 0f);
        offsetB = new Vector3(2f, 0f, 0f);
        moveStyle = MoveStyle.Walk;
    }

    public void Start()
    {
        animator = GetComponent<Animator>();   // ★ Animator 가져오기

        // 스타일에 따라 속도 설정
        switch (moveStyle)
        {
            case MoveStyle.Walk:
                speed = 2f;
                break;
            case MoveStyle.Run:
                speed = 4f;
                break;
        }

        // ★ Animator 파라미터 세팅 (이름은 Animator 창과 반드시 동일해야 함)
        if (animator != null)
        {
            animator.SetBool("IsWalking", moveStyle == MoveStyle.Walk);
            animator.SetBool("IsRunning", moveStyle == MoveStyle.Run);
        }

        // 위치 계산
        Vector3 center = transform.position;
        pointAWorld = center + offsetA;
        pointBWorld = center + offsetB;

        transform.position = pointAWorld;
        currentTarget = pointBWorld;
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;

        transform.position = Vector3.MoveTowards(transform.position, currentTarget, step);

        if (Vector3.Distance(transform.position, currentTarget) < 0.01f)
        {
            currentTarget = (currentTarget == pointAWorld) ? pointBWorld : pointAWorld;
            transform.Rotate(0f, 180f, 0f);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        // DeliveryBot에 "Player" 또는 "DeliveryBot" 태그를 달아둔다고 가정
        if (other.CompareTag("Robot"))
        {
            Debug.Log("Boy와 DeliveryBot이 만났습니다.");

            // 여기서 네가 이미 만든 리스폰 시스템을 호출
            // 예: other.GetComponent<DeliveryBotRespawn>().Respawn();
        }
    }
}
