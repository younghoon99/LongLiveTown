using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

using Kinnly;

namespace RedstoneinventeGameStudio
{
    /// <summary>
    /// NPC 인벤토리 시스템을 관리하는 클래스
    /// 이벤트 핸들러를 구현하여 마우스 입력에 반응
    /// </summary>
    public class Inventory : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        /// NPC 참조
        /// </summary>
        private Npc npc;

        

        /// <summary>
        /// NPC와의 상호작용을 관리하는 컴포넌트
        /// </summary>
        private NpcInteraction npcInteraction;

        /// <summary>
        /// 플레이어의 위치를 참조하는 트랜스폼
        /// </summary>
        [SerializeField] private Transform playerTransform;

        /// <summary>
        /// NPC와 상호작용이 가능한 범위
        /// </summary>
        [SerializeField] private float interactionRange = 10f;

        /// <summary>
        /// 현재 슬롯에 있는 아이템
        /// </summary>
        public Kinnly.Item item;

        /// <summary>
        /// 슬롯이 점유 중인지 여부
        /// </summary>
        public bool isOccupied;

        /// <summary>
        /// 아이템 아이콘을 표시하는 이미지 컴포넌트
        /// </summary>
        public Image itemIcon;

        /// <summary>
        /// 이 인벤토리가 플레이어 인벤토리인지 여부
        /// </summary>
        public bool isPlayerInventory;

        /// <summary>
        /// 컴포넌트 초기화
        /// </summary>
        private void Start()
        {
            if (!isPlayerInventory) ComponentGet();

            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                    playerTransform = playerObj.transform;
            }

