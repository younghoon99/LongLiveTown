using System.Collections;
using System.Collections.Generic;
using Kinnly;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI; // UI 관련 기능 사용

public class Player : MonoBehaviour
{
    // 현재 플레이어가 장착한 아이템
    public Item equippedItem;

    //좌클릭시 공격이 나갈지 안나갈지 하는 변수
    public bool isAttack = true;


    // 플레이어 이동 관련 변수
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

    // 물리 및 상태 변수
    private Rigidbody2D rb;
    private bool isGrounded;
    [Header("플레이어 방향 설정")]
    [SerializeField] private bool isFacingRight = false;

    // 상호작용 거리 설정
    [Header("상호작용 설정")]
    [SerializeField] private float interactionDistance = 2.0f; // 플레이어와 Money 사이의 최대 상호작용 거리

    // 공격 설정
    [Header("공격 설정")]
    private float attackDamage;      
    [SerializeField] private float attackRange = 1.5f;      // 공격 범위
    [SerializeField] private Transform attackPoint;         // 공격 지점 (비어있으면 자동 생성)
    [SerializeField] private string enemyTag = "Enemy";     // 적 태그
    [SerializeField] private float attackDelay = 0.2f;      // 공격 애니메이션 후 데미지 적용 지연 시간
    [SerializeField] private float attackCooldown = 1f;   // 공격 쿨다운 시간 (애니메이션 종료 후 다시 공격 가능한 시간)
    private bool isAttacking = false;                       // 현재 공격 중인지 여부



    // 타일맵 관련 변수
    [Header("타일맵 설정")]
    [SerializeField] private Tilemap resourceTilemap; // Resource 타일맵 참조
    [SerializeField] private ResourceTileSpawner resourceTileSpawner; // ResourceTileSpawner 스크립트 참조

    // 카메라 관련 변수
    private Camera mainCamera;

    // 애니메이션 관련 변수
    [SerializeField] Animator animator;
    private float horizontalInput;

