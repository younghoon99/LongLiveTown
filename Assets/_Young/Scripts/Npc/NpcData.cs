using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NpcData", menuName = "NPC/NPC 데이터")]
public class NpcData : ScriptableObject
{
    // NPC 등급 정의
    public enum NpcRarity
    {
        노말 = 0,
        레어 = 1,
        영웅 = 2,
        전설 = 3
    }

    // 이름 생성을 위한 데이터
    [System.Serializable]
    public class NameData
    {
        public List<string> firstNames = new List<string>();
        public List<string> lastNames = new List<string>();
    }

    [Header("NPC 이름 데이터")]
    public NameData nameData;

    [Header("NPC 외형 프리팹")]
    public List<GameObject> npcPrefabs;

    [Header("등급별 능력치 총합")]
    public int normalStatTotal = 10;
    public int rareStatTotal = 20;
    public int epicStatTotal = 30;
    public int legendaryStatTotal = 40;

    // NPC 데이터 항목
    [System.Serializable]
    public class NpcEntry
    {
        // 기본 정보
        public string npcId;
        public string npcName;
        public string description;
        public NpcRarity rarity;
        public GameObject prefab;
        
        // 능력치
        public int attack;        // 공격력
        public int health;        // 체력
        public int miningPower;   // 채굴 능력
        public int moveSpeed;     // 이동 속도
        
        // 이동 설정
        public float idleTimeMin = 2.0f;
        public float idleTimeMax = 5.0f;
        public float moveTimeMin = 1.0f;
        public float moveTimeMax = 3.0f;
        
        // 능력치 총합 반환
        public int GetTotalStats()
        {
            return attack + health + miningPower + moveSpeed;
        }
    }

    // NPC 데이터 리스트
    public List<NpcEntry> npcEntries = new List<NpcEntry>();

    // ID로 NPC 데이터 검색
    public NpcEntry GetNpcById(string id)
    {
        return npcEntries.Find(entry => entry.npcId == id);
    }

    // 랜덤 NPC 생성
    public NpcEntry GenerateRandomNpc(NpcRarity rarity = NpcRarity.노말)
    {
        NpcEntry newNpc = new NpcEntry();
        
        // 간단한 ID 생성 (인덱스 기반으로 변경)
        newNpc.npcId = "NPC_" + Random.Range(1000, 9999).ToString();
        
        // 랜덤 이름 생성
        newNpc.npcName = GenerateRandomName();
        
        // 등급 설정
        newNpc.rarity = rarity;
        
        // 랜덤 프리팹 선택
        if (npcPrefabs != null && npcPrefabs.Count > 0)
        {
            newNpc.prefab = npcPrefabs[Random.Range(0, npcPrefabs.Count)];
        }
        
        // 등급별 설명 생성
        switch (rarity)
        {
            case NpcRarity.노말:
                newNpc.description = "평범한 NPC입니다.";
                break;
            case NpcRarity.레어:
                newNpc.description = "약간 특별한 능력을 가진 NPC입니다.";
                break;
            case NpcRarity.영웅:
                newNpc.description = "상당한 능력을 보유한 영웅적인 NPC입니다.";
                break;
            case NpcRarity.전설:
                newNpc.description = "전설적인 능력을 지닌 매우 강력한 NPC입니다.";
                break;
        }
        
        // 등급별 능력치 총합 계산
        int statTotal;
        switch (rarity)
        {
            case NpcRarity.레어:
                statTotal = rareStatTotal;
                break;
            case NpcRarity.영웅:
                statTotal = epicStatTotal;
                break;
            case NpcRarity.전설:
                statTotal = legendaryStatTotal;
                break;
            default: // 노말 등급
                statTotal = normalStatTotal;
                break;
        }
        
        // 능력치 다이나믹하게 분배 (최소 1점만 보장)
        DistributeDynamicStats(newNpc, statTotal);
        
        // 이동 시간 설정
        newNpc.idleTimeMin = Random.Range(1.5f, 3.0f);
        newNpc.idleTimeMax = newNpc.idleTimeMin + Random.Range(1.0f, 3.0f);
        newNpc.moveTimeMin = Random.Range(0.8f, 2.0f);
        newNpc.moveTimeMax = newNpc.moveTimeMin + Random.Range(1.0f, 2.0f);
        
        return newNpc;
    }
    
