// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.EventSystems;
// using UnityEngine.UI;
// using Kinnly; // Kinnly 네임스페이스 사용

// // 인벤토리 슬롯 클래스: 아이템을 담을 수 있는 UI 요소
// // 마우스 호버링을 감지하여 플레이어 인벤토리에 현재 호버링 중인 슬롯 정보 제공
// public class NPCInventory : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
// {
//     // [SerializeField] Kinnly.PlayerInventory playerInventory; // 연결된 플레이어 인벤토리 참조
//     [SerializeField] Npc npc; // 연결된 NPC 참조

//     [SerializeField] Image slotImage; // 슬롯 이미지 (선택 시 색상 변경용)
//     [SerializeField] GameObject inventoryItemPrefab; // 인벤토리 아이템 프리팹 (슬롯에 표시되는 아이템 UI)

//     private Item currentItem; // 현재 보유 중인 아이템
//     private GameObject currentItemObject; // 현재 슬롯에 표시된 아이템 오브젝트

//     Color32 defaultColor = new Color32(255, 255, 255, 255);
//     Color32 selectedColor = new Color32(255, 161, 161, 128);

//     void Start()
//     {
//         if (slotImage == null)
//         {
//             slotImage = GetComponent<Image>();
//         }

//         if (playerInventory == null)
//         {
//             playerInventory = FindObjectOfType<PlayerInventory>();
//             if (playerInventory == null)
//             {
//                 Debug.LogError("NPC Inventory: PlayerInventory를 찾을 수 없습니다! 씬에 PlayerInventory가 존재하는지 확인하세요.");
//             }
//             else
//             {
//                 Debug.Log("NPC Inventory: PlayerInventory가 성공적으로 초기화되었습니다.");
//             }
//         }

//         if (npc == null)
//         {
//             npc = GetComponentInParent<Npc>();
//             if (npc == null)
//             {
//                 Debug.LogError("NPC Inventory: Npc 컴포넌트를 찾을 수 없습니다! NPC가 올바르게 설정되었는지 확인하세요.");
//             }
//         }
//     }

//     public void AddItem(Item item)
//     {
//         if (item != null)
//         {
//             // currentItem이 null이 아닌 경우에만 RemoveItem 호출
//             if (currentItem != null)
//             {
//                 RemoveItem();
//             }
//             else
//             {
//                 // currentItem이 null인 경우 직접 NPC 작업 초기화
//                 if (npc != null)
//                 {
//                     npc.SetTask(Npc.NpcTask.None);
//                 }
//             }

//             currentItem = item;

//             Debug.Log($"NPC 인벤토리에 {item.name} 아이템이 추가되었습니다.");

//             SpawnInventoryItem(item, 1);
//             SetNpcTaskBasedOnItem(item);
//         }
//         else
//         {
//             Debug.LogError("NPC Inventory: AddItem에 null 아이템이 전달되었습니다!");
//         }
//     }

//     private void SpawnInventoryItem(Item item, int amount)
//     {
//         if (inventoryItemPrefab == null)
//         {
//             Debug.LogError("NPC Inventory: 인벤토리 아이템 프리팹이 null입니다! 인스펙터에서 설정했는지 확인하세요.");
//             return;
//         }

//         if (currentItemObject != null)
//         {
//             Destroy(currentItemObject);
//         }

//         GameObject go = Instantiate(inventoryItemPrefab, transform);
//         InventoryItem inventoryItem = go.GetComponent<InventoryItem>();

//         if (inventoryItem != null)
//         {
//             inventoryItem.SetItem(item, amount);
//             inventoryItem.IsDragging = false;
//             inventoryItem.Amount = amount;
//             go.GetComponent<Image>().raycastTarget = false;
//             currentItemObject = go;

//             Debug.Log($"NPC 인벤토리에 {item.name} 아이템 UI가 생성되었습니다.");
//         }
//         else
//         {
//             Debug.LogError("NPC Inventory: 생성된 오브젝트에 InventoryItem 컴포넌트가 없습니다!");
//             Destroy(go);
//         }
//     }

//     private void SetNpcTaskBasedOnItem(Item item)
//     {
//         if (npc == null)
//         {
//             Debug.LogWarning("NPC Inventory: NPC 참조가 null이므로 작업을 설정할 수 없습니다.");
//             return;
//         }

//         if (item.isSword)
//         {
//             npc.SetTask(Npc.NpcTask.Combat);
//             Debug.Log($"NPC {npc.NpcName}이(가) 검을 받아 전투 작업으로 설정되었습니다.");
//         }
//         else if (item.isAxe)
//         {
//             npc.SetTask(Npc.NpcTask.Woodcutting);
//             Debug.Log($"NPC {npc.NpcName}이(가) 도끼를 받아 나무 채집 작업으로 설정되었습니다.");
//         }
//         else if (item.isPickaxe)
//         {
//             npc.SetTask(Npc.NpcTask.Mining);
//             Debug.Log($"NPC {npc.NpcName}이(가) 곡괭이를 받아 광석 채집 작업으로 설정되었습니다.");
//         }
//     }

//     public Item GetCurrentItem()
//     {
//         return currentItem;
//     }

//     public void RemoveItem()
//     {
//         if (currentItem == null)
//         {
//             Debug.LogError("RemoveItem: 현재 아이템이 null입니다.");
//         }
//         else
//         {
//             Debug.Log($"RemoveItem: {currentItem.name} 아이템이 제거됩니다.");
//         }

//         if (currentItemObject == null)
//         {
//             Debug.LogError("RemoveItem: 현재 아이템 오브젝트가 null입니다.");
//         }
//         else
//         {
//             Destroy(currentItemObject);
//             currentItemObject = null;
//         }

//         currentItem = null;

//         if (npc != null)
//         {
//             npc.SetTask(Npc.NpcTask.None);
//             Debug.Log($"NPC {npc.NpcName}의 작업이 초기화되었습니다.");
//         }
//     }

//     // 버튼 클릭 시 아이템을 플레이어 인벤토리로 이동
//     public void MoveItemToPlayerInventory()
//     {
//         if (currentItem == null)
//         {
//             Debug.LogError("현재 아이템이 없습니다. 이동할 수 없습니다.");
//             return;
//         }

//         if (playerInventory == null)
//         {
//             Debug.LogError("PlayerInventory가 null입니다. 인벤토리를 확인하세요.");
//             return;
//         }
//         npc.animator.SetTrigger("CancelMining");

//         string itemName = currentItem.name; // 아이템 이름 미리 저장
//         playerInventory.AddItem(currentItem, 1); // 플레이어 인벤토리에 아이템 추가
//         Debug.Log($"{itemName} 아이템이 플레이어 인벤토리로 이동했습니다.");
//         RemoveItem(); // NPC 인벤토리에서 아이템 제거
//     }

//     public void OnPointerEnter(PointerEventData eventData)
//     {
//         if (playerInventory != null)
//         {
//             playerInventory.CurrentlyHoveredInventorySlot = this.gameObject;
//         }
//     }

//     public void OnPointerExit(PointerEventData eventData)
//     {
//         if (playerInventory != null)
//         {
//             playerInventory.CurrentlyHoveredInventorySlot = null;
//         }
//     }
// }
