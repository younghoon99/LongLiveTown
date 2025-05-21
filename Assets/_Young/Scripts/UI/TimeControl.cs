using UnityEngine;
using UnityEngine.Rendering.Universal;

public class TimeControl : MonoBehaviour
{
   
    [Range(0, 1)]
    public float timeOfDay = 0; // 0~1: 하루
    [SerializeField] private Light2D globalLight; // 글로벌 라이트
    private float cycleSpeed = 0.00138889f; // 현실 5초마다 게임 10분, 24시간=12분

    public float dayIntensity = 1.5f;
    public float nightIntensity = 0.3f;


    void Update()
    {
        // GameManager의 ClockTime(분 단위, 0~1439)을 기반으로 timeOfDay 동기화
        if (GameManager.instance != null)
        {
            timeOfDay = Mathf.Clamp01(GameManager.instance.ClockTime / 1440f);
        }

        // 낮/밤 밝기 조절 (경계는 부드럽게 보간)
        float intensity = 0f;
        if (timeOfDay < 0.2f)
            intensity = Mathf.Lerp(nightIntensity, dayIntensity, timeOfDay / 0.2f); // 새벽~아침
        else if (timeOfDay < 0.75f)
            intensity = dayIntensity; // 낮
        else if (timeOfDay < 0.85f)
            intensity = Mathf.Lerp(dayIntensity, nightIntensity, (timeOfDay - 0.75f) / 0.1f); // 저녁~밤
        else
            intensity = nightIntensity; // 밤

        if (globalLight != null)
            globalLight.intensity = intensity;
    }
}