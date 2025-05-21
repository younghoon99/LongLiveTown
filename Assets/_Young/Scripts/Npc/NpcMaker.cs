using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class NpcMaker : MonoBehaviour
{
    [Header("NPC 데이터")]
    [SerializeField] private NpcData npcData; // NPC 데이터 스크립터블 오브젝트 참조
    
    [Header("NPC 생성 설정")]
    [SerializeField] private Vector3 spawnPosition = new Vector3(0, 0, 0); // 기본 생성 위치
    
    [Header("UI 요소")]
    [SerializeField] private Button spawnButton; // NPC 생성 버튼
    
    [Header("NPC 등급 설정")]
    [SerializeField] private bool useRandomRarity = true; // 랜덤 등급 사용 여부
    [SerializeField] private NpcData.NpcRarity npcRarity = NpcData.NpcRarity.노말; // 고정 등급 (useRandomRarity가 false일 때 사용)

    [System.Serializable]
    public class NpcCreatedEvent : UnityEvent<Npc> { }

    public NpcCreatedEvent OnNpcCreated;

    private void Start()
    {
        // 버튼에 클릭 이벤트 리스너 추가
        if (spawnButton != null)
        {
            spawnButton.onClick.AddListener(SpawnRandomNpc);
        }
    }
    
    /// <summary>
    /// 버튼 클릭 시 호출되는 랜덤 NPC 생성 메서드
    /// </summary>
    public void SpawnRandomNpc()
    {
        if (npcData == null)
        {
            Debug.LogError("NpcMaker: NPC 데이터가 없어 생성할 수 없습니다.");
            return;
        }
        
        // NpcData의 프리팹 리스트 확인
        if (npcData.npcPrefabs == null || npcData.npcPrefabs.Count == 0)
        {
            // NpcData에 프리팹이 없고 대체 프리팹도 없으면 생성 불가
            Debug.LogError("NpcMaker: NpcData에 프리팹이 없고 대체 프리팹도 할당되지 않았습니다.");
            return;
        }
        
        // 랜덤 등급 결정 (useRandomRarity가 true일 경우)
        NpcData.NpcRarity selectedRarity = npcRarity;
        if (useRandomRarity)
        {
            // 랜덤 등급 결정 (가중치 적용: 노말 60%, 레어 25%, 영웅 10%, 전설 5%)
            float rarityRoll = Random.Range(0f, 1f);
            if (rarityRoll < 0.6f)
            {
                selectedRarity = NpcData.NpcRarity.노말;
            }
            else if (rarityRoll < 0.85f)
            {
                selectedRarity = NpcData.NpcRarity.레어;
            }
            else if (rarityRoll < 0.95f)
            {
                selectedRarity = NpcData.NpcRarity.영웅;
            }
            else
            {
                selectedRarity = NpcData.NpcRarity.전설;
            }
        }
        
        // NpcData를 사용하여 랜덤 NPC 데이터 생성
        NpcData.NpcEntry randomNpcData = npcData.GenerateRandomNpc(selectedRarity);
        
        // NPC 게임 오브젝트 생성 (NpcData의 프리팹 사용)
        GameObject prefabToUse;
        
        // randomNpcData에 프리팹이 있으면 해당 프리팹 사용
        if (randomNpcData.prefab != null)
        {
            prefabToUse = randomNpcData.prefab;
            Debug.Log($"NpcData의 NpcEntry에서 프리팹 사용: {randomNpcData.prefab.name}");
        }
        // NpcData의 프리팹 리스트에서 랜덤 선택
        else if (npcData.npcPrefabs != null && npcData.npcPrefabs.Count > 0)
        {
            prefabToUse = npcData.npcPrefabs[Random.Range(0, npcData.npcPrefabs.Count)];
            Debug.Log($"NpcData의 프리팹 리스트에서 랜덤 선택: {prefabToUse.name}");
        }
        // 프리팹이 없으면 생성 불가
        else
        {
            Debug.LogError("NpcMaker: NpcData에 프리팹이 없고 대체 프리팹도 할당되지 않았습니다.");
            return;
        }
        
        // 선택된 프리팹으로 NPC 생성
        GameObject newNpcObject = Instantiate(prefabToUse, spawnPosition, Quaternion.identity);
        
        
        // Npc 컴포넌트 가져오기
        Npc npcComponent = newNpcObject.GetComponent<Npc>();
        if (npcComponent != null)
        {
            // NPC 데이터로 초기화
            npcComponent.InitializeFromData(randomNpcData);
            
            // 생성 로그
            Debug.Log($"NPC 생성 완료: {randomNpcData.npcName} (등급: {randomNpcData.rarity}) - 위치: {spawnPosition}");
            
            // NPC 컴포넌트를 이벤트로 전달
            OnNpcCreated.Invoke(npcComponent);
        }
        else
        {
            Debug.LogError("NpcMaker: 생성된 게임 오브젝트에 Npc 컴포넌트가 없습니다.");
        }
    }
    
    /// <summary>
    /// 외부에서 호출 가능한 NPC 생성 메서드 (커스텀 위치 지정 가능)
    /// </summary>
    /// <param name="position">생성할 위치</param>
    public void SpawnRandomNpcAtPosition(Vector3 position)
    {
        // 임시로 생성 위치 변경
        Vector3 originalPosition = spawnPosition;
        spawnPosition = position;
        
        // NPC 생성
        SpawnRandomNpc();
        
        // 원래 위치 복원
        spawnPosition = originalPosition;
    }
}
