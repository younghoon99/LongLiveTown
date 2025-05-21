using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class HQHealth : MonoBehaviour
{
    [Header("체력 설정")]
    public float maxHealth = 100f; // 최대 체력
    private float currentHealth;

    [Header("UI 설정")]

    public Image healthBarImage; // 체력바 이미지 (Fill 방식)
    public float smoothSpeed = 5f; // 체력바 변화 속도
    public GameObject floatingDamageTextPrefab; // 데미지 텍스트 프리팹
    public Vector3 healthBarOffset = new Vector3(0, 2f, 0); // 체력바 위치 오프셋
    public GameObject exitPanel; // Exit Panel 오브젝트
    public Canvas worldCanvas;          // 월드 캔버스
    private float targetFill;

    private bool isDestroyed = false; // HQ 파괴 여부

    [Header("피격 깜빡임 효과")]
    [SerializeField] private float blinkDuration = 0.5f; // 깜빡임 지속 시간
    [SerializeField] private float blinkInterval = 0.1f; // 깜빡임 간격

    void Start()
    {
        // 초기 체력 설정
        currentHealth = maxHealth;
        targetFill = 1f;

        // 체력바 초기화
        UpdateHealthBar();

        // Exit Panel 비활성화
        if (exitPanel != null)
        {
            exitPanel.SetActive(false);
        }
    }

    void Update()
    {
        // 체력바 부드럽게 변화
        if (healthBarImage != null)
        {
            healthBarImage.fillAmount = Mathf.Lerp(healthBarImage.fillAmount, targetFill, Time.deltaTime * smoothSpeed);

            // 체력바 위치 업데이트
            Transform healthBarTransform = healthBarImage.transform.parent;
            if (healthBarTransform != null)
            {
                healthBarTransform.position = transform.position + healthBarOffset;

                // 체력바가 항상 카메라를 향하도록 설정 (빌보드 효과)
                if (Camera.main != null)
                {
                    healthBarTransform.LookAt(healthBarTransform.position + Camera.main.transform.forward);
                }
            }
        }
    }

    public void TakeDamage(float damage)
    {
        // 이미 파괴된 경우 무시
        if (isDestroyed) return;

        // 체력 감소
        currentHealth = Mathf.Max(0, currentHealth - damage);

        // 체력바 업데이트
        UpdateHealthBar();
        GameManager.instance.PlaySFX("Damaged");

        // 데미지 텍스트 표시
        ShowDamageText(damage);

        // 깜빡임 효과 시작
        StartCoroutine(BlinkEffect());

        Debug.Log("HQ가 " + damage + "의 데미지를 입었습니다. 남은 체력: " + currentHealth);

        // 파괴 확인
        if (currentHealth <= 0)
        {
            DestroyHQ();
        }
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    private void UpdateHealthBar()
    {
        if (healthBarImage != null)
        {
            targetFill = currentHealth / maxHealth;
        }
    }

    public void ShowDamageText(float damage)
    {
        // 방향 정보 저장: isFlipped

        if (floatingDamageTextPrefab != null && worldCanvas != null)
        {
            // 머리 위에 데미지 텍스트 생성
            GameObject damageTextObj = Instantiate(floatingDamageTextPrefab, transform.position + Vector3.up * 4.0f, Quaternion.identity, worldCanvas.transform);
            
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

    private IEnumerator AnimateDamageText(GameObject textObj)
    {
        float duration = 1.0f;
        float startTime = Time.time;
        Vector3 startOffset = Vector3.up * 1.2f;

        while (Time.time < startTime + duration)
        {
            float progress = (Time.time - startTime) / duration;

            // 텍스트가 위로 올라가는 효과
            textObj.transform.position = transform.position + healthBarOffset + Vector3.up * progress * 0.5f;

            // 텍스트 페이드아웃
            TextMeshProUGUI tmpText = textObj.GetComponent<TextMeshProUGUI>();
            if (tmpText != null)
            {
                Color color = tmpText.color;
                color.a = 1f - progress;
                tmpText.color = color;
            }

            yield return null;
        }

        Destroy(textObj);
    }


    private IEnumerator BlinkEffect()
    {
        float elapsedTime = 0f;
        bool isVisible = true;

        while (elapsedTime < blinkDuration)
        {
            // 스프라이트 렌더러의 가시성을 토글
            SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
            foreach (var renderer in spriteRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = isVisible;
                }
            }
    
            isVisible = !isVisible;
            elapsedTime += blinkInterval;
            yield return new WaitForSeconds(blinkInterval);
        }
    
        // 깜빡임 종료 후 스프라이트 렌더러를 다시 활성화
        SpriteRenderer[] finalSpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var renderer in finalSpriteRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }
    }
    

    private void DestroyHQ()
    {
        if (isDestroyed) return;

        isDestroyed = true;
        Debug.Log("HQ 파괴됨");

        // ExitPanel 활성화 (UI 레이어 안에 있는 오브젝트로 처리)
        if (exitPanel != null && exitPanel.layer == LayerMask.NameToLayer("UI"))
        {
            exitPanel.SetActive(true);
            Time.timeScale = 0;
            //씬 재시작->PauseMenu 스크립트에 있음음
        }
        else
        {
            Debug.LogWarning("ExitPanel 태그를 가진 UI 레이어의 GameObject를 찾을 수 없습니다.");
        }

        // 체력바와 관련 UI 요소 비활성화
        if (healthBarImage != null)
        {
            healthBarImage.transform.parent.gameObject.SetActive(false);
        }

        // HQ 오브젝트 제거
        Destroy(gameObject);
    }
}

