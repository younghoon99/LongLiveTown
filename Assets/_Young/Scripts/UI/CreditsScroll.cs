using UnityEngine;
using TMPro; // TextMeshPro 네임스페이스
using System.Collections; // For IEnumerator

public class CreditsScroll : MonoBehaviour
{
    public TextMeshProUGUI creditsText; // TextMeshPro UI 텍스트
    public GameObject programCredit; // 추가된 프로그램 크레딧 프리팹
    public float scrollSpeed = 50f; // 스크롤 속도

    private RectTransform textRectTransform;
    private RectTransform programCreditRectTransform;
    private bool programCreditShown = false; // 프로그램 크레딧 표시 여부
    private bool cameraColorChanged = false; // 카메라 색상 변경 여부

    void Start()
    {
        // TextMeshPro 텍스트의 RectTransform 가져오기
        textRectTransform = creditsText.GetComponent<RectTransform>();

        // 프로그램 크레딧 프리팹 RectTransform 가져오기 및 비활성화
        if (programCredit != null)
        {
            programCreditRectTransform = programCredit.GetComponent<RectTransform>();
            programCredit.SetActive(false);
        }
    }

    void ScrollCredits()
    {
        // 텍스트를 위로 스크롤
        textRectTransform.anchoredPosition += new Vector2(0, scrollSpeed * Time.deltaTime);

        // 프로그램 크레딧을 텍스트와 연결
        if (programCredit != null && !programCreditShown)
        {
            programCreditRectTransform.anchoredPosition = new Vector2(
                textRectTransform.anchoredPosition.x,
                -350f // Y축 고정 위치
            );

            if (textRectTransform.anchoredPosition.y > Screen.height + textRectTransform.rect.height)
            {
                StartCoroutine(ShowAndBlinkProgramCredit()); // 프로그램 크레딧 깜빡임 시작
                programCreditShown = true;
            }
        }

        // 카메라 색상 변경
        if (!cameraColorChanged && textRectTransform.anchoredPosition.y > Screen.height + textRectTransform.rect.height)
        {
            StartCoroutine(SmoothChangeCameraBackgroundColor(Color.white, 3f)); // 배경색을 흰색으로 부드럽게 변경
            cameraColorChanged = true;
        }
    }

    private IEnumerator ShowAndBlinkProgramCredit()
    {
        programCredit.SetActive(true);
        float blinkDuration = 3f; // 3초 동안 깜빡임
        float blinkInterval = 0.3f; // 깜빡임 간격
        float elapsedTime = 0f;

        while (elapsedTime < blinkDuration)
        {
            programCredit.SetActive(!programCredit.activeSelf); // 활성화/비활성화 반복
            elapsedTime += blinkInterval;
            yield return new WaitForSeconds(blinkInterval);
        }

        programCredit.SetActive(true); // 깜빡임 종료 후 활성화
    }

    private IEnumerator SmoothChangeCameraBackgroundColor(Color targetColor, float duration)
    {
        if (Camera.main != null)
        {
            Color initialColor = Camera.main.backgroundColor;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                Camera.main.backgroundColor = Color.Lerp(initialColor, targetColor, elapsedTime / duration);
                yield return null;
            }

            Camera.main.backgroundColor = targetColor; // 최종 색상 설정
        }
    }

    void Update()
    {
        // Update에서 CreditsScroll 호출
        ScrollCredits();
    }
}