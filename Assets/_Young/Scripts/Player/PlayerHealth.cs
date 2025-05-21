using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("체력 설정")]
    public float maxHealth = 100f;        // 최대 체력
    private float currentHealth;           // 현재 체력

    [Header("UI 요소")]
    public Image healthBarImage;           // 체력바 이미지 (Fill 방식 이미지여야 함)
    public float smoothSpeed = 5f;         // 체력바 변화 속도 (부드러운 전환)
    private float targetFill;              // 목표 체력바 비율

    [Header("피격 효과")]
    public bool useFlashEffect = true;     // 피격 시 플래시 효과 사용 여부
    public Image damageFlashImage;         // 피격 시 화면 플래시 이미지
    public float flashSpeed = 5f;          // 플래시 사라지는 속도
    private Color flashColor;              // 플래시 색상 (알파값 조절)

    [Header("캐릭터 깜빡임 효과")]
    public float blinkDuration = 1.0f;     // 깜빡임 지속 시간
    public float blinkRate = 0.1f;         // 깜빡임 간격 (초)
    private SpriteRenderer[] spriteRenderers; // 플레이어 스프라이트 렌더러

    [Header("피격 무적시간")]
    public float invincibilityTime = 1.0f; // 피격 후 무적 시간 (초)
    private bool isInvincible = false;     // 무적 상태 여부

    private bool isDead = false;           // 사망 상태 여부

    public GameObject exitPanel; // Exit Panel 오브젝트

    // 애니메이션 컴포넌트
    private Animator animator;

    void Start()
    {
        // 초기 체력 설정
        currentHealth = maxHealth;
        targetFill = 1f;

        // 애니메이터 컴포넌트 가져오기
        animator = GetComponentInChildren<Animator>();

        // 스프라이트 렌더러 컴포넌트 가져오기
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        // 피격 플래시 이미지 초기화
        if (damageFlashImage != null)
        {
            flashColor = damageFlashImage.color;
            flashColor.a = 0f;
            damageFlashImage.color = flashColor;
        }

        // 체력바 초기화
        UpdateHealthBar();
    }

    void Update()
    {
        // 체력바 부드럽게 변화
        if (healthBarImage != null)
        {
            healthBarImage.fillAmount = Mathf.Lerp(healthBarImage.fillAmount, targetFill, Time.deltaTime * smoothSpeed);
        }

        // 피격 플래시 효과 업데이트
        if (useFlashEffect && damageFlashImage != null)
        {
            if (flashColor.a > 0)
            {
                flashColor.a = Mathf.Max(0, flashColor.a - Time.deltaTime * flashSpeed);
                damageFlashImage.color = flashColor;
            }
        }
    }

    // 피격 처리 함수
    public void TakeDamage(float damage)
    {
        // 무적 상태거나 사망 상태라면 피격 무시
        if (isInvincible || isDead) return;

        // 체력 감소
        currentHealth = Mathf.Max(0, currentHealth - damage);

        // 체력바 업데이트
        UpdateHealthBar();
        GameManager.instance.PlaySFX("Damaged");


        // 피격 효과 표시
        ShowDamageEffect();

        // 피격 애니메이션 재생 (있다면)
        if (animator != null)
        {
            animator.SetTrigger("3_Damaged");
        }

        Debug.Log("플레이어가 " + damage + "의 데미지를 입었습니다. 남은 체력: " + currentHealth);

        // 사망 확인
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // 무적 시간 설정
            StartCoroutine(InvincibilityCoroutine());
        }
    }

    // 체력 회복 함수
    public void Heal(float amount)
    {
        if (isDead) return;

        // 체력 회복 (최대치 초과 불가)
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        // 체력바 업데이트
        UpdateHealthBar();

        Debug.Log("플레이어가 " + amount + "의 체력을 회복했습니다. 현재 체력: " + currentHealth);
    }

    // 체력바 업데이트
    private void UpdateHealthBar()
    {
        if (healthBarImage != null)
        {
            targetFill = currentHealth / maxHealth;
        }
    }

    // 피격 효과 표시
    private void ShowDamageEffect()
    {
        if (useFlashEffect && damageFlashImage != null)
        {
            flashColor.a = 0.5f;  // 플래시 알파값 설정
            damageFlashImage.color = flashColor;
        }
    }

    // 사망 처리
    private void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("플레이어 사망");

        // 사망 애니메이션 재생
        if (animator != null)
        {
            animator.SetTrigger("4_Death");
            // 애니메이션이 끝날 때까지 플레이어 움직임 비활성화 등의 처리를 여기서 합니다
            // 예: GetComponent<PlayerMovement>().enabled = false;
        }
        if (exitPanel != null && exitPanel.layer == LayerMask.NameToLayer("UI"))
        {
            StartCoroutine(GameOver());
        }

    }
    private System.Collections.IEnumerator GameOver()
    {
        yield return new WaitForSeconds(1f); // 1초 대기
        exitPanel.SetActive(true);
        Time.timeScale = 0;
    }

    // 무적 시간 코루틴
    private System.Collections.IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;

        // 캐릭터 깜빡임 효과
        float endTime = Time.time + blinkDuration;
        bool visible = false;

        while (Time.time < endTime)
        {
            // 캐릭터 가시성 전환
            visible = !visible;
            SetCharacterVisibility(visible);

            yield return new WaitForSeconds(blinkRate);
        }

        // 깜빡임 종료 후 항상 보이게 설정
        SetCharacterVisibility(true);

        isInvincible = false;
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
                    Color color = renderer.color;
                    color.a = visible ? 1f : 0.3f; // 완전히 투명하지 않고 반투명으로 설정
                    renderer.color = color;
                }
            }
        }
    }

    // 현재 체력 비율 가져오기 (0~1)
    public float GetHealthRatio()
    {
        return currentHealth / maxHealth;
    }

    // 현재 체력 가져오기
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    // 사망 상태 확인
    public bool IsDead()
    {
        return isDead;
    }

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