            RefreshUI();
        }

        /// <summary>
        /// 컴포넌트 활성화 시 호출
        /// </summary>
        private void OnEnable()
        {
            if (!isPlayerInventory) ComponentGet();
        }

        /// <summary>
        /// 필요한 컴포넌트들을 초기화
        /// </summary>
        void ComponentGet()
        {
            npc = GetComponentInParent<Npc>();
            npcInteraction = GetComponentInParent<NpcInteraction>();

            for (int i = 0; i < 5; i++)
            {
                var pui = GameObject.FindGameObjectsWithTag("PlayerInventory")[i].GetComponent<Inventory>();
                pui.npcInteraction = this.npcInteraction;
            }
        }

        /// <summary>
        /// 마우스 클릭 이벤트 처리
        /// </summary>
        /// <param name="eventData">이벤트 데이터</param>
        public void OnPointerClick(PointerEventData eventData)
        {
            bool isShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (ItemDraggingManager.Instance.IsDragging())
            {
                Kinnly.Item draggingItem = ItemDraggingManager.Instance.draggingItem;

                if (!isPlayerInventory && draggingItem != null && !draggingItem.CanAddToNpcInventory())
                {
                    Debug.LogWarning("이 아이템은 NPC 인벤토리에 넣을 수 없습니다.");
                    return;
                }

                if (item != null)
                {
                    // 교환
                    Kinnly.Item tempItem = item;
                    SetItem(draggingItem);
                    ItemDraggingManager.Instance.StartDragging(tempItem, tempItem.image);
                }
                else
                {
                    SetItem(draggingItem);
                    ItemDraggingManager.Instance.ClearDragging();
                }

                return;
            }

            if (isShift && item != null)
            {
                if (isPlayerInventory)
                    TryMoveItemToNearbyNpcInventory();
                else
                    TryMoveItemToPlayerInventory();

                return;
            }

            if (item != null)
            {
                ItemDraggingManager.Instance.StartDragging(item, item.image);
                SetItem(null);
                return;
            }
        }

        /// <summary>
        /// 인근 NPC 인벤토리로 아이템 이동 시도
        /// </summary>
        public void TryMoveItemToNearbyNpcInventory()
        {
            // 현재 상호작용 중인 NPC 확인
            Npc currentNpc = npcInteraction.GetCurrentNpc();

            if (npcInteraction.isInteracting && currentNpc != null)
            {
                // 아이템 타입 검증
                if (!item.CanAddToNpcInventory())
                {
                    Debug.LogError("이 아이템은 NPC 인벤토리에 넣을 수 없습니다.");
                    return;
                }

                // 현재 상호작용 중인 NPC의 카드 슬롯이 비어있으면 아이템 전달
                Inventory npcSlot = currentNpc.inventory;
                if (npcSlot != null && !npcSlot.isOccupied)
                {
                    Kinnly.Item tempItem = item;  // 복사
                    RemoveItem();                // 원래 슬롯 비우기
                    npcSlot.SetItem(tempItem);   // 대상 슬롯에 넣기
                    Debug.Log($"현재 상호작용 중인 NPC '{currentNpc.name}'에게 아이템을 전달했습니다.");

                    
                }
                else
                {
                    Debug.Log("현재 상호작용 중인 NPC의 슬롯이 이미 차 있습니다.");
                }
            }
            else
            {
                Debug.Log("NPC와 상호작용 중이 아니므로 아이템을 전달할 수 없습니다.");
            }
        }

        /// <summary>
        /// 플레이어 인벤토리로 아이템 이동 시도
        /// </summary>
        public void TryMoveItemToPlayerInventory()
        {
            var playerSlots = InventoryUIManager.Instance.GetPlayerInventorySlots();
            foreach (var slot in playerSlots)
            {
                if (!slot.isOccupied)
                {
                    Kinnly.Item tempItem = item;
                    RemoveItem();              // 원래 슬롯 비우기
                    slot.SetItem(tempItem);   // 새 슬롯에 아이템 넣기
                    return;
                }
            }

            Debug.Log("플레이어 인벤토리에 빈 칸이 없습니다.");
        }

        /// <summary>
        /// 마우스 포인터가 슬롯 위로 들어왔을 때 호출
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            // 툴팁 표시
        }

        /// <summary>
        /// 마우스 포인터가 슬롯에서 벗어났을 때 호출
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            // 툴팁 숨기기
        }

        /// <summary>
        /// 슬롯에 아이템 설정
        /// </summary>
        /// <param name="newItem">새로 설정할 아이템</param>
        public void SetItem(Kinnly.Item newItem)
        {
            if (item != null && npc != null)
            {
                npc.StopCurrentTask();
            }

            item = newItem;
            isOccupied = item != null;

            if (!isPlayerInventory && newItem != null && !newItem.CanAddToNpcInventory())
            {
                Debug.LogError("NPC 인벤토리에는 곡괭이, 검, 도끼만 넣을 수 있습니다.");
                return;
            }

            RefreshUI();

            if (isPlayerInventory)
            {
                int selectedSlotIndex = InventoryUIManager.Instance.SelectedSlotIndex;
                if (selectedSlotIndex >= 0 &&
                    selectedSlotIndex < InventoryUIManager.Instance.playerInventorySlots.Count)
                {
                    var selectedSlot = InventoryUIManager.Instance.playerInventorySlots[selectedSlotIndex];
                    if (selectedSlot == this)
                    {
                        InventoryUIManager.Instance.sword.SetActive(false);
                        InventoryUIManager.Instance.axe.SetActive(false);
                        InventoryUIManager.Instance.pickaxe.SetActive(false);
                        InventoryUIManager.Instance.hammer.SetActive(false);
                        InventoryUIManager.Instance.bow.SetActive(false);

                        if (item != null)
                        {
                            if (item.isSword) InventoryUIManager.Instance.sword.SetActive(true);
                            else if (item.isAxe) InventoryUIManager.Instance.axe.SetActive(true);
                            else if (item.isPickaxe) InventoryUIManager.Instance.pickaxe.SetActive(true);
                            else if (item.isHammer) InventoryUIManager.Instance.hammer.SetActive(true);
                            else if (item.isBow) InventoryUIManager.Instance.bow.SetActive(true);
                        }
                    }
                }
            }

            if (item != null && npc != null)
            {
                SetNpcTaskBasedOnItem(item);
            }
        }

        /// <summary>
        /// 슬롯에서 아이템 제거
        /// </summary>
        public void RemoveItem()
        {
            item = null;
            isOccupied = false;

            RefreshUI();

            if (isPlayerInventory)
            {
                int selectedSlotIndex = InventoryUIManager.Instance.SelectedSlotIndex;
                if (selectedSlotIndex >= 0 &&
                    selectedSlotIndex < InventoryUIManager.Instance.playerInventorySlots.Count)
                {
                    var selectedSlot = InventoryUIManager.Instance.playerInventorySlots[selectedSlotIndex];
                    if (selectedSlot == this)
                    {
                        InventoryUIManager.Instance.sword.SetActive(false);
                        InventoryUIManager.Instance.axe.SetActive(false);
                        InventoryUIManager.Instance.pickaxe.SetActive(false);
                        InventoryUIManager.Instance.hammer.SetActive(false);
                        InventoryUIManager.Instance.bow.SetActive(false);
                    }
                }
            }

            if (npc != null)
            {
                npc.StopCurrentTask();
            }
        }

        /// <summary>
        /// 아이템에 따라 NPC의 작업 설정
        /// </summary>
        /// <param name="item">NPC가 수행할 작업을 결정할 아이템</param>
        private void SetNpcTaskBasedOnItem(Kinnly.Item item)
        {
            if (npc == null)
            {
                Debug.LogError("[NPC 인벤토리] NPC 컴포넌트가 없습니다!");
                return;
            }

            if (item.isSword)
                npc.SetTask(Npc.NpcTask.Combat);
            else if (item.isAxe)
                npc.SetTask(Npc.NpcTask.Woodcutting);
            else if (item.isPickaxe)
                npc.SetTask(Npc.NpcTask.Mining);
            else if (item.isBow)
                npc.SetTask(Npc.NpcTask.BowCombat);
        }
        
        /// <summary>
        /// UI 업데이트
        /// </summary>
        private void RefreshUI()
        {
            if (itemIcon == null) return;

            if (item != null)
            {
                itemIcon.sprite = item.image;
                itemIcon.enabled = true;
            }
            else
            {
                itemIcon.sprite = null;
                itemIcon.enabled = false;
            }
        }
    }
}
