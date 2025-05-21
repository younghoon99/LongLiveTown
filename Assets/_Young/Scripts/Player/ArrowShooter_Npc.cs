using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YS.Effects;

public class ArrowShooter_Npc : MonoBehaviour
{
    public static bool isBowEquipped = false;
    public static int arrowDamage;
    public GameObject arrowPrefab; // 화살 프리팹
    public Transform shootPoint;  // 화살이 발사될 위치
    public int poolSize = 10; // 오브젝트 풀 크기
    public float arcHeight = 2f; // 반원 또는 타원의 높이
    public float maxShootDistance = 10f; // 발사 가능 최대 X축 거리


    private Queue<GameObject> arrowPool = new Queue<GameObject>(); // 화살 오브젝트 풀
    private UIManager uiManager; // UIManager 참조

    // 마지막 활 발사 시각 저장용 변수 (1초 쿨타임 체크)
    private float lastShootTime = -999f;

    private Npc npc;

    void Start()
    {
        // Arrow 부모 오브젝트 찾기 또는 생성
        GameObject arrowParentObj = GameObject.Find("Arrow");
        if (arrowParentObj == null)
            arrowParentObj = new GameObject("Arrow");
        Transform arrowParent = arrowParentObj.transform;

        // 오브젝트 풀 초기화
        for (int i = 0; i < poolSize; i++)
        {
            GameObject arrow = Instantiate(arrowPrefab, arrowParent);
            arrow.SetActive(false);
            arrowPool.Enqueue(arrow);
        }

        uiManager = FindObjectOfType<UIManager>();
        if (uiManager == null)
        {
            Debug.LogError("UIManager를 찾을 수 없습니다!");
        }
        shootPoint = this.transform;
        npc = GetComponent<Npc>();
    }

    void Update()
    {
        // NPC 자동 발사: 가장 가까운 몹이 일정 거리 이내면 쿨타임마다 자동 발사
        GameObject targetMob = FindNearestMob();
        if (targetMob == null) return;

        float distance = Mathf.Abs(shootPoint.position.x - targetMob.transform.position.x);
        if (distance > maxShootDistance) return;

        if (npc.bow.activeSelf == true)
        {
            isBowEquipped = true;
            arrowDamage = npc.attackDamage;
        }
        else
        {
            isBowEquipped = false;
        }



        if (Time.time - lastShootTime >= 1.0f) // 쿨타임 1초, 필요시 변수화
        {
            if (isBowEquipped && ShootArrow())
            {
                lastShootTime = Time.time;
                GameManager.instance.PlaySFX("ArrowAttack");
            }
        }
    }

    // 성공 시 true, 실패(적 없음) 시 false 반환
    public bool ShootArrow()
    {
        // 가장 가까운 몹 찾기
        GameObject targetMob = FindNearestMob();
        if (targetMob == null) return false;

        // 플레이어와 몹 간의 X축 거리 계산
        float xDistanceToMob = Mathf.Abs(shootPoint.position.x - targetMob.transform.position.x);
        Debug.LogWarning($"현재 X축 거리: {xDistanceToMob}"); // 디버그 로그로 거리 확인

        if (xDistanceToMob > maxShootDistance)
        {
            Debug.Log("몹이 너무 멀리 있어 화살을 발사할 수 없습니다.");
            if (uiManager != null)
            {
                uiManager.ShowShootDistText("몹 가까이에서 발사하세요!");
            }
            return false;
        }

        // 화살 가져오기
        GameObject arrow = GetPooledArrow();
        if (arrow == null) return false;

        // 화살 초기화
        arrow.transform.SetParent(GameObject.Find("Arrow").transform); // Arrow 오브젝트의 자식으로 설정
        arrow.transform.position = shootPoint.position;
        arrow.transform.rotation = Quaternion.identity;
        arrow.SetActive(true);

        // 화살의 EffectCollisionHandler에 NPC별 공격력 세팅
        EffectCollisionHandler handler = arrow.GetComponent<EffectCollisionHandler>();
        if (handler != null)
        {
            handler.isNpcArrow = true; // NPC가 쏜 화살임을 표시
            handler.npcAttackDamage = npc.attackDamage; // 이 NPC의 공격력
        }

        // 타겟 위치 계산
        Vector3 startPosition = shootPoint.position + new Vector3(0, 2f, 0); // 플레이어 위치보다 Y축 2f 위
        Vector3 targetPosition = targetMob.transform.position + new Vector3(0, 2f, 0); // 몹 위치보다 Y축 2f 위

        // 타겟 방향 계산
        Vector2 direction = (targetMob.transform.position - shootPoint.position).normalized;

        // 화살의 SpriteRenderer 가져오기
        SpriteRenderer arrowRenderer = arrow.GetComponent<SpriteRenderer>();
        if (arrowRenderer != null)
        {
            // 타겟이 오른쪽에 있을 경우 flipX 설정 해제
            arrowRenderer.flipX = direction.x < 0;
        }

        // 화살 이동 코루틴 시작
        StartCoroutine(MoveArrowInArc(arrow, startPosition, targetPosition));

        // 정상적으로 화살 발사 성공 시 true 반환
        return true;

    }

    System.Collections.IEnumerator MoveArrowInArc(GameObject arrow, Vector3 start, Vector3 target)
    {
        float duration = 1.0f; // 화살이 이동하는 데 걸리는 시간
        float elapsedTime = 0f;

        // 방향에 따라 Z축 회전 각도 설정
        bool isShootingRight = target.x > start.x;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // 반원 또는 타원 경로 계산
            float x = Mathf.Lerp(start.x, target.x, t);
            float y = Mathf.Lerp(start.y, target.y, t) + arcHeight * Mathf.Sin(Mathf.PI * t); // 타원의 높이 추가
            arrow.transform.position = new Vector3(x, y-0.5f, start.z);

            // Z축 회전 계산
            float angle = isShootingRight
                ? Mathf.Lerp(60f, -60f, t) // 오른쪽으로 발사 시 60도에서 -60도로 회전
                : Mathf.Lerp(-60f, 60f, t); // 왼쪽으로 발사 시 -60도에서 60도로 회전
            arrow.transform.rotation = Quaternion.Euler(0, 0, angle);

            yield return null;
        }

        // 화살이 타겟 위치에 도달하면 비활성화
        arrow.SetActive(false);
        ReturnArrowToPool(arrow);
    }

    GameObject GetPooledArrow()
    {
        if (arrowPool.Count > 0)
        {
            GameObject arrow = arrowPool.Dequeue();
            return arrow;
        }
        return null; // 풀에 사용 가능한 화살이 없으면 null 반환
    }

    public void ReturnArrowToPool(GameObject arrow)
    {
        arrow.SetActive(false);
        arrowPool.Enqueue(arrow);
    }

    public GameObject FindNearestMob()
    {
        if (MobManager.instance == null) return null;

        GameObject nearestMob = null;
        float closestDistance = float.MaxValue;

        foreach (GameObject mob in MobManager.instance.activeMobs)
        {
            if (!mob.activeInHierarchy) continue;

            // X축 거리만 계산
            float xDistance = Mathf.Abs(shootPoint.position.x - mob.transform.position.x);
            if (xDistance < closestDistance)
            {
                closestDistance = xDistance;
                nearestMob = mob;
            }
        }

        return nearestMob;
    }
}