    // Start is called before the first frame update
    void Start()
    {
        // Start()에서는 attackDamage를 세팅하지 않음. 장착 아이템이 있을 때만 SetEquippedItem에서 갱신.

        // 컴포넌트 초기화
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        attackDamage = 0f;





        // Rigidbody2D 관성 제거
        if (rb != null)
        {
            rb.drag = 0f;         // 공기 저항 0으로 설정
            rb.gravityScale = 3f; // 중력 스케일 설정
            rb.freezeRotation = true; // 회전 방지
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // 부드러운 이동
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 연속 충돌 감지
            // 관성 제거를 위해 속도 즉시 적용
            rb.inertia = 0f;
        }

        if (attackPoint == null)
        {
            GameObject attack = new GameObject("AttackPoint");
            attack.transform.parent = transform;
            attack.transform.localPosition = new Vector3(1f, 0f, 0f); // 플레이어 앞쪽에 위치
            attackPoint = attack.transform;
            Debug.Log("AttackPoint 자동 생성됨");
        }

        // 카메라 참조가 없을 경우 메인 카메라로 설정
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("메인 카메라를 찾을 수 없습니다!");
                enabled = false; // 스크립트 비활성화
                return;
            }
        }
        IgnoreCollisionsWithEnemiesAndNpcs();


    }

    // Update is called once per frame
    void Update()
    {
        // 입력 값 받기
        horizontalInput = Input.GetAxis("Horizontal");

        if (Time.timeScale == 0) return; // 일시정지면 아무것도 하지 않음
        
            // 마우스 위치에 따른 플레이어 방향 설정
            FlipBasedOnMousePosition();

            // 마우스 좌클릭 입력 처리 - 공격 중이 아닐 때만 공격 가능
            if (Input.GetMouseButtonDown(0) && !isAttacking && isAttack)
            {
                Attack();
            }
            

            // E키 입력 처리 - Money 상호작용
            if (Input.GetKeyDown(KeyCode.E))
            {
                HandleMoneyInteraction();
            }

            // 애니메이션 파라미터 업데이트
            UpdateAnimationParameters();
        
    }
    
    // 외부에서 장착 아이템을 세팅하는 함수
    // 아이템 장착 시 데미지도 즉시 갱신
    public void SetEquippedItem(Item item)
    {
        equippedItem = item;
        if (equippedItem != null)
            attackDamage = equippedItem.Damage;
        else if (item == null)
            attackDamage = 1.0f; // 맨손 등 기본값

        if (item != null && item.isBow)
        {
            isAttack = false;
        }
    }

    // Resource 상호작용 처리 함수 (도끼/곡괭이/기타 세분화)
    private void HandleMoneyInteraction()
    {
        if (animator == null)
        {
            Debug.LogError("애니메이터 컴포넌트가 없습니다!");
            return;
        }

        // 1. 도끼: 나무만 채집
        if (equippedItem != null && equippedItem.isAxe)
        {
            // 나무 타일만 상호작용
            Vector3 stonePos = resourceTileSpawner.GetNearestStoneTilePosition(transform.position);
            Vector3 woodPos = resourceTileSpawner.GetNearestWoodTilePosition(transform.position);
            if (woodPos != Vector3.zero)
            {
                animator.SetTrigger("6_Other");
                resourceTileSpawner.ReserveWoodTile(woodPos);
                resourceTileSpawner.ReserveStoneTile(stonePos);
                StartCoroutine(RemoveClosestTileAfterDelay(3f, stonePos, ResourceType.Stone));
                StartCoroutine(RemoveClosestTileAfterDelay(3f, woodPos, ResourceType.Wood));
                Debug.Log("도끼로 나무 채집!");
            }
            else
            {
                Debug.Log("근처에 나무가 없습니다.");
            }
            return;
        }
        // 2. 곡괭이: 돌만 채집
        else if (equippedItem != null && equippedItem.isPickaxe)
        {
            Vector3 stonePos = resourceTileSpawner.GetNearestStoneTilePosition(transform.position);
            Vector3 woodPos = resourceTileSpawner.GetNearestWoodTilePosition(transform.position);
            if (stonePos != Vector3.zero)
            {
                animator.SetTrigger("6_Other");
                resourceTileSpawner.ReserveWoodTile(woodPos);
                resourceTileSpawner.ReserveStoneTile(stonePos);
                StartCoroutine(RemoveClosestTileAfterDelay(3f, stonePos, ResourceType.Stone));
                StartCoroutine(RemoveClosestTileAfterDelay(3f, woodPos, ResourceType.Wood));
                Debug.Log("곡괭이로 돌 채집!");
            }
            else
            {
                Debug.Log("근처에 돌이 없습니다.");
            }
            return;
        }
        // 3. 맨손일때
        else if (equippedItem == null)
        {
            Vector3 stonePos = resourceTileSpawner.GetNearestStoneTilePosition(transform.position);
            Vector3 woodPos = resourceTileSpawner.GetNearestWoodTilePosition(transform.position);
            if (stonePos != Vector3.zero)
            {
                animator.SetTrigger("6_Other");
                if (woodPos != Vector3.zero)
                {
                    resourceTileSpawner.ReserveWoodTile(woodPos);
                    StartCoroutine(RemoveClosestTileAfterDelay(5f, woodPos, ResourceType.Wood));
                }
                else if (stonePos != Vector3.zero)
                {
                    resourceTileSpawner.ReserveStoneTile(stonePos);
                    StartCoroutine(RemoveClosestTileAfterDelay(5f, stonePos, ResourceType.Stone));
                }
            }
            else
            {
                Debug.Log("근처에 돌이 없습니다.");
            }
            return;
        }
        // 4. 그 외(망치, 활 등): 상호작용 불가
        else
        {
            Debug.Log("현재 장착한 아이템으로는 자원 채집이 불가능합니다.");
            return;
        }
    }

    public enum ResourceType { Wood, Stone }

