// using UnityEngine;
// using UnityEngine.Events;
// using UnityEngine.EventSystems;
// using TMPro;
// using RedstoneinventeGameStudio;

// public class NPCUI : MonoBehaviour
// {
//     [Header("NPC 참조")]
//     public Npc npc; // NPC 참조
//     public GameObject tapKey; //패널 복사
//     public GameObject npcPanel; // NPC 정보 패널 프리팹
//     [Header("부모 게임 오브젝트 ")]
//     public GameObject NpcPanel; // NPC의 정보 패널이 될 부모 게임오브젝트

//     private TextMeshProUGUI npcNameText; // NPC 이름 텍스트
//     private TextMeshProUGUI npcDescriptionText; // NPC 설명 텍스트
//     private TextMeshProUGUI npcInfoText; // NPC 정보 텍스트

//     private void Start()
//     {
//         // NpcMaker의 이벤트에 리스너 등록
//         NpcMaker npcMaker = FindObjectOfType<NpcMaker>();
//         if (npcMaker != null)
//         {
//             npcMaker.OnNpcCreated.AddListener(OnNpcCreated);
            
//         }
//         NpcPanel.SetActive(false);
//     }
    

//     private void OnNpcCreated(Npc newNpc)
//     {
//         Debug.Log("NPC가 생성되었습니다!");
//         npc = newNpc; // NPC 컴포넌트 설정

//         // npcPanel 복제
//         if (npcPanel != null && NpcPanel != null)
//         {
//             // NPCPanel의 자식으로 생성
//             GameObject newPanel = Instantiate(npcPanel, NpcPanel.transform);
            
//             // 텍스트 컴포넌트들 초기화
//             npcNameText = newPanel.transform.Find("Name").GetComponent<TextMeshProUGUI>();
//             npcDescriptionText = newPanel.transform.Find("Explain").GetComponent<TextMeshProUGUI>();
//             npcInfoText = newPanel.transform.Find("Info").GetComponent<TextMeshProUGUI>();

//             // 텍스트 컴포넌트들이 모두 잘 찾았는지 확인
//             if (npcNameText == null) Debug.LogError("NPCUI: Name 텍스트 컴포넌트를 찾을 수 없습니다.");
//             if (npcDescriptionText == null) Debug.LogError("NPCUI: Explain 텍스트 컴포넌트를 찾을 수 없습니다.");
//             if (npcInfoText == null) Debug.LogError("NPCUI: Info 텍스트 컴포넌트를 찾을 수 없습니다.");

//             // NPC 정보 설정
//             UpdateNPCInfo();
//         }
//         else
//         {
//             if (npcPanel == null) Debug.LogError("NPCUI: npcPanel이 할당되지 않았습니다.");
//             if (NpcPanel == null) Debug.LogError("NPCUI: NpcPanel이 할당되지 않았습니다.");
//         }
//     }

//     public void NPCui()
//     {
//         Debug.Log("npc호출입니다.");
        
//         if (npc == null)
//         {
//             Debug.LogError("NPCUI: NPC 컴포넌트가 없습니다.");
//             return;
//         }

//         // NPC 정보 업데이트
//         UpdateNPCInfo();
//     }

//     private void UpdateNPCInfo()
//     {
//         if (npc == null || npcNameText == null || npcDescriptionText == null || npcInfoText == null)
//         {
//             Debug.LogError("NPCUI: 필요한 컴포넌트가 초기화되지 않았습니다.");
//             return;
//         }

//         // NPC 이름 표시
//         npcNameText.text = npc.NpcName;

//         // NPC 설명 표시
//         npcDescriptionText.text = npc.NpcEntry?.description ?? "";

//         // NPC 정보 표시
//         string infoText = $"등급: {npc.NpcEntry.rarity}\n";
//         infoText += $"공격력: {npc.GetAttackPower()}\n";
//         infoText += $"채광력: {npc.GetMiningPower()}\n";
//         infoText += $"이동속도: {npc.GetMoveSpeedStat()}\n";
//         npcInfoText.text = infoText;
//     }

//     private void Update()
//     {
//         if (Input.GetKeyDown(KeyCode.Tab))
//         {
//             tapKey.SetActive(!tapKey.activeSelf);
//         }
//     }
// }
