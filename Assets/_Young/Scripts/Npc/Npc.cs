using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI; // UI 관련 기능 사용
using TMPro;
using RedstoneinventeGameStudio;

public class Npc : MonoBehaviour
{
    [Header("NPC 데이터")]
    [SerializeField] private NpcData npcData;  // NPC 데이터 참조
    [SerializeField] private string npcId;     // NPC ID
    private NpcData.NpcEntry npcEntry = null;         // 현재 NPC의 데이터 항목

    [SerializeField] public Inventory inventory;

    // ResourceTileSpawner 참조 추가
    private ResourceTileSpawner resourceTileSpawner;

    // 외부에서 NPC 데이터 접근용 프로퍼티
    public NpcData.NpcEntry NpcEntry => npcEntry;
    public NpcHealth npcHealth;

    // 접근자 속성
    public string NpcName => npcEntry != null ? npcEntry.npcName : gameObject.name;

    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 1.0f;         // 이동 속도
    [SerializeField] private float idleTimeMin = 2.0f;       // 최소 정지 시간
    [SerializeField] private float idleTimeMax = 5.0f;       // 최대 정지 시간
    [SerializeField] private float moveTimeMin = 1.0f;       // 최소 이동 시간
    [SerializeField] private float moveTimeMax = 3.0f;       // 최대 이동 시간
    private float movementRange = 30.0f;     // 초기 위치로부터 최대 이동 가능 거리

    [Header("방향 설정")]
    [SerializeField] private bool facingleft = true;        // NPC의 초기 방향 (true: 왼쪽, false: 오른쪽)



    [Header("장착 아이템")]
    [SerializeField] private GameObject sword;
    [SerializeField] public GameObject bow;
    [SerializeField] private GameObject axe;
    [SerializeField] private GameObject pickaxe;

    [Header("컴포넌트 참조")]
    [SerializeField] public Animator animator;              // 애니메이터 참조

    // 내부 상태 변수
    private Vector3 initialPosition;                         // 초기 위치 저장
    private Vector2 moveDirection = Vector2.zero;            // 현재 이동 방향
    private float moveTimer = 0f;                            // 이동 타이머
    private float idleTimer = 0f;                            // 정지 타이머
    private bool isMoving = false;                           // 이동 중인지 여부
    private bool canMove = true;                             // 움직임 가능 여부 (상호작용 중에는 false)
    private bool randomMovementActive = true;                // 랜덤 움직임 활성화 여부
    private Rigidbody2D rb;                                  // Rigidbody2D 참조
    private SpriteRenderer spriteRenderer;                   // SpriteRenderer 참조

    // NPC 능력치 변수
    private int attackPower;
    private int miningPower;
    private int moveSpeedStat;



    // NPC 상태
    public enum NpcState { Idle, Moving, Interacting, Escaping }
    private NpcState currentState = NpcState.Idle;

    // NPC 작업 유형
    public enum NpcTask
    {
        None,
        Woodcutting, // 나무 채집
        Mining,      // 광물 채집
        Combat,       // 전투
        BowCombat // 활사용 전투
    }

    // 현재 작업
    private NpcTask currentTask = NpcTask.None;

    // 마지막 채집 작업 기억 (도망 후 복귀용)
    private NpcTask lastGatherTask = NpcTask.None;

    // 작업 관련 변수
    private Transform targetObject = null;
    private bool isReturningToBase = false;
    private Vector3 basePosition;
    private float resourceGatheringTimer = 0f;
    private int gatheredResources = 0;

    // 기본 채집 시간 상수
    private const float BASE_WOODCUTTING_TIME = 5f; // 기본 나무 채집 시간
    private const float BASE_MINING_TIME = 5f; // 기본 채광 시간

    // 채집 시간 계산 메서드
    private float CalculateGatheringTime(float baseTime, int miningPower)
    {
        // 채굴력이 1일 때 기본 시간 (5초)
        // 채굴력이 증가할수록 시간이 감소 (예: 채굴력 2 -> 4초, 채굴력 3 -> 3.33초)
        return baseTime / (1 + (miningPower - 1) * 0.25f);
    }