private IEnumerator RemoveClosestTileAfterDelay(float delay, Vector3 tileWorldPos, ResourceType type)
    {
        float elapsedTime = 0f;

        while (elapsedTime < delay)
        {
            // 캐는 도중 움직임 감지
            if (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(rb.velocity.x) > 0.1f)
            {
                Debug.Log("캐는 도중 움직임이 감지되어 작업이 취소되었습니다.");

                // 예약 해제(취소 시에도 반드시!)
                if (type == ResourceType.Wood)
                    resourceTileSpawner.ReleaseWoodTile(tileWorldPos);
                else if (type == ResourceType.Stone)
                    resourceTileSpawner.ReleaseStoneTile(tileWorldPos);

                // 캐는 애니메이션 취소 트리거 설정
                if (animator != null)
                {
                    animator.SetTrigger("CancelMining");
                }

                yield break; // 코루틴 종료
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (resourceTilemap == null || resourceTileSpawner == null)
        {
            Debug.LogError("Resource 타일맵 또는 ResourceTileSpawner가 설정되지 않았습니다!");
            // 예약 해제(안전)
            if (type == ResourceType.Wood)
                resourceTileSpawner.ReleaseWoodTile(tileWorldPos);
            else if (type == ResourceType.Stone)
                resourceTileSpawner.ReleaseStoneTile(tileWorldPos);
            yield break;
        }

        // 플레이어 위치를 기준으로 가장 가까운 타일 찾기
        Vector3Int closestTilePosition = FindClosestTile();
        if (closestTilePosition != Vector3Int.zero)
        {
            // ResourceTileSpawner에 타일 삭제 요청
            resourceTileSpawner.RemoveTile(closestTilePosition);

            // 예약 해제(정상 완료 시)
            if (type == ResourceType.Wood)
                resourceTileSpawner.ReleaseWoodTile(tileWorldPos);
            else if (type == ResourceType.Stone)
                resourceTileSpawner.ReleaseStoneTile(tileWorldPos);

            // 캐는 작업 완료 후 애니메이션 트리거 설정
            if (animator != null)
            {
                animator.SetTrigger("CancelMining");
            }

            Debug.Log("타일 캐기 완료!");
        }
        else
        {
            Debug.Log("가까운 타일을 찾을 수 없습니다.");
            // 예약 해제(혹시 모르니 안전하게)
            if (type == ResourceType.Wood)
                resourceTileSpawner.ReleaseWoodTile(tileWorldPos);
            else if (type == ResourceType.Stone)
                resourceTileSpawner.ReleaseStoneTile(tileWorldPos);
        }
    }

    private Vector3Int FindClosestTile()
    {
        Vector3Int closestTilePosition = Vector3Int.zero;
        float closestDistance = float.MaxValue;

        // Resource 타일맵의 모든 타일 좌표를 순회
        BoundsInt bounds = resourceTilemap.cellBounds;
        foreach (Vector3Int position in bounds.allPositionsWithin)
        {
            TileBase tile = resourceTilemap.GetTile(position);
            if (tile != null && (IsWoodTile(tile) || IsStoneTile(tile)))
            {
                // 플레이어와 타일 간의 거리 계산
                Vector3 worldPosition = resourceTilemap.CellToWorld(position);
                float distance = Vector3.Distance(transform.position, worldPosition);

                // 플레이어의 상호작용 거리(`interactionDistance`) 내에 있는 타일만 고려
                if (distance <= interactionDistance && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTilePosition = position;
                }
            }
        }

        // 상호작용 거리 내에 타일이 없으면 Vector3Int.zero 반환
        if (closestTilePosition == Vector3Int.zero)
        {
            Debug.Log("상호작용 거리 내에 제거 가능한 타일이 없습니다.");
        }

        return closestTilePosition;
    }

    private bool IsWoodTile(TileBase tile)
    {
        return System.Array.Exists(resourceTileSpawner.GetWoodTiles(), t => t == tile);
    }

    private bool IsStoneTile(TileBase tile)
    {
        return System.Array.Exists(resourceTileSpawner.GetStoneTiles(), t => t == tile);
    }

    // 공격 수행 함수
    public void Attack()
    {
        // 이미 공격 중이면 무시
        if (isAttacking)
            return;

        // 공격 상태로 설정
        isAttacking = true;

        // 애니메이션 재생 (최우선 처리)
        if (animator != null)
        {
            // 다른 애니메이션 즉시 중단하고 공격 애니메이션 재생
            animator.ResetTrigger("2_Attack"); // 기존 트리거 초기화
            animator.SetTrigger("2_Attack");

            // 공격 애니메이션 파라미터 우선순위 높이기 (선택사항)
            animator.SetLayerWeight(animator.GetLayerIndex("Base Layer"), 1);
        }
        GameManager.instance.PlaySFX("Attack");

        // 공격 데미지 처리
        StartCoroutine(ApplyAttackDamage());

        // 공격 쿨다운 시작
        StartCoroutine(AttackCooldown());
    }


    public void IgnoreCollisionsWithEnemiesAndNpcs()
    {
        Collider2D myCollider = GetComponent<Collider2D>();
        if (myCollider == null) return;

        // WallHealth 오브젝트의 모든 Collider2D와 무시
        WallHealth[] walls = FindObjectsOfType<WallHealth>();
        foreach (WallHealth w in walls)
        {
            Collider2D[] wallColliders = w.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D col in wallColliders)
            {
                if (col != null && myCollider != col)
                    Physics2D.IgnoreCollision(myCollider, col, true);
            }
        }

        // NPC와 충돌 무시 (모든 콜라이더)
        Npc[] npcs = FindObjectsOfType<Npc>();
        foreach (Npc npc in npcs)
        {
            Collider2D[] npcColliders = npc.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D col in npcColliders)
            {
                if (col != null && myCollider != col)
                    Physics2D.IgnoreCollision(myCollider, col, true);
            }
        }
    }

    // 공격 데미지 적용 코루틴
    private IEnumerator ApplyAttackDamage()
    {
        // 애니메이션 타이밍 맞추기 위한 지연
        yield return new WaitForSeconds(attackDelay);

        // 공격 범위 내 모든 콜라이더 감지
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(attackPoint.position, attackRange);

        // 감지된 콜라이더 중 적 태그를 가진 것 찾기
        bool hitEnemy = false;
        foreach (Collider2D hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag(enemyTag))
            {
                hitEnemy = true;
                Debug.Log("적 히트: " + hitCollider.name);

                // EnemyHealth 컴포넌트 확인
                EnemyHealth enemyHealth = hitCollider.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    // 데미지 적용
                    enemyHealth.TakeDamage(attackDamage, (Vector2)transform.position);
                    Debug.Log(hitCollider.name + "에게 " + attackDamage + " 데미지 적용");
                }
            }
        }

        if (!hitEnemy)
        {

        }
    }

    // 공격 쿨다운 코루틴
    private IEnumerator AttackCooldown()
    {
        // 애니메이션 길이 가져오기 (또는 고정된 쿨다운 시간 사용)
        float cooldownTime = attackCooldown;

        if (animator != null)
        {
            // 애니메이션 클립 정보 가져오기 시도
            AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
            if (clipInfo.Length > 0)
            {
                // 현재 애니메이션 길이에 기반한 쿨다운 계산
                cooldownTime = clipInfo[0].clip.length;

            }
        }

        // 쿨다운 시간 동안 대기
        yield return new WaitForSeconds(cooldownTime);

        // 공격 가능 상태로 변경
        isAttacking = false;

    }

    void FixedUpdate()
    {
        // 이동 입력 처리
        Move();
    }

    // 이동 처리 함수
    private void Move()
    {
        // 이동 적용 (좌우 이동만 가능하고, y 속도는 그대로 유지)
        rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);
    }

    // 애니메이션 파라미터 업데이트
    private void UpdateAnimationParameters()
    {
        if (animator != null)
        {
            // 움직임 감지 (절대값이 0.1보다 크면 움직이는 것으로 간주)
            bool isMoving = Mathf.Abs(horizontalInput) > 0.1f;

            // 움직임 파라미터 설정
            animator.SetBool("1_Move", isMoving);
        }
    }

    // 점프 함수
    private void Jump()
    {
        // 현재 y속도는 0으로 설정하고 jumpForce만큼 위로 힘 가함
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

    }

    // 마우스 위치에 따른 플레이어 방향 전환
    private void FlipBasedOnMousePosition()
    {
        // 마우스 위치가 유효한지 확인 (화면 내부에 있는지)
        if (Input.mousePosition.x >= 0 && Input.mousePosition.x <= Screen.width &&
            Input.mousePosition.y >= 0 && Input.mousePosition.y <= Screen.height)
        {
            // 마우스 위치를 월드 좌표로 변환
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0; // z값을 플레이어와 동일하게 설정

            // 플레이어 기준 마우스가 오른쪽에 있는지 확인
            bool mouseOnRight = mousePos.x > transform.position.x;

            // 마우스가 오른쪽에 있을 때 플레이어도 오른쪽을 바라보도록, 왼쪽일 때는 왼쪽을 바라보도록 처리
            if ((mouseOnRight && !isFacingRight) || (!mouseOnRight && isFacingRight))
            {
                // 플레이어 방향 전환
                Flip();
            }
        }
    }

    // 플레이어 방향 전환 함수
    private void Flip()
    {
        // 현재 방향 반전
        isFacingRight = !isFacingRight;

        // 스케일의 x값 반전으로 스프라이트 뒤집기
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;

        // 공격 지점 위치 조정 (플레이어가 바라보는 방향으로)
        if (attackPoint != null)
        {
            Vector3 attackPos = attackPoint.localPosition;
            attackPos.x = Mathf.Abs(attackPos.x) * (isFacingRight ? 1 : -1);
            attackPoint.localPosition = attackPos;
        }
    }

    // 카메라 이동 처리 (LateUpdate에서 처리하여 플레이어 이동 후 카메라 이동)
    void LateUpdate()
    {
        // 메인 카메라가 있을 경우에만 실행
        if (mainCamera != null)
        {
            // 현재 카메라 위치
            Vector3 cameraPos = mainCamera.transform.position;

            // 카메라의 x 좌표만 플레이어를 따라가도록 업데이트 (y, z 값은 유지)
            mainCamera.transform.position = new Vector3(transform.position.x, cameraPos.y, cameraPos.z);
        }

       // IgnoreCollisionsWithEnemiesAndNpcs();
    }

    // 디버깅용 그리기 함수
    private void OnDrawGizmos()
    {
        // if (groundCheck != null)
        // {
        //     // 지면 체크 영역 시각화
        //     Gizmos.color = Color.red;
        //     Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        // }

        // 상호작용 거리 시각화
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);

        // 공격 범위 시각화
        if (attackPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }


}