    // 다이나믹한 능력치 분배 - 최소 1점만 보장하고 나머지는 가중치 기반 랜덤 분배
    private void DistributeDynamicStats(NpcEntry npc, int totalPoints)
    {
        // 각 능력치 초기화
        npc.attack = 0;
        npc.health = 0;
        npc.miningPower = 0;
        npc.moveSpeed = 0;
        
        // 최소 1점은 반드시 할당
        int remainingPoints = totalPoints;
        
        // 각 능력치에 최소 1점씩 할당
        npc.attack = 1;
        npc.health = 1;
        npc.miningPower = 1;
        npc.moveSpeed = 1;
        remainingPoints -= 4;
        
        // 남은 포인트가 없으면 여기서 종료
        if (remainingPoints <= 0) return;
        
        // 각 능력치별 가중치 (특성을 강하게 만들기 위한 확률 조정)
        int attackWeight = Random.Range(1, 10);  // 공격력 가중치
        int healthWeight = Random.Range(1, 10);  // 체력 가중치
        int miningWeight = Random.Range(1, 10);  // 채굴력 가중치
        int speedWeight = Random.Range(1, 10);   // 이동속도 가중치
        
        // 남은 포인트 가중치 기반으로 랜덤 분배
        for (int i = 0; i < remainingPoints; i++)
        {
            // 가중치 총합 계산
            int totalWeight = attackWeight + healthWeight + miningWeight + speedWeight;
            
            // 0~totalWeight 사이의 랜덤 값 생성
            int roll = Random.Range(0, totalWeight);
            int runningTotal = 0;
            
            // 공격력에 포인트 할당
            runningTotal += attackWeight;
            if (roll < runningTotal)
            {
                npc.attack++;
                continue;
            }
            
            // 체력에 포인트 할당
            runningTotal += healthWeight;
            if (roll < runningTotal)
            {
                npc.health++;
                continue;
            }
            
            // 채굴력에 포인트 할당
            runningTotal += miningWeight;
            if (roll < runningTotal)
            {
                npc.miningPower++;
                continue;
            }
            
            // 이동속도에 포인트 할당
            npc.moveSpeed++;
        }
        
        // 디버그 로그
        Debug.Log($"NPC 능력치 분배 결과: 공격력-{npc.attack}, 체력-{npc.health}, 채굴력-{npc.miningPower}, 이동속도-{npc.moveSpeed}");
    }
    
    // 기존 메서드들은 유지
    private void DistributeBalancedStats(NpcEntry npc, int totalPoints)
    {
        // 각 능력치는 최소 1 이상
        npc.attack = 1;
        npc.health = 1;
        npc.miningPower = 1;
        npc.moveSpeed = 1;
        
        // 남은 포인트
        int remainingPoints = totalPoints - 4;
        
        // 노말 등급이면 모든 능력치에 최소 1씩 더 추가 (총 8포인트 사용)
        if (remainingPoints >= 4)
        {
            npc.attack++;
            npc.health++;
            npc.miningPower++;
            npc.moveSpeed++;
            remainingPoints -= 4;
        }
        
        // 나머지 포인트는 랜덤 분배하되 너무 치우치지 않게 조정
        if (remainingPoints > 0)
        {
            int[] statBoosts = new int[4]; // 각 능력치 별 추가 포인트
            
            // 첫 번째 라운드: 균등하게 분배 시도
            for (int i = 0; i < remainingPoints && i < 4; i++)
            {
                statBoosts[i % 4]++;
            }
            
            // 두 번째 라운드: 남은 포인트는 랜덤 분배
            remainingPoints -= Mathf.Min(remainingPoints, 4);
            while (remainingPoints > 0)
            {
                int statIndex = Random.Range(0, 4);
                statBoosts[statIndex]++;
                remainingPoints--;
            }
            
            // 부스트 적용
            npc.attack += statBoosts[0];
            npc.health += statBoosts[1];
            npc.miningPower += statBoosts[2];
            npc.moveSpeed += statBoosts[3];
        }
    }
    
    // 랜덤 능력치 분배 (기존 메서드는 유지)
    private void DistributeRandomStats(NpcEntry npc, int totalPoints)
    {
        // 최소 능력치 설정 (각 1)
        npc.attack = 1;
        npc.health = 1;
        npc.miningPower = 1;
        npc.moveSpeed = 1;
        
        // 남은 포인트 계산
        int remainingPoints = totalPoints - 4; // 이미 각 능력치에 1씩 할당했으므로
        
        // 남은 포인트 랜덤 분배
        while (remainingPoints > 0)
        {
            int statToIncrease = Random.Range(0, 4); // 0: 공격력, 1: 체력, 2: 채굴력, 3: 이동속도
            
            switch (statToIncrease)
            {
                case 0:
                    npc.attack++;
                    break;
                case 1:
                    npc.health++;
                    break;
                case 2:
                    npc.miningPower++;
                    break;
                case 3:
                    npc.moveSpeed++;
                    break;
            }
            
            remainingPoints--;
        }
    }
    
    // 랜덤 이름 생성
    private string GenerateRandomName()
    {
        if (nameData == null || nameData.firstNames.Count == 0 || nameData.lastNames.Count == 0)
        {
            return "NPC" + Random.Range(1000, 9999);
        }
        
        string firstName = nameData.firstNames[Random.Range(0, nameData.firstNames.Count)];
        string lastName = nameData.lastNames[Random.Range(0, nameData.lastNames.Count)];
        
        return $"{firstName} {lastName}";
    }
}