    void Awake()
    {
        if (inventory == null)
        {
            inventory = transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).GetComponentInChildren<Inventory>();
            if (inventory == null)
            {
                Debug.LogError("Inventory component not found!");
            }
        }
        // NPC 슬롯이니까 isPlayerInventory는 false로!
        if (inventory != null)
            inventory.isPlayerInventory = false;
        npcHealth = GetComponent<NpcHealth>();
    }







    // 시작 시 호출됨
    private void Start()
    {
        npcEntry = null; //테스트용 npcEntry 


        // 컴포넌트 초기화
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();


        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // 초기 위치 저장
        initialPosition = transform.position;

        // ResourceTileSpawner 찾기
        if (resourceTileSpawner == null)
        {
            resourceTileSpawner = FindObjectOfType<ResourceTileSpawner>();
            if (resourceTileSpawner == null)
            {
                Debug.LogWarning($"{NpcName}: Start에서 ResourceTileSpawner를 찾을 수 없습니다. 나무/광물 채집 작업이 제대로 동작하지 않을 수 있습니다.");
            }
            else
            {
                Debug.Log($"{NpcName}: ResourceTileSpawner를 성공적으로 찾았습니다.");
            }
        }

        // 다른 에너미 오브젝트와의 충돌 무시 설정
        IgnoreCollisionsWithEnemies();

        // NPC 생성 시 무조건 랜덤 등급으로 생성
        if (npcData != null)
        {
            // 등급별 확률 계산 (노말 60%, 레어 25%, 영웅 10%, 전설 5%)
            float rarityRoll = Random.Range(0f, 1f);
            NpcData.NpcRarity rarity;

            if (rarityRoll < 0.05f)
                rarity = NpcData.NpcRarity.전설;
            else if (rarityRoll < 0.15f)
                rarity = NpcData.NpcRarity.영웅;
            else if (rarityRoll < 0.40f)
                rarity = NpcData.NpcRarity.레어;
            else
                rarity = NpcData.NpcRarity.노말;

            npcEntry = npcData.GenerateRandomNpc(rarity);
            npcId = npcEntry.npcId;
            // 데이터 초기화
            InitializeFromData();
        }
        else
        {
            Debug.LogError("NPC 데이터가 없습니다. NPC 데이터를 할당해 주세요.");
        }

        // 초기 상태 설정 (자동으로 움직이기 시작)
        DecideNextAction();
    }

    // 매 프레임 업데이트
    private void FixedUpdate()
    {
        // 상호작용 중이거나 움직임이 비활성화된 경우 움직이지 않음
        if (!canMove || currentState == NpcState.Interacting)
        {
            // 움직이지 않을 때는 속도를 0으로 설정
            if (rb != null) rb.velocity = Vector2.zero;

            // 움직임 애니메이션 비활성화
            if (animator != null) animator.SetBool("1_Move", false);
            return;
        }

        // 초기 위치로 돌아가는 중인 경우 우선 처리
        if (returningToInitialPosition)
        {
            // 초기 위치와의 거리 계산
            float distanceFromStart = Vector3.Distance(transform.position, initialPosition);

            // 초기 위치에 근접했는지 확인
            if (distanceFromStart <= movementRange * 0.3f) // 조금 더 작은 범위로 설정
            {
                // 초기 위치에 도착했으니 랜덤 이동 모드로 전환
                returningToInitialPosition = false;
                randomMovementActive = true;
                rb.velocity = Vector2.zero;
                if (animator != null) animator.SetBool("1_Move", false);
                currentState = NpcState.Idle;
                isMoving = false;
                Debug.Log($"{NpcName}이(가) 초기 위치에 도착하여 랜덤 이동 모드로 전환합니다.");
                DecideNextAction();

                // 애니메이션 업데이트
                UpdateAnimation();
                return;
            }
            else
            {
                // 계속 초기 위치로 이동
                Vector3 direction = (initialPosition - transform.position).normalized;
                rb.velocity = direction * moveSpeed;
                UpdateDirection(direction);
                if (animator != null) animator.SetBool("1_Move", true);

                // 애니메이션 업데이트
                UpdateAnimation();
                return;
            }
        }
        

        // ----------- 도망 로직 시작 -----------
        GameObject nearestEnemy = FindNearestObjectWithTag("Enemy");
        float dangerDistance = 4f;
        if ((currentTask == NpcTask.Woodcutting || currentTask == NpcTask.Mining 
        || currentTask == NpcTask.None) && nearestEnemy != null)
        {
            float distanceToEnemy = Vector3.Distance(transform.position, nearestEnemy.transform.position);
            Vector3 direction = (nearestEnemy.transform.position - transform.position).normalized;
            if (distanceToEnemy < dangerDistance)
            {
                Debug.Log($"{NpcName}이(가) 적이 가까워 도망치는 중");
                // 적이 가까우면 카이팅 도망 로직 그대로 적용
                rb.velocity = -direction * moveSpeed * 1.5f;
                if (animator != null) animator.SetBool("1_Move", true);
                UpdateDirection(-direction);
                // 자원 예약 취소
                CancelResourceReservation();
                Debug.Log($"{NpcName}이(가) 도망치기 lastGatherTask({lastGatherTask}가 직업 ");
                return;
            }
        }
      


        // 작업이 있는 경우 랜덤 이동을 하지 않음
        if (currentTask != NpcTask.None)
        {
            HandleTask();
            return;
        }

        // 현재 상태에 따른 처리
        switch (currentState)
        {
            case NpcState.Idle:
                HandleIdleState();
                break;

            case NpcState.Moving:
                HandleMovingState();
                break;
        }

        // 애니메이션 업데이트
        UpdateAnimation();
    }
    // Npc 클래스 내부, 다른 메서드들과 동일한 레벨에 위치해야 함
    private void CancelResourceReservation()
    {

        // 자원 위치가 초기화되어 있는지 확인 (작업 중단 후 재시작 시)
        if (currentResourcePosition == Vector3.zero && isMining)
        {
            Debug.Log($"{NpcName}: 채광 작업이 중단되었습니다. 자원 위치가 초기화되어 있습니다.");
            isMining = false; // 작업 상태 초기화
            resourceTimer = 0f;
            resourceTileSpawner.ReleaseStoneTile(currentResourcePosition);
            currentResourcePosition = Vector3.zero;
        }
        if (animator != null)
            animator.SetTrigger("CancelMining");
    }

    // 등급에 따른 색상 이름 반환
    private string GetColoredRarityName()
    {
        if (npcEntry == null) return gameObject.name;

        string colorCode;

        switch (npcEntry.rarity)
        {
            case NpcData.NpcRarity.노말:
                colorCode = "white";
                break;
            case NpcData.NpcRarity.레어:
                colorCode = "blue";
                break;
            case NpcData.NpcRarity.영웅:
                colorCode = "purple";
                break;
            case NpcData.NpcRarity.전설:
                colorCode = "orange";
                break;
            default:
                colorCode = "white";
                break;
        }

        // ID 대신 이름만 표시
        return $"<color={colorCode}>{npcEntry.npcName}</color>";
    }







    // NPC 데이터로부터 초기화
    public void InitializeFromData()
    {
        if (npcEntry == null)
        {
            Debug.LogError("NPC 데이터 항목이 null입니다.");
            return;
        }

        InitializeStats();
    }

    // NPC 데이터 항목을 받아서 초기화하는 오버로드
    public void InitializeFromData(NpcData.NpcEntry newNpcEntry)
    {
        // 받은 데이터 항목 설정
        npcEntry = newNpcEntry;

        if (npcEntry == null)
        {
            Debug.LogError("NPC 데이터 항목이 null입니다.");
            return;
        }

        InitializeStats();
    }

    // 스탯 초기화 공통 메서드
    private void InitializeStats()
    {
        // 기본 스탯 설정 (체력 관련 설정은 NpcHealth에서 처리하도록 수정)
        attackPower = npcEntry.attack;
        miningPower = npcEntry.miningPower;
        moveSpeedStat = npcEntry.moveSpeed;

        // 능력치가 0이하인 경우 최소값으로 설정 (데이터 누락 방지)
        if (attackPower <= 0) attackPower = 1;
        if (miningPower <= 0) miningPower = 1;
        if (moveSpeedStat <= 0) moveSpeedStat = 1;


        // 이동 속도 설정
        moveSpeed = 1f + (moveSpeedStat * 0.1f); // 이동 속도는 기본 0.5 + 스탯의 10%
        idleTimeMin = npcEntry.idleTimeMin;
        idleTimeMax = npcEntry.idleTimeMax;
        moveTimeMin = npcEntry.moveTimeMin;
        moveTimeMax = npcEntry.moveTimeMax;

        // 디버그 로그
        Debug.Log($"{npcEntry.npcName} NPC 초기화 완료: 등급-{npcEntry.rarity}, 공격력-{attackPower}, 채굴력-{miningPower}, 이동속도-{moveSpeedStat}");
    }

    // Enemy 오브젝트들과의 충돌 무시 설정

    // Enemy 오브젝트들과의 충돌 무시 설정
    public void IgnoreCollisionsWithEnemies()
    {
        Collider2D npcCollider = GetComponent<Collider2D>();

        if (npcCollider != null)
        {
            // 모든 Enemy와의 충돌 무시
            Enemy[] enemies = FindObjectsOfType<Enemy>();
            foreach (Enemy enemy in enemies)
            {
                Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
                if (enemyCollider != null)
                {
                    Physics2D.IgnoreCollision(npcCollider, enemyCollider, true);
                }
            }

            // 플레이어와의 충돌 무시
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Collider2D playerCollider = player.GetComponent<Collider2D>();
                if (playerCollider != null)
                {
                    Physics2D.IgnoreCollision(npcCollider, playerCollider, true);
                }
            }

            // 다른 NPC와의 충돌 무시
            Npc[] npcs = FindObjectsOfType<Npc>();
            foreach (Npc otherNpc in npcs)
            {
                // 자기 자신은 제외
                if (otherNpc != this)
                {
                    Collider2D otherNpcCollider = otherNpc.GetComponent<Collider2D>();
                    if (otherNpcCollider != null)
                    {
                        Physics2D.IgnoreCollision(npcCollider, otherNpcCollider, true);
                    }
                }
            }
            // WallHealth 오브젝트의 모든 Collider2D와 무시
            WallHealth[] walls = FindObjectsOfType<WallHealth>();
            foreach (WallHealth w in walls)
            {
                Collider2D[] wallColliders = w.GetComponentsInChildren<Collider2D>();
                foreach (Collider2D col in wallColliders)
                {
                    if (col != null && npcCollider != col)
                        Physics2D.IgnoreCollision(npcCollider, col, true);
                }
            }
        }
    }

    // 정지 상태 처리
    private void HandleIdleState()
    {
        // 정지 타이머 증가
        idleTimer += Time.deltaTime;

        // 정지 시간이 지나면 이동 상태로 전환
        if (idleTimer >= Random.Range(idleTimeMin, idleTimeMax))
        {
            // 이동 방향 결정 (왼쪽 또는 오른쪽)
            float directionX = Random.Range(0, 2) == 0 ? -1 : 1;
            moveDirection = new Vector2(directionX, 0);

            // 방향에 따라 facingleft 업데이트
            if (directionX > 0) facingleft = false;
            else facingleft = true;

            // 타이머 초기화 및 상태 변경
            idleTimer = 0f;
            moveTimer = 0f;
            currentState = NpcState.Moving;
            isMoving = true;


        }
    }

    // 이동 상태 처리
    private void HandleMovingState()
    {
        // 이동 타이머 증가
        moveTimer += Time.deltaTime;

        // 이동 범위 체크
        Vector3 potentialPosition = transform.position + (Vector3)moveDirection * moveSpeed * Time.deltaTime;
        float distanceFromStart = Vector3.Distance(initialPosition, potentialPosition);

        // 이동 범위를 벗어나지 않는 경우에만 이동
        if (distanceFromStart <= movementRange)
        {
            // 물리 기반 이동
            rb.velocity = moveDirection * moveSpeed;
        }
        else
        {
            // 반대 방향으로 전환
            moveDirection = -moveDirection;
            Debug.Log($"NPC {NpcName}이(가) 이동 범위 한계에 도달하여 방향을 바꿨습니다");

            // 방향을 바꾸고 즉시 이동하도록 설정
            rb.velocity = moveDirection * moveSpeed;

            // 방향 업데이트
            UpdateDirection(moveDirection);
        }

        // 이동 시간이 지나면 정지 상태로 전환
        if (moveTimer >= Random.Range(moveTimeMin, moveTimeMax))
        {
            // 속도 초기화 및 상태 변경
            rb.velocity = Vector2.zero;
            moveTimer = 0f;
            idleTimer = 0f;
            currentState = NpcState.Idle;
            isMoving = false;

            Debug.Log($"NPC {NpcName}이(가) 이동을 멈추고 대기 상태로 전환");
        }
    }

    // 애니메이션 업데이트
    private void UpdateAnimation()
    {
        if (animator != null)
        {
            // 이동 애니메이션 파라미터 업데이트
            animator.SetBool("1_Move", isMoving);

            // 스프라이트 방향 업데이트 (localScale 사용)
            Vector3 newScale = transform.localScale;
            // facingleft가 true이면 양수 스케일, false이면 음수 스케일
            newScale.x = Mathf.Abs(newScale.x) * (facingleft ? 1 : -1);
            transform.localScale = newScale;
        }
    }

    // 다음 행동 결정
    private void DecideNextAction()
    {
        // 랜덤하게 첫 상태 결정
        if (Random.Range(0, 2) == 0)
        {
            currentState = NpcState.Idle;
            idleTimer = 0f;
        }
        else
        {
            currentState = NpcState.Moving;
            moveTimer = 0f;
            float directionX = Random.Range(0, 2) == 0 ? -1 : 1;
            moveDirection = new Vector2(directionX, 0);
            isMoving = true;

            // 방향에 따라 facingleft 업데이트
            if (directionX > 0) facingleft = false;
            else facingleft = true;
        }
    }

    // 상호작용 시작 (NpcInteraction에서 호출)
    public void OnInteractionStart()
    {
        // 상호작용 시작 시 이동 중지
        canMove = false;
        rb.velocity = Vector2.zero;
        currentState = NpcState.Interacting;
        isMoving = false;

        // 애니메이션 업데이트 (이동 중지)
        if (animator != null)
        {
            animator.SetBool("1_Move", false);
        }

        Debug.Log($"NPC {NpcName}이(가) 플레이어와의 상호작용을 시작했습니다");
    }

    // 상호작용 종료 (NpcInteraction에서 호출)
    public void OnInteractionEnd()
    {
        // 상호작용 종료 시 이동 가능 상태로 복귀
        canMove = true;
        currentState = NpcState.Idle;
        idleTimer = 0f;

        Debug.Log($"NPC {NpcName}이(가) 플레이어와의 상호작용을 종료했습니다");
    }

    // NPC 정보 반환 (상호작용 UI용)
    public string GetNpcInfoText()
    {
        if (npcEntry == null) return "NPC 정보가 없습니다.";

        string coloredName = GetColoredRarityName();
        string statInfo = $"<b>공격력:</b> {attackPower}\n<b>체력:</b> {GetCurrentHealth()}/{GetMaxHealth()}\n<b>채굴능력:</b> {miningPower}\n<b>이동속도:</b> {moveSpeedStat}";

        return $"{coloredName}\n\n<b>[등급 {npcEntry.rarity}]</b>\n\n{statInfo}\n\n{npcEntry.description}";
    }

    // 능력치 값들 반환 메서드
    public int GetAttackPower() => attackPower;
    public int GetMaxHealth() => npcHealth != null ? (int)npcHealth.MaxHealth : 0;
    public int GetCurrentHealth() => npcHealth != null ? (int)npcHealth.CurrentHealth : 0;
    public int GetMiningPower() => miningPower;
    public int GetMoveSpeedStat() => moveSpeedStat;
    public NpcData.NpcRarity GetRarity() => npcEntry != null ? npcEntry.rarity : NpcData.NpcRarity.노말;

    // 작업 처리
    private void HandleTask()
    {
        switch (currentTask)
        {
            case NpcTask.Woodcutting:
                HandleWoodcuttingTask();
                axe.SetActive(true);
                break;
            case NpcTask.Mining:
                HandleMiningTask();
                pickaxe.SetActive(true);
                break;
            case NpcTask.Combat:
                HandleCombatTask();
                sword.SetActive(true);
                break;
            case NpcTask.BowCombat:
                HandleBowCombatTask();
                bow.SetActive(true);
                break;
        }
    }

    // 자원 채굴 관련 변수
    public bool isWoodcutting = false;
    public bool isMining = false;
    private float resourceTimer = 0f;
    private Vector3 currentResourcePosition;
    private float resourceGatheringDuration = 3f; // 채굴 시간


    private void HandleWoodcuttingTask()
    {
        ResetAllWorkAnimation();
        axe.SetActive(true);
        pickaxe.SetActive(false);
        bow.SetActive(false);
        sword.SetActive(false);
        // ResourceTileSpawner가 없는 경우 처리
        if (resourceTileSpawner == null)
        {
            resourceTileSpawner = FindObjectOfType<ResourceTileSpawner>();
            if (resourceTileSpawner == null)
            {
                Debug.LogWarning($"{NpcName}: ResourceTileSpawner를 찾을 수 없습니다.");
                DecideNextAction();
                return;
            }
        }

        // 자원 위치가 초기화되어 있는지 확인 (작업 중단 후 재시작 시)
        if (currentResourcePosition == Vector3.zero && isWoodcutting)
        {
            Debug.Log($"{NpcName}: 나무 작업이 중단되었습니다. 자원 위치가 초기화되어 있습니다.");
            isWoodcutting = false; // 작업 상태 초기화
            resourceTimer = 0f;
        }

        // 이미 채굴 중이면 채굴 진행
        if (isWoodcutting)
        {
            resourceTimer += Time.deltaTime;

            // 채굴 완료
            if (resourceTimer >= resourceGatheringDuration)
            {
                // 채굴 완료 후 애니메이션 상태 초기화
                if (animator != null)
                {
                    animator.SetTrigger("CancelMining");
                }

                // 타일 제거
                Tilemap tilemap = resourceTileSpawner.GetComponent<Tilemap>();
                if (tilemap != null)
                {
                    Vector3Int cellPosition = tilemap.WorldToCell(currentResourcePosition);
                    resourceTileSpawner.RemoveTile(cellPosition);
                    Debug.Log($"{NpcName}이(가) 나무 채굴을 완료했습니다.");
                }

                // 채굴 상태 초기화
                isWoodcutting = false;
                resourceTimer = 0f;

                // 타겟 해제
                resourceTileSpawner.ReleaseWoodTile(currentResourcePosition);
                currentResourcePosition = Vector3.zero;
                return;
            }
            return;
        }

        // 모든 적 위치 리스트 생성
        List<Vector3> enemyPositions = new List<Vector3>();
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var enemy in enemies)
        {
            enemyPositions.Add(enemy.transform.position);
        }
        float enemyDangerRadius = 3.0f; // 적 근처로 간주할 반경(필요시 조정)

        // 적이 가까이 없는 안전한 나무 타일만 찾기
        Vector3 nearestWoodPosition = resourceTileSpawner.GetNearestSafeWoodTilePosition(transform.position, enemyPositions, enemyDangerRadius);
        Debug.Log($"{NpcName}이(가) (적 근처 제외) 가장 가까운 나무를 찾았습니다: {nearestWoodPosition}");

        // 나무가 없는 경우
        if (nearestWoodPosition == Vector3.zero)
        {
            Debug.Log($"{NpcName}: 근처에 나무가 없습니다.");
            rb.velocity = Vector2.zero;
            if (animator != null) animator.SetBool("1_Move", false);
            return;
        }

        // 나무로 이동
        float distanceToWood = Vector3.Distance(transform.position, nearestWoodPosition);

        if (distanceToWood > 0.8f) // 더 가까이 접근하도록 거리 조정
        {
            // 나무로 이동
            Vector3 direction = (nearestWoodPosition - transform.position).normalized;
            rb.velocity = direction * moveSpeed;

            // 애니메이션 업데이트 - 이동 시 반드시 1_Move 애니메이션만 활성화
            if (animator != null)
            {
                animator.SetTrigger("CancelMining");
                animator.SetBool("1_Move", true);
            }

            // 방향 설정
            UpdateDirection(direction);
            Debug.Log($"{NpcName}이(가) 나무를 향해 이동 중입니다.");
        }
        else
        {
            // 나무 근처에 도착하면 채굴 시작
            rb.velocity = Vector2.zero;

            // 애니메이션 업데이트 (이동 중지, 채굴 시작)
            if (animator != null)
            {
                animator.SetBool("1_Move", false);
                animator.SetTrigger("6_Other");

            }

            // 현재 자원 위치 저장
            currentResourcePosition = nearestWoodPosition;
            // 자원 타일 예약
            resourceTileSpawner.ReserveWoodTile(nearestWoodPosition);
            Debug.Log($"{NpcName}이(가) 현재 나무 위치를 저장했습니다: {currentResourcePosition}");

            // 채굴 상태 설정
            isWoodcutting = true;
            resourceTimer = 0f;

            Debug.Log($"{NpcName}이(가) 나무 채굴을 시작합니다.");
        }
    }


    // 광물 채집 처리
    private void HandleMiningTask()
    {
        ResetAllWorkAnimation();
        axe.SetActive(false);
        pickaxe.SetActive(true);
        bow.SetActive(false);
        sword.SetActive(false);
        // ResourceTileSpawner가 없는 경우 처리
        if (resourceTileSpawner == null)
        {
            resourceTileSpawner = FindObjectOfType<ResourceTileSpawner>();
            if (resourceTileSpawner == null)
            {
                Debug.LogWarning($"{NpcName}: ResourceTileSpawner를 찾을 수 없습니다.");
                rb.velocity = Vector2.zero;
                if (animator != null) animator.SetBool("1_Move", false);
                return;
            }
        }

        // 자원 위치가 초기화되어 있는지 확인 (작업 중단 후 재시작 시)
        if (currentResourcePosition == Vector3.zero && isMining)
        {
            Debug.Log($"{NpcName}: 채광 작업이 중단되었습니다. 자원 위치가 초기화되어 있습니다.");
            isMining = false; // 작업 상태 초기화
            resourceTimer = 0f;
        }

        // 이미 채굴 중이면 채굴 진행
        if (isMining)
        {
            // 채집 시간 계산
            float gatheringTime = CalculateGatheringTime(BASE_MINING_TIME, npcEntry.miningPower);
            resourceTimer -= Time.deltaTime;

            // 채굴 완료
            if (resourceTimer <= 0)
            {

                // 채굴 완료 후 애니메이션 상태 초기화
                if (animator != null)
                {
                    Debug.Log($"{NpcName}이(가) 돌 채굴을 완료했습니다.");
                    animator.SetTrigger("CancelMining");
                }


                // 타일 제거
                Tilemap tilemap = resourceTileSpawner.GetComponent<Tilemap>();
                if (tilemap != null)
                {
                    Vector3Int cellPosition = tilemap.WorldToCell(currentResourcePosition);
                    resourceTileSpawner.RemoveTile(cellPosition);
                }

                // 채굴 상태 초기화
                isMining = false;
                resourceTimer = 0f;
                // 타겟 해제
                resourceTileSpawner.ReleaseStoneTile(currentResourcePosition);
                currentResourcePosition = Vector3.zero;
                return;
            }
            return;
        }

        // 모든 적 위치 리스트 생성
        List<Vector3> enemyPositions = new List<Vector3>();
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var enemy in enemies)
        {
            enemyPositions.Add(enemy.transform.position);
        }
        float enemyDangerRadius = 3.0f; // 적 근처로 간주할 반경(필요시 조정)

        // 적이 가까이 없는 안전한 돌 타일만 찾기
        Vector3 nearestStonePosition = resourceTileSpawner.GetNearestSafeStoneTilePosition(transform.position, enemyPositions, enemyDangerRadius);
        Debug.Log($"{NpcName}이(가) (적 근처 제외) 가장 가까운 돌을 찾았습니다: {nearestStonePosition}");

        // 돌이 없는 경우
        if (nearestStonePosition == Vector3.zero)
        {
            Debug.Log($"{NpcName}: 근처에 돌이 없습니다.");
            rb.velocity = Vector2.zero;
            if (animator != null) animator.SetBool("1_Move", false);
            return;
        }

        // 돌로 이동
        float distanceToStone = Vector3.Distance(transform.position, nearestStonePosition);

        if (distanceToStone > 0.8f) // 더 가까이 접근하도록 거리 조정
        {
            // 돌로 이동
            Vector3 direction = (nearestStonePosition - transform.position).normalized;
            rb.velocity = direction * moveSpeed;

            // 애니메이션 업데이트 - 이동 시 반드시 1_Move 애니메이션만 활성화
            if (animator != null)
            {
                animator.SetTrigger("CancelMining");
                animator.SetBool("1_Move", true);
            }

            // 방향 설정
            UpdateDirection(direction);
            Debug.Log($"{NpcName}이(가) 돌을 향해 이동 중입니다.");
        }
        else
        {
            // 돌 근처에 도착하면 채굴 시작
            rb.velocity = Vector2.zero;

            // 애니메이션 업데이트 (이동 중지, 채굴 시작)
            if (animator != null)
            {
                animator.SetBool("1_Move", false);
                animator.SetTrigger("6_Other");

            }

            // 현재 자원 위치 저장
            currentResourcePosition = nearestStonePosition;
            resourceTileSpawner.ReserveStoneTile(nearestStonePosition);
            Debug.Log($"{NpcName}이(가) 현재 돌 위치를 저장했습니다: {currentResourcePosition}");

            // 채굴 상태 설정
            isMining = true;
            resourceTimer = CalculateGatheringTime(BASE_MINING_TIME, npcEntry.miningPower);

            Debug.Log($"{NpcName}이(가) 돌 채굴을 시작합니다.");
        }
    }

    // 전투 관련 변수
    private bool isAttacking = false;
    private float attackCooldown = 1.5f;
    private float attackRange = 1.5f;
    public int attackDamage;
    private GameObject currentTarget;

    // 전투 처리 - 적에게 이동 및 공격 구현
    // 전투 탐색 범위 (Inspector에서 조정 가능)
    private float combatSearchRange = 100f;

    // 지정된 범위 내에서 가장 가까운 적을 찾는 함수
    private GameObject FindNearestEnemyInRange(float range)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject nearest = null;
        float minDist = Mathf.Infinity;
        Vector3 currentPos = transform.position;

        foreach (GameObject enemy in enemies)
        {
            float dist = Vector3.Distance(currentPos, enemy.transform.position);
            if (dist < minDist && dist <= range)
            {
                minDist = dist;
                nearest = enemy;
            }
        }
        return nearest;
    }

    // 전투 태스크 처리 (범위 내 적만 탐색)
    private void HandleCombatTask()
    {
        ResetAllWorkAnimation();
        axe.SetActive(false);
        pickaxe.SetActive(false);
        bow.SetActive(false);
        sword.SetActive(true);

        // 이미 공격 중이면 처리하지 않음
        if (isAttacking)
            return;

        // 범위 내 적 찾기
        GameObject nearestEnemy = FindNearestEnemyInRange(combatSearchRange);
        if (nearestEnemy != null)
        {
            currentTarget = nearestEnemy;

            // 적까지 거리 계산
            float distanceToEnemy = Vector3.Distance(transform.position, nearestEnemy.transform.position);

            if (distanceToEnemy > attackRange)
            {
                // 적에게 이동
                Vector3 direction = (nearestEnemy.transform.position - transform.position).normalized;
                rb.velocity = direction * moveSpeed;

                // 애니메이션 업데이트
                if (animator != null) animator.SetBool("1_Move", true);

                // 방향 설정
                UpdateDirection(direction);
                Debug.Log($"{NpcName}이(가) 적을 향해 이동 중입니다.");
            }
            else
            {
                // 적 근처에 도착하면 정지 후 공격
                rb.velocity = Vector2.zero;
                if (animator != null) animator.SetBool("1_Move", false);

                // 공격 실행
                AttackEnemy();
            }
        }
        else
        {
            // 범위 내 적이 없는 경우 정지
            currentTarget = null;
            rb.velocity = Vector2.zero;
            if (animator != null) animator.SetBool("1_Move", false);
            Debug.Log($"{NpcName}: 근처에 적이 없습니다.");
        }
    } 

    // 적 공격 함수
    private void AttackEnemy()
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
        }
        GameManager.instance.PlaySFX("Attack");

        // NpcData에서 공격력 가져오기
        attackDamage = npcEntry != null ? npcEntry.attack : 1;

        // 공격 데미지 처리
        StartCoroutine(ApplyAttackDamage());

        // 공격 쿨다운 시작
        StartCoroutine(AttackCooldown());

        Debug.Log($"{NpcName}이(가) {attackDamage}의 데미지로 공격합니다.");
    }

    // 공격 데미지 적용 코루틴
    private IEnumerator ApplyAttackDamage()
    {
        // 공격 애니메이션이 데미지를 입히는 시점까지 대기 (약 0.3초)
        yield return new WaitForSeconds(0.3f);

        // 타겟이 여전히 존재하고 공격 범위 내에 있는지 확인
        if (currentTarget != null && Vector3.Distance(transform.position, currentTarget.transform.position) <= attackRange)
        {
            // 적에게 데미지 적용
            Enemy enemy = currentTarget.GetComponent<Enemy>();
            if (enemy != null)
            {
                // EnemyHealth 컴포넌트도 찾아서 호출
                EnemyHealth enemyHealth = currentTarget.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(attackDamage, (Vector2)transform.position);
                }

                Debug.Log($"{NpcName}이(가) {enemy.name}에게 {attackDamage} 데미지를 입혔습니다.");
            }
            else
            {
                // Enemy 컴포넌트가 없는 경우 플레이어일 수 있음
                Player player = currentTarget.GetComponent<Player>();
                if (player != null)
                {
                    // 플레이어의 EnemyHealth 컴포넌트 확인 (플레이어에게 데미지를 입히는 방법)
                    EnemyHealth playerHealth = player.GetComponent<EnemyHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(attackDamage, (Vector2)transform.position);
                        Debug.Log($"{NpcName}이(가) 플레이어에게 {attackDamage} 데미지를 입혔습니다.");
                    }
                }
            }
        }
    }

    // 공격 쿨다운 코루틴
    private IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
        Debug.Log($"{NpcName}의 공격 쿨다운이 끝났습니다.");

    }

    //활 공격 로직직
    public void HandleBowCombatTask()
    {
        ResetAllWorkAnimation();
        axe.SetActive(false);
        pickaxe.SetActive(false);
        bow.SetActive(true);
        sword.SetActive(false);

        ArrowToMove();
        attackDamage = npcEntry.attack;
    }
    private void ArrowToMove()
    {
        if (isAttacking)
            return;

        // 지정한 범위 내에서만 Enemy 탐색
        float enemyDetectRange = 100f; // 원하는 탐지 범위(예시)
        GameObject nearestEnemy = FindNearestObjectWithTagInRange("Enemy", enemyDetectRange);
        if (nearestEnemy != null)
        {
            currentTarget = nearestEnemy;
            float distanceToEnemy = Vector3.Distance(transform.position, nearestEnemy.transform.position);

            // 활 사거리 및 카이팅 최소 거리 계산
            ArrowShooter_Npc shooter = bow != null ? bow.GetComponentInParent<ArrowShooter_Npc>() : GetComponentInParent<ArrowShooter_Npc>();
            float maxShootDistance = shooter != null ? shooter.maxShootDistance : attackRange;
            float minKiteDistance = maxShootDistance * 0.6f; // 예시: 사거리의 60% 이하로 가까워지면 도망

            Debug.Log($"{NpcName}의 활 사거리: {maxShootDistance}, 카이팅 최소 거리: {minKiteDistance}");

            Vector3 direction = (nearestEnemy.transform.position - transform.position).normalized;

            if (distanceToEnemy > maxShootDistance)
            {
                // 적이 너무 멀면 접근
                rb.velocity = direction * moveSpeed;
                if (animator != null) animator.SetBool("1_Move", true);
                UpdateDirection(direction);
                Debug.Log($"{NpcName}이(가) 적을 향해 이동 중입니다.");
            }
            else if (distanceToEnemy < minKiteDistance)
            {
                // 적이 너무 가까우면 도망(반대 방향), 시선도 도망 방향
                rb.velocity = -direction * moveSpeed;
                if (animator != null) animator.SetBool("1_Move", true);
                UpdateDirection(-direction); // 도망 방향으로 시선
                Debug.Log($"{NpcName}이(가) 적에게서 도망 중입니다. (카이팅, 시선: 도망 방향)");
            }
            else
            {
                // 카이팅 구간: 멈추고 공격, 시선은 적 방향
                rb.velocity = Vector2.zero;
                if (animator != null) animator.SetBool("1_Move", false);
                UpdateDirection(direction); // 적 방향으로 시선 전환
                Debug.Log($"{NpcName}이(가) 적당한 사거리에서 대기 중 (카이팅 구간, 시선: 적)");
            }
        }
        else
        {
            currentTarget = null;
            rb.velocity = Vector2.zero;
            if (animator != null) animator.SetBool("1_Move", false);
            Debug.Log($"{NpcName}: 근처에 적이 없습니다.");
        }
    }





    // 지정한 범위 내에서만 가장 가까운 오브젝트 찾기
    private GameObject FindNearestObjectWithTagInRange(string tag, float range)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        GameObject nearest = null;
        float minDistance = float.MaxValue;

        foreach (GameObject obj in objects)
        {
            float distance = Vector3.Distance(transform.position, obj.transform.position);
            if (distance < minDistance && distance <= range)
            {
                minDistance = distance;
                nearest = obj;
            }
        }

        return nearest;
    }

    // 태그로 가장 가까운 오브젝트 찾기 (전체)
    private GameObject FindNearestObjectWithTag(string tag)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        GameObject nearest = null;
        float minDistance = float.MaxValue;

        foreach (GameObject obj in objects)
        {
            float distance = Vector3.Distance(transform.position, obj.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = obj;
            }
        }

        return nearest;
    }

    // 방향 업데이트 및 UI Flip
    private void UpdateDirection(Vector3 direction)
    {
        if (direction.x > 0)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            facingleft = false;
            // 좌우 반전 시 UI도 반전
            NpcHealth health = GetComponent<NpcHealth>();
            if (health != null)
            {
                health.FlipUI(transform.localScale.x < 0);
            }
        }
        else if (direction.x < 0)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            facingleft = true;
            // 좌우 반전 시 UI도 반전
            NpcHealth health = GetComponent<NpcHealth>();
            if (health != null)
            {
                health.FlipUI(transform.localScale.x < 0);
            }
        }
    }

    // 모든 작업/전투 애니메이션 트리거 및 파라미터 초기화
    private void ResetAllWorkAnimation()
    {
        if (animator == null) return;
        animator.ResetTrigger("6_Other");   // 곡괭이,돌돌 채집
        // animator.ResetTrigger("7_Bow");     // 활
        animator.ResetTrigger("2_Attack");  // 근접공격
        animator.ResetTrigger("CancelMining"); // 채집 취소
        animator.SetBool("1_Move", false);  // 이동 중지
    }

    // 작업 상태 초기화를 위한 공통 메서드
    private void ResetTaskState()
    {
        ResetAllWorkAnimation();
        // 채집 애니메이션 중지
        if (animator != null)
        {
            Debug.Log(" 작업 중단");
        }
        // 자원 예약 해제
        if (currentResourcePosition != Vector3.zero)
        {
            resourceTileSpawner.ReleaseStoneTile(currentResourcePosition);
        }
        currentResourcePosition = Vector3.zero;

        // 채집 상태 초기화
        isWoodcutting = false;
        isMining = false;
        resourceTimer = 0f;

        sword.SetActive(false);
        axe.SetActive(false);
        pickaxe.SetActive(false);
        bow.SetActive(false);


        // 자원 위치 초기화 - 이렇게 하면 다음에 작업을 시작할 때 가장 가까운 자원을 다시 찾게 됨
        currentResourcePosition = Vector3.zero;
    }

    // 현재 작업 중지
    public void StopCurrentTask()
    {
        if (currentTask == NpcTask.None) return;

        // NPC 작업 초기화
        SetTask(NpcTask.None);
    }

    // 작업 설정
    private bool returningToInitialPosition = false; // 초기 위치로 돌아가는 중인지 표시

    public void SetTask(NpcTask task)
    {
        // 작업 상태 초기화
        ResetTaskState();

        // 새 작업 설정
        currentTask = task;

        if (task != NpcTask.None)
        {
            // 새로운 작업이 설정되면 초기 위치로 돌아가는 상태 초기화
            returningToInitialPosition = false;
            randomMovementActive = false;
            Debug.Log($"{NpcName}이(가) {task} 작업을 시작합니다.");
        }
        else
        {
            // 작업이 초기화되면 NPC를 초기 위치 근처로 이동시키고 랜덤 이동 상태로 설정
            Debug.Log($"{NpcName}이(가) 작업을 중지하고 랜덤 이동 모드로 전환합니다.");
            animator.SetTrigger("CancelMining");


            // 현재 위치가 초기 위치에서 멀어졌다면 초기 위치 근처로 이동
            float distanceFromStart = Vector3.Distance(transform.position, initialPosition);
            if (distanceFromStart > movementRange * 0.5f)
            {
                // 초기 위치로 돌아가는 상태로 설정
                returningToInitialPosition = true;
                randomMovementActive = false; // 랜덤 이동 비활성화

                // 초기 위치 방향으로 이동하는 로직 추가
                Vector3 direction = (initialPosition - transform.position).normalized;
                rb.velocity = direction * moveSpeed;

                // 애니메이션 업데이트
                if (animator != null)
                {

                    animator.SetBool("1_Move", true);
                }

                // 방향 설정
                UpdateDirection(direction);
                Debug.Log($"{NpcName}이(가) 초기 위치로 돌아가는 중입니다.");

                // 상태 변경
                currentState = NpcState.Moving;
                isMoving = true;
            }
            else
            {
                // 초기 위치 근처에 있는 경우 정지 후 다음 행동 결정
                returningToInitialPosition = false;
                randomMovementActive = true; // 랜덤 이동 활성화
                rb.velocity = Vector2.zero;
                if (animator != null) animator.SetBool("1_Move", false);
                currentState = NpcState.Idle;
                isMoving = false;
                DecideNextAction();
            }
        }
    }
}
