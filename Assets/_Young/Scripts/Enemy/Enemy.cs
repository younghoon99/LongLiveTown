using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [Header("공격 설정")]
    public float attackDamage = 10f;
    private float attackRange = 2.0f;
    public float attackCooldown = 2f;        // 공격 쿨타임
    public float attackDelay = 0.3f;         // 공격 애니메이션 후 데미지 적용까지 딜레이
    private float nextAttackTime = 0f;

    [Header("탐지 설정")]
    public float detectionRange = 5f;
    private string[] targetTags = { "Home", "Player", "NPC", "Wall" };  // 우선순위 높은 순서
    private Transform currentTarget;  // 현재 타겟

    [Header("이동 설정")]
    public float moveSpeed = 2f;
    private float stoppingDistance = 1.5f;  // 멈추는 거리
    public bool canMove = true;

    [Header("방향 설정")]
    public bool facingRight = true;

   

    // 캐싱할 컴포넌트
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        // 필수 컴포넌트 캐싱
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        // Rigidbody2D가 없으면 추가
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        // Rigidbody2D 설정
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.bodyType = RigidbodyType2D.Dynamic;

        // 다른 적, NPC와 충돌 무시
        IgnoreCollisionsWithEnemiesAndNpcs();

        // 타겟 탐색 시작
        FindTarget();
    }
    private void OnEnable()
    {
        IgnoreCollisionsWithEnemiesAndNpcs();
    }

    void Update()
    {
        // 주기적으로 타겟 재탐색 (성능 최적화 위해 30프레임마다)
        if (Time.frameCount % 30 == 0)
        {
            FindTarget();
           

        }

        if (currentTarget == null) return;

        float distance = Vector2.Distance(transform.position, currentTarget.position);

        if (distance <= detectionRange)
        {
            LookAtTarget();

            if (distance <= attackRange && Time.time >= nextAttackTime)
            {
                Attack();
                nextAttackTime = Time.time + attackCooldown;
            }
            else if (distance > stoppingDistance && canMove)
            {
                MoveTowardsTarget();
                SetAnimMove(true);
            }
            else
            {
                StopMoving();
                SetAnimMove(false);
            }
        }
        else
        {
            StopMoving();
            SetAnimMove(false);
        }
    }

    // 지정된 타겟을 향해 이동
    // 타겟을 향해 x축으로만 이동
    private void MoveTowardsTarget()
    {
        if (rb == null || currentTarget == null) return;
        // x축 방향만 계산, y축은 0으로 고정
        float xDir = currentTarget.position.x - transform.position.x;
        Vector2 direction = new Vector2(xDir, 0).normalized;
        rb.velocity = direction * moveSpeed;
    }

    // 이동 멈춤
    private void StopMoving()
    {
        if (rb != null)
            rb.velocity = Vector2.zero;

        // 걷는 애니메이션 끄기
        if (animator != null)
            animator.SetBool("1_Move", false);
    }

    // 타겟을 바라보는 방향으로 회전
    private void LookAtTarget()
    {
        if (currentTarget == null) return;

        bool targetIsRight = currentTarget.position.x > transform.position.x;

        if (targetIsRight != facingRight)
        {
            facingRight = targetIsRight;
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
            // EnemyHealth UI Flip 호출
            EnemyHealth health = GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.FlipUI(scale.x < 0);
            }
        }
    }

    // 애니메이션 상태 설정
    private void SetAnimMove(bool isMoving)
    {
        if (animator != null)
        {
            animator.SetBool("1_Move", isMoving);
        }
    }

    // 공격 수행
    // 공격 수행
    private void Attack()
    {
        // 모든 공격 시 정지
        StopMoving();

        // 공격 애니메이션 재생
        if (animator != null)
        {
            animator.SetBool("1_Move", false);  // 걷는 애니메이션 끄기
            animator.SetTrigger("2_Attack");
        }
        GameManager.instance.PlaySFX("EnemyAttack");

        // 데미지 적용 코루틴 시작
        StartCoroutine(ApplyAttackDamage());
    }


    // 공격 데미지 적용 코루틴
    private IEnumerator ApplyAttackDamage()
    {
        yield return new WaitForSeconds(attackDelay);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange);
        foreach (string tag in targetTags)
        {
            foreach (Collider2D col in hits)
            {
                if (col.CompareTag(tag))
                {
                    ApplyDamage(col);
                    yield break;
                }
            }
        }

        Debug.Log($"{gameObject.name}의 공격이 빗나감");

        // 공격 후 다시 이동 시작
        if (currentTarget != null)
        {
            MoveTowardsTarget();
            if (animator != null)
                animator.SetBool("1_Move", true);
        }
    }

    // 타겟에 데미지 적용
    private void ApplyDamage(Collider2D target)
    {
        string tag = target.tag;
        string targetName = target.name;

        Debug.Log($"{gameObject.name}이(가) {tag}({targetName})을(를) 타격");

        // 대상에 따라 데미지 처리
        if (tag == "Player")
        {
            PlayerHealth player = target.GetComponent<PlayerHealth>();
            if (player != null) player.TakeDamage(attackDamage);
        }
        else if (tag == "NPC")
        {
            NpcHealth npc = target.GetComponent<NpcHealth>();
            if (npc != null) npc.TakeDamage(attackDamage);
        }
        else if (tag == "Wall")
        {
            WallHealth wall = target.GetComponent<WallHealth>();
            if (wall != null) wall.TakeDamage(attackDamage);
        }
        else if (tag == "Home")
        {
            HQHealth home = target.GetComponent<HQHealth>();
            if (home != null) home.TakeDamage(attackDamage);
        }
    }

    // 모든 타겟을 체크하고 가장 가까운 타겟을 선택
    private void FindTarget()
    {
        currentTarget = null;
        float closestDistance = Mathf.Infinity;
        Transform bestTarget = null;
        string bestTag = string.Empty;

        // 모든 타겟 태그에 대해 체크
        foreach (string tag in targetTags)
        {
            GameObject[] candidates = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject candidate in candidates)
            {
                float distance = Vector2.Distance(transform.position, candidate.transform.position);
                if (distance < detectionRange && distance < closestDistance)
                {
                    closestDistance = distance;
                    bestTarget = candidate.transform;
                    bestTag = tag;
                }
            }
        }

        // 가장 가까운 타겟이 있다면 설정
        if (bestTarget != null)
        {
            currentTarget = bestTarget;
           
        }
    }

    // Enemy, NPC와 충돌 무시
    private void IgnoreCollisionsWithEnemiesAndNpcs()
    {
        Collider2D myCollider = GetComponent<Collider2D>();
        if (myCollider == null) return;

        // 다른 Enemy와 충돌 무시
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        foreach (Enemy e in enemies)
        {
            if (e != this)
            {
                Collider2D col = e.GetComponent<Collider2D>();
                if (col != null)
                    Physics2D.IgnoreCollision(myCollider, col, true);
            }
        }

        // NPC와 충돌 무시
        Npc[] npcs = FindObjectsOfType<Npc>();
        foreach (Npc npc in npcs)
        {
            Collider2D col = npc.GetComponent<Collider2D>();
            if (col != null)
                Physics2D.IgnoreCollision(myCollider, col, true);
        }
    }

    // 디버그용 공격 범위 표시
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
