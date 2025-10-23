using UnityEngine;
using UnityEngine.AI; // AI 네임스페이스 추가 (NavMeshAgent 사용)

[RequireComponent(typeof(NavMeshAgent))] // 이 스크립트는 NavMeshAgent가 필수임을 명시
public class RobotWaypointFollower : MonoBehaviour
{
    // Inspector 창에서 웨이포인트들을 순서대로 드래그 앤 드롭할 배열
    public Transform[] waypoints;

    private NavMeshAgent agent;
    private int currentWaypointIndex = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 로봇에 붙어있는 NavMeshAgent 컴포넌트를 가져옴
        agent = GetComponent<NavMeshAgent>();

        // 웨이포인트가 하나라도 설정되어 있는지 확인
        if (waypoints.Length == 0)
        {
            Debug.LogError("웨이포인트가 설정되지 않았습니다.");
            return;
        }

        // 첫 번째 웨이포인트로 이동 시작
        GoToNextWaypoint();

    }

    // Update is called once per frame
    void Update()
    {
        // 에이전트가 경로 계산을 끝냈고 (pathPending)
        // 현재 목표 지점까지의 남은 거리가 stoppingDistance보다 작거나 같으면 (목표에 도착했다면)
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            // 다음 웨이포인트로 이동
            GoToNextWaypoint();
        }

    }
    void GoToNextWaypoint()
    {
        // 웨이포인트가 없으면 함수 종료
        if (waypoints.Length == 0) return;

        // NavMeshAgent의 목적지(destination)를 현재 웨이포인트 위치로 설정
        agent.destination = waypoints[currentWaypointIndex].position;

        // 다음 웨이포인트 인덱스로 업데이트
        // (waypoints.Length)로 나눈 나머지를 사용하면 배열의 끝에 도달했을 때 
        // 자동으로 0번 인덱스(처음)로 돌아가게 됩니다. (순환)
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }
}
