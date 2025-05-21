using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPC 능력치 관련 클래스
/// 기존 어빌리티 시스템을 제거하고 새로운 등급 기반 시스템으로 변경됨
/// </summary>
public class NpcAbility : MonoBehaviour
{
    private Npc npc;

    [Header("채굴 관련")]
    [SerializeField] private float gatheringCoolTime = 1.0f;
    [SerializeField] private ParticleSystem gatheringEffect;
    [SerializeField] private AudioSource gatheringSound;
    
    private float currentGatheringCoolTime;
    private bool isGathering = false;

    private void Start()
    {
        npc = GetComponent<Npc>();
        currentGatheringCoolTime = 0f;
    }

    private void Update()
    {
        if (isGathering)
        {
            currentGatheringCoolTime -= Time.deltaTime;
            if (currentGatheringCoolTime <= 0f)
            {
                Gathering();
                currentGatheringCoolTime = gatheringCoolTime / npc.GetMiningPower(); // 채굴 능력치에 따라 쿨타임 감소
            }
        }
    }

    /// <summary>
    /// 채굴 시작
    /// </summary>
    public void StartGathering()
    {
        isGathering = true;
        currentGatheringCoolTime = 0f;
        
        if (gatheringEffect != null && !gatheringEffect.isPlaying)
        {
            gatheringEffect.Play();
        }
        
        if (gatheringSound != null)
        {
            gatheringSound.Play();
        }
    }

    /// <summary>
    /// 채굴 중지
    /// </summary>
    public void StopGathering()
    {
        isGathering = false;
        
        if (gatheringEffect != null && gatheringEffect.isPlaying)
        {
            gatheringEffect.Stop();
        }
        
        if (gatheringSound != null)
        {
            gatheringSound.Stop();
        }
    }

    /// <summary>
    /// 채굴 실행
    /// </summary>
    private void Gathering()
    {
        // 채굴 능력치에 따라 자원 획득량 조정
        int resourceAmount = Mathf.RoundToInt(npc.GetMiningPower() / 2f);
        if (resourceAmount < 1) resourceAmount = 1;
        
        Debug.Log($"{npc.NpcName}이(가) 자원 {resourceAmount}개를 획득했습니다.");
        
        // 여기에 인벤토리나 자원 관리 시스템에 자원 추가하는 코드를 추가할 수 있음
    }

    /// <summary>
    /// NPC가 전투 시 데미지를 계산
    /// </summary>
    /// <returns>계산된 데미지</returns>
    public int CalculateDamage()
    {
        // 기본 데미지에 공격력을 곱함
        int baseDamage = 5;
        
        // 공격력만 적용, 추가 보너스 없음
        int totalDamage = baseDamage * npc.GetAttackPower();
        return totalDamage;
    }
}
