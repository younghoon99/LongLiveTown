using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.VisualScripting;

public class NpcHealth : MonoBehaviour
{
    private bool isFlipped = false;

    [Header("체력 설정")]
    [SerializeField] private float maxHealth = 100f;      // 최대 체력 (초기값, NpcData에서 재정의됨)
    [SerializeField] private float currentHealth;         // 현재 체력
    
    [Header("UI 설정")]
    [SerializeField] private Image healthBarImage;        // 체력바 이미지 (Fill 방식)
    [SerializeField] private float smoothSpeed = 5f;      // 체력바 변화 속도
    [SerializeField] private GameObject floatingDamageTextPrefab; // 데미지 텍스트 프리팹
    [SerializeField] private Canvas worldCanvas;          // 월드 캔버스 (없으면 자동 생성)
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0, 1.5f, 0); // 체력바 위치 오프셋
    private float targetFill;                            // 목표 체력바 비율
    
    [Header("피격 효과")]
    [SerializeField] private float invincibilityTime = 0.5f; // 무적 시간
    [SerializeField] private float blinkRate = 0.1f;      // 깜빡임 간격 (초)
    private bool isInvincible = false;                    // 무적 상태 여부 
    
    // 컴포넌트 참조
    private Animator animator;
    private SpriteRenderer[] spriteRenderers;
    private Rigidbody2D rb;
    private Npc npcScript;
    
    // 원래 색상 저장용 딕셔너리
    private Dictionary<SpriteRenderer, Color> originalColors = new Dictionary<SpriteRenderer, Color>();
    
    // 상태 관리
    private bool isDead = false;

    void Start()
    {
        healthBarImage = transform.GetChild(2).transform.GetChild(0).transform.Find("Heath").GetComponent<Image>();
        floatingDamageTextPrefab = transform.GetChild(2).transform.GetChild(1).gameObject;
        worldCanvas = transform.GetChild(2).gameObject.GetComponent<Canvas>();

        // 컴포넌트 참조 가져오기
        animator = GetComponentInChildren<Animator>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        npcScript = GetComponent<Npc>();

        
        // 잠시 대기하여 Npc 컴포넌트가 초기화될 시간 제공
        Invoke("InitializeHealth", 0.1f);
        
        // 각 스프라이트 렌더러의 원래 색상 저장
        SaveOriginalColors();
        
       
    }
    
    // 초기 체력 설정 함수
    private void InitializeHealth()
    {
        // NpcData에서 체력 값 가져오기
        if (npcScript != null && npcScript.NpcEntry != null)
        {
            // NpcData에서 설정한 체력 값 가져오기 (health * 10)
            maxHealth = npcScript.NpcEntry.health * 10f;
            
            // 체력이 0 이하인 경우 최소값 10으로 설정 (health=1 * 10)
            if (maxHealth <= 0)
            {
                maxHealth = 10f; // 최소 체력은 10 (health=1 * 10)
                Debug.LogWarning($"NPC {npcScript.NpcName}의 체력이 0 이하였습니다. 최소값 10으로 설정합니다.");
            }
            else
            {
                Debug.Log($"NPC {npcScript.NpcName}의 최대 체력을 {maxHealth}로 설정했습니다.");
            }
        }
        else
        {
            // NpcData가 없는 경우 기본값 사용
            Debug.LogWarning("NPC 데이터를 찾을 수 없습니다. 기본 체력을 사용합니다.");
            maxHealth = 10f; // 최소 체력은 10 (health=1 * 10)
        }
        
        // 초기 체력을 최대 체력으로 설정
        currentHealth = maxHealth;
        targetFill = 1f;
        
        // 체력바 초기화
        UpdateHealthBar();
    }
    
    // 원래 색상 저장 함수
    private void SaveOriginalColors()
    {
        originalColors.Clear();
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            foreach (SpriteRenderer renderer in spriteRenderers)
            {
                if (renderer != null)
                {
                    originalColors[renderer] = renderer.color;
                }
            }
        }
    }
    
    void Update()
    {
        // 체력바 부드럽게 변화
        if (healthBarImage != null)
        {
            healthBarImage.fillAmount = Mathf.Lerp(healthBarImage.fillAmount, targetFill, Time.deltaTime * smoothSpeed);
            
            // 체력바가 NPC의 머리 위를 따라다니도록 설정
            Transform healthBarTransform = healthBarImage.transform.parent;
            if (healthBarTransform != null)
            {
                // NPC 머리 위에 위치하도록 설정
                healthBarTransform.position = transform.position + healthBarOffset;
                
                // 체력바가 항상 카메라를 향하도록 설정 (빌보드 효과)
                if (Camera.main != null)
                {
                    healthBarTransform.LookAt(healthBarTransform.position + Camera.main.transform.forward);
                }
            }
        }
    }

    // 데미지 처리 함수
    public void TakeDamage(float damage, Vector2 hitPosition = default)
    {
        // 죽었거나 무적 상태면 데미지를 받지 않음
        if (isDead || isInvincible) return;
        
        // 체력 감소
        currentHealth = Mathf.Max(0, currentHealth - damage);
        
        // 데미지 텍스트 생성
        ShowDamageText(damage);
        
        // 체력바 업데이트
        UpdateHealthBar();
        GameManager.instance.PlaySFX("Damaged");

        // 애니메이션 재생
        if (animator != null)
        {
            animator.SetTrigger("3_Damaged");
            
            // 채굴 중이면 채굴 애니메이션 트리거
            if (npcScript.isWoodcutting || npcScript.isMining)
            {
                animator.SetTrigger("6_Other");
                // 애니메이션 상태를 계속 유지하기 위해 약간의 딜레이 후 다시 트리거
                StartCoroutine(KeepMiningAnimation());
            }
        }
        
        // 피격 효과 코루틴 실행
        if (gameObject.activeInHierarchy)
        {
            isInvincible = true;
            StartCoroutine(InvincibilityCoroutine());
        }
        
        // NPC 이름 가져오기
        string npcName = "NPC";
        if (npcScript != null)
        {
            npcName = npcScript.NpcName;
        }
        
        // 디버그 출력
        Debug.Log($"NPC {npcName}이(가) {damage}의 피해를 입었습니다. 현재 체력: {currentHealth}/{maxHealth}");
        
        // 체력이 0이 되면 사망 처리
        if (currentHealth < 1 && !isDead)
        {
            Die();
        }
    }
    
    // 채굴 애니메이션을 계속 유지하는 코루틴
    private System.Collections.IEnumerator KeepMiningAnimation()
    {
        yield return new WaitForSeconds(0.1f); // 약간의 딜레이
        if (animator != null)
        {
            animator.SetTrigger("6_Other");
        }
    }
    
    // 무적 시간 및 깜빡임 효과 코루틴
    private System.Collections.IEnumerator InvincibilityCoroutine()
    {
        // 캐릭터 깜빡임 효과
        float endTime = Time.time + invincibilityTime;
        bool visible = false;
        
        while (Time.time < endTime)
        {
            // 캐릭터 가시성 전환
            visible = !visible;
            SetCharacterVisibility(visible);
            
            yield return new WaitForSeconds(blinkRate);
        }
        
        // 깜빡임 종료 후 원래 색상으로 복원
        RestoreOriginalColors();
        
        // 무적 해제
        isInvincible = false;
    }
    
    // 원래 색상으로 복원하는 함수
    private void RestoreOriginalColors()
    {
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            foreach (SpriteRenderer renderer in spriteRenderers)
            {
                if (renderer != null && originalColors.ContainsKey(renderer))
                {
                    renderer.color = originalColors[renderer];
                }
            }
        }
    }
    
    // 캐릭터 가시성 설정
    private void SetCharacterVisibility(bool visible)
    {
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            foreach (SpriteRenderer renderer in spriteRenderers)
            {
                if (renderer != null)
                {
                    if (visible)
                    {
                        // 원래 색상이 저장되어 있다면 사용, 아니면 흰색 사용
                        if (originalColors.ContainsKey(renderer))
                        {
                            Color originalColor = originalColors[renderer];
                            renderer.color = originalColor;
                        }
                        else
                        {
                            renderer.color = Color.white;
                        }
                    }
                    else
                    {
                        // 피격 시 흰색 적용
                        Color whiteColor = Color.white;
                        whiteColor.a = 0.5f; // 반투명 흰색
                        renderer.color = whiteColor;
                    }
                }
            }
        }
    }
    
    // 사망 처리 함수
    private void Die()
    {
        // 사망 상태로 변경
        isDead = true;

        // 사망 애니메이션 재생
        if (animator != null)
        {
            animator.SetTrigger("4_Death");
            // 애니메이션이 끝날 때까지 플레이어 움직임 비활성화 등의 처리를 여기서 합니다
            // 예: GetComponent<PlayerMovement>().enabled = false;
        }
        Invoke("DestroyNpc", 3.0f);
       
        GetComponent<Npc>().enabled = false;
        GetComponent<Npc>().bow.SetActive(false);
        
        // NPC 이름 가져오기
        string npcName = "NPC";
        if (npcScript != null)
        {
            npcName = npcScript.NpcName;
        }
        
        // 디버그 출력
        Debug.Log($"NPC {npcName}이(가) 사망했습니다.");
    }
    
    // NPC 오브젝트 제거 함수
    private void DestroyNpc()
    {
        Destroy(gameObject);
    }

    // 체력바 업데이트 함수
    private void UpdateHealthBar()
    {
        if (healthBarImage != null)
        {
            // 0으로 나누기 방지 (NaN 오류 방지)
            if (maxHealth <= 0)
            {
                Debug.LogError($"최대 체력이 0 이하입니다: {maxHealth}. 최소값으로 재설정합니다.");
                maxHealth = 10f; // 최소 체력은 10 (health=1 * 10)
                currentHealth = maxHealth;
            }
            
            targetFill = Mathf.Clamp01(currentHealth / maxHealth);
            // 즉시 업데이트 추가
            healthBarImage.fillAmount = targetFill;
            Debug.Log($"체력바 업데이트: {targetFill} (현재 체력: {currentHealth}/{maxHealth})");
        }
        else
        {
            Debug.LogWarning("체력바 이미지가 없습니다!");
        }
    }

    // 데미지 텍스트 표시 함수
    // 데미지 텍스트 표시 함수
    public void ShowDamageText(float damage)
    {
        // 방향 정보 저장: isFlipped

        if (floatingDamageTextPrefab != null && worldCanvas != null)
        {
            // 적 머리 위에 데미지 텍스트 생성
            GameObject damageTextObj = Instantiate(floatingDamageTextPrefab, 
            transform.position + Vector3.up * 1.2f, Quaternion.identity, worldCanvas.transform);
            // 방향에 맞게 텍스트도 반전
            float sign = isFlipped ? -1f : 1f;
            Vector3 scale = damageTextObj.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * sign;
            damageTextObj.transform.localScale = scale;

            // TextMeshProUGUI 컴포넌트 검색
            TextMeshProUGUI damageText = damageTextObj.GetComponent<TextMeshProUGUI>();
            if (damageText == null)
            {
                // 일반 TextMesh 또는 Text 컴포넌트 검색
                TextMesh textMesh = damageTextObj.GetComponent<TextMesh>();
                if (textMesh != null)
                {
                    textMesh.text = damage.ToString("0");
                    textMesh.color = Color.red;
                }
                else
                {
                    Text uiText = damageTextObj.GetComponent<Text>();
                    if (uiText != null)
                    {
                        uiText.text = damage.ToString("0");
                        uiText.color = Color.red;
                    }
                }
            }
            else
            {
                // TextMeshProUGUI 구성
                damageText.text = damage.ToString("0");
                damageText.color = Color.red;
            }

            // 데미지 텍스트 애니메이션
            StartCoroutine(AnimateDamageText(damageTextObj));
        }
    }

    // 데미지 텍스트 애니메이션 코루틴
    private System.Collections.IEnumerator AnimateDamageText(GameObject textObj)
    {
        float duration = 1.0f;
        float startTime = Time.time;
        Vector3 startPosition = textObj.transform.position;
        
        // 1초 동안 위로 움직이면서 페이드아웃
        while (Time.time < startTime + duration)
        {
            float progress = (Time.time - startTime) / duration;

            // 텍스트가 위로 올라가는 효과
            textObj.transform.position = transform.position + healthBarOffset + Vector3.up * progress * 0.5f;

            // 텍스트 컴포넌트 찾아서 알파값 조절
            TextMeshProUGUI tmpText = textObj.GetComponent<TextMeshProUGUI>();
            if (tmpText != null)
            {
                Color color = tmpText.color;
                color.a = 1f - progress;
                tmpText.color = color;
            }
            else
            {
                TextMesh textMesh = textObj.GetComponent<TextMesh>();
                if (textMesh != null)
                {
                    Color color = textMesh.color;
                    color.a = 1f - progress;
                    textMesh.color = color;
                }
                else
                {
                    Text uiText = textObj.GetComponent<Text>();
                    if (uiText != null)
                    {
                        Color color = uiText.color;
                        color.a = 1f - progress;
                        uiText.color = color;
                    }
                }
            }
            
            yield return null;
        }
        
        // 애니메이션 끝나면 오브젝트 제거
        Destroy(textObj);
    }

    public void FlipUI(bool flipX)
    {
        isFlipped = flipX;
        float sign = flipX ? -1f : 1f;
        // 체력바 루트(healthBarImage의 부모)만 반전 (크기 유지)
        if (healthBarImage != null && healthBarImage.transform.parent != null)
        {
            Vector3 scale = healthBarImage.transform.parent.localScale;
            scale.x = Mathf.Abs(scale.x) * sign;
            healthBarImage.transform.parent.localScale = scale;
        }
        // 이미 떠 있는 데미지 텍스트도 모두 반전 (크기 유지)
        if (worldCanvas != null)
        {
            foreach (Transform child in worldCanvas.transform)
            {
                Vector3 scale = child.localScale;
                scale.x = Mathf.Abs(scale.x) * sign;
                child.localScale = scale;
            }
        }
    }


    // 체력 회복 함수
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHealthBar();
        
        // NPC 이름 가져오기
        string npcName = "NPC";
        if (npcScript != null)
        {
            npcName = npcScript.NpcName;
        }
        
        Debug.Log($"NPC {npcName}이(가) {amount}만큼 회복되었습니다. 현재 체력: {currentHealth}/{maxHealth}");
    }
    
    // 체력 설정 함수
    public void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        UpdateHealthBar();
    }
    
    // 최대 체력 설정 함수
    public void SetMaxHealth(float health)
    {
        maxHealth = Mathf.Max(1, health);
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        UpdateHealthBar();
    }
    
    // 현재 체력 반환
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    // 최대 체력 반환
    public float GetMaxHealth()
    {
        return maxHealth;
    }
    
    // 체력 비율 반환 (0~1)
    public float GetHealthRatio()
    {
        return currentHealth / maxHealth;
    }
    
    // 체력 관련 속성 공개
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    
    // 체력을 초기화
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        targetFill = 1f;
        isDead = false;
        isInvincible = false;
        UpdateHealthBar();
    }
}
