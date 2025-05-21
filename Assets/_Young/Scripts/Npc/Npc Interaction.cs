using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NpcInteraction : MonoBehaviour
{
    [Header("상호작용 설정")]
    [SerializeField] private float interactionRange = 2.0f;
    [SerializeField] private KeyCode interactionKey = KeyCode.F;

    [Header("UI 오프셋 설정")]
    [SerializeField] private Vector3 promptOffset = new Vector3(0, 2.0f, 0); // NPC 머리 위 오프셋
    [SerializeField] private Vector3 infoOffset = new Vector3(0, 2.5f, 0);    // 정보 패널 오프셋

    [Header("UI 설정")]
    private GameObject interactionPrompt;    // NPC와 상호작용할 수 있을 때 표시되는 프롬프트 UI (예: "F키를 눌러 대화하기")
    private GameObject npcInfoPanel;         // NPC 정보를 표시하는 패널 (이름, 등급, 능력치 등을 포함)
    private TextMeshProUGUI npcInfoText;     // NPC 세부 정보를 표시하는 텍스트 컴포넌트

    // 참조 변수
    private Transform playerTransform;
    private Npc currentNpc;
    public bool isInteracting = false;


    private void Start()
    {
        // 플레이어 트랜스폼 찾기
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        interactionPrompt = transform.GetChild(1).transform.Find("NPCPrompt").gameObject;
        npcInfoPanel = transform.GetChild(1).transform.Find("npcInfoPanel").gameObject;
        npcInfoText = transform.GetChild(1).transform.Find("npcInfoPanel").transform.GetChild(1).GetComponent<TextMeshProUGUI>();


        // UI 초기 상태 설정
        if (interactionPrompt) interactionPrompt.SetActive(false);
        if (npcInfoPanel) npcInfoPanel.SetActive(false);
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // ▶ 먼저 모든 NPC의 프롬프트 숨기기
        Npc[] allNpcs = FindObjectsOfType<Npc>();
        foreach (Npc npc in allNpcs)
        {
            npc.transform.GetChild(1).transform.Find("NPCPrompt").gameObject.SetActive(false);
        }

        // 가장 가까운 NPC 찾기
        Npc nearestNpc = FindNearestNpc();

        // NPC와의 상호작용 처리
        if (nearestNpc != null)
        {
            // 프롬프트 표시
            if (interactionPrompt && !isInteracting)
            {
                nearestNpc.transform.GetChild(1).transform.Find("NPCPrompt").gameObject.SetActive(true);
                interactionPrompt.transform.position = Camera.main.WorldToScreenPoint(
                    nearestNpc.transform.position + promptOffset);
            }

            // 키 입력
            if (Input.GetKeyDown(interactionKey))
            {
                if (!isInteracting) StartInteraction(nearestNpc);
                else EndInteraction();
            }
        }
        else
        {
            if (interactionPrompt) interactionPrompt.SetActive(false);
            if (isInteracting) EndInteraction();
        }

        // UI 위치 갱신
        UpdateUIPositions();
    }


    private void UpdateUIPositions()
    {
        if (currentNpc != null)
        {
            // UI 요소 위치 업데이트
            if (interactionPrompt && interactionPrompt.activeSelf)
            {
                interactionPrompt.transform.position = Camera.main.WorldToScreenPoint(
                    currentNpc.transform.position + promptOffset);
            }

            if (npcInfoPanel && npcInfoPanel.activeSelf)
            {
                npcInfoPanel.transform.position = Camera.main.WorldToScreenPoint(
                    currentNpc.transform.position + infoOffset);
            }
        }
    }

    // 가장 가까운 NPC 찾기
    private Npc FindNearestNpc()
    {
        Npc closestNpc = null;
        float closestDistance = interactionRange;

        Npc[] allNpcs = FindObjectsOfType<Npc>();
        foreach (Npc npc in allNpcs)
        {
            float distance = Vector3.Distance(playerTransform.position, npc.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestNpc = npc;
            }
        }

        // currentNpc = closestNpc;
        return closestNpc;
    }

    // NPC와 상호작용 시작
    private void StartInteraction(Npc npc)
    {
        currentNpc = npc;
        isInteracting = true;

        // 플레이어 공격 비활성화
        Player player = playerTransform != null ? playerTransform.GetComponent<Player>() : null;
        if (player != null)
            player.isAttack = false;

        // NPC 정보 패널 표시
        if (npcInfoPanel)
        {
            npc.transform.GetChild(1).transform.Find("npcInfoPanel").gameObject.SetActive(true);
            if (npcInfoText) npcInfoText.text = npc.GetNpcInfoText();
        }

        // 상호작용 프롬프트 숨기기
        if (interactionPrompt) npc.transform.GetChild(1).transform.Find("NPCPrompt").gameObject.SetActive(false);

        // NPC에게 상호작용 시작 알림
        npc.OnInteractionStart();
    }

    // NPC와 상호작용 종료
    private void EndInteraction()
    {
        isInteracting = false;

        // 플레이어 공격 다시 활성화
        Player player = playerTransform != null ? playerTransform.GetComponent<Player>() : null;
        if (player != null)
            player.isAttack = true;

        // NPC 정보 패널 숨기기
        if (npcInfoPanel) npcInfoPanel.SetActive(false);

        // NPC에게 상호작용 종료 알림
        if (currentNpc != null)
        {
            currentNpc.OnInteractionEnd();
            currentNpc = null;
        }
    }

    // 현재 상호작용 중인지 확인
    public bool IsInteracting()
    {
        return isInteracting;
    }

    // 현재 상호작용 중인 NPC 가져오기
    public Npc GetCurrentNpc()
    {
        return currentNpc;
    }
}
