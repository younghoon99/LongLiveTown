using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RedstoneinventeGameStudio;

namespace RedstoneinventeGameStudio
{
public class InventoryUIManager : MonoBehaviour
{
    public static InventoryUIManager Instance;
    public int SelectedSlotIndex = 0; // 현재 선택된 슬롯 인덱스

    [SerializeField] public List<Inventory> playerInventorySlots = new List<Inventory>();
    [SerializeField] private int maxSlots = 5; // 최대 슬롯 수


        [Header("플레이어 장착 아이템")]
        [SerializeField] public GameObject sword;
        [SerializeField] public GameObject axe;
        [SerializeField] public GameObject pickaxe;
        [SerializeField] public GameObject hammer;
        [SerializeField] public GameObject bow;

        [SerializeField] private Kinnly.Item isSelectedItem; // 현재 선택된 아이템 (미사용중)

        private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // 슬롯 초기화
        if (playerInventorySlots.Count != maxSlots)
        {
            Debug.LogError("플레이어 인벤토리 슬롯 개수가 " + maxSlots + "개가 아닙니다!");
        }
    }

    private void Start()
    {
        // 게임 시작 시 1번 인벤토리 선택
        SelectSlot(0);
    }

    private void Update()
    {
        // 플레이어 인벤토리일 때만 키보드 입력 처리
        if (playerInventorySlots.Count == maxSlots)
        {
            // 숫자키 입력 처리 (1-5)
            for (int i = 0; i < maxSlots; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SelectSlot(i);
                    break;
                }
            }

            // 마우스 휠 입력 처리
            if (Input.GetAxis("Mouse ScrollWheel") != 0f)
            {
                if (Input.GetAxis("Mouse ScrollWheel") > 0f)
                {
                    SelectSlot((SelectedSlotIndex + 1) % maxSlots);
                }
                else
                {
                    SelectSlot((SelectedSlotIndex + maxSlots - 1) % maxSlots);
                }
            }
                // [추가] 현재 선택된 슬롯의 아이템 정보를 항상 플레이어에 전달
                if (SelectedSlotIndex >= 0 && SelectedSlotIndex < playerInventorySlots.Count)
                {
                    var selectedItem = playerInventorySlots[SelectedSlotIndex].item;
                    var player = GetComponentInParent<Player>();
                    if (player != null)
                    {
                        player.SetEquippedItem(selectedItem);
                    }
                }
            }
    }

    // 슬롯 선택
    public void SelectSlot(int index)
    {
        if (index >= 0 && index < maxSlots)
        {
            SelectedSlotIndex = index;

            // 선택된 슬롯의 UI 업데이트
            foreach (var slot in playerInventorySlots)
            {
                slot.GetComponent<Image>().color = Color.white; // 선택 해제
            }
            if (playerInventorySlots[index] != null)
            {
                playerInventorySlots[index].GetComponent<Image>().color = Color.yellow; // 선택 표시

                // 모든 장착 아이템 비활성화
                sword.SetActive(false);
                axe.SetActive(false);   
                pickaxe.SetActive(false);
                hammer.SetActive(false);
                bow.SetActive(false);
                
                // 선택된 슬롯의 아이템에 따라 해당 장착 아이템 활성화
                var selectedItem = playerInventorySlots[index].item;
                if (selectedItem != null)
                {
                    // 플레이어에 장착 아이템 정보 전달
                    var player = GetComponentInParent<Player>();
                    
                    if (player != null)
                        player.SetEquippedItem(selectedItem);
                    // ArrowShooter와 연동: 선택된 아이템이 활이면 true, 아니면 false
                    if (selectedItem.isBow)
                    {
                        bow.SetActive(true);
                        player.isAttack = false;

                        if (FindObjectOfType<WallPlacement>() != null)
                        {
                            FindObjectOfType<WallPlacement>().OnHammerDeselected();
                        }
                        ArrowShooter.isBowEquipped = true; // 활 장착 상태 전달
                    }
                    else
                    {
                        ArrowShooter.isBowEquipped = false;
                        if (selectedItem.isSword)
                        {
                            sword.SetActive(true);
                            player.isAttack = true;
                            if (FindObjectOfType<WallPlacement>() != null)
                            {
                                FindObjectOfType<WallPlacement>().OnHammerDeselected();
                            }
                        }
                        else if (selectedItem.isAxe)
                        {
                            axe.SetActive(true);
                            player.isAttack = true;
                            if (FindObjectOfType<WallPlacement>() != null)
                            {
                                FindObjectOfType<WallPlacement>().OnHammerDeselected();
                            }
                        }
                        else if (selectedItem.isPickaxe)
                        {
                            pickaxe.SetActive(true);
                            player.isAttack = true;
                            if (FindObjectOfType<WallPlacement>() != null)
                            {
                                FindObjectOfType<WallPlacement>().OnHammerDeselected();
                            }
                        }
                        else if (selectedItem.isHammer)
                        {
                            // 망치일 때만 패널 활성화
                            hammer.SetActive(true);
                            player.isAttack = true;
                            if (FindObjectOfType<WallPlacement>() != null)
                            {
                                Debug.Log("망치 선택됨 - 패널 활성화");
                                FindObjectOfType<WallPlacement>().OnHammerSelected();
                            }
                            ArrowShooter.isBowEquipped = false;
                        }
                        else
                        {
                            ArrowShooter.isBowEquipped = false;
                        }
                    }
                }
                else
                    {
                        var player = GetComponentInParent<Player>();
                        if (player != null)
                            player.SetEquippedItem(selectedItem);
                        // 아무것도 선택하지 않은 경우 활 장착 해제
                        ArrowShooter.isBowEquipped = false;
                    // 맨손일 때 패널 비활성화
                    if (FindObjectOfType<WallPlacement>() != null)
                    {
                        FindObjectOfType<WallPlacement>().OnHammerDeselected();
                    }
                    player.isAttack = true;
                }
            }
        }
    }

    // 마우스 클릭 이벤트 처리
    public void HandleMouseClick(Inventory clickedSlot)
    {
        // Shift + 클릭 처리
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            if (clickedSlot.item != null)
            {
                if (clickedSlot.isPlayerInventory)
                {
                    clickedSlot.TryMoveItemToNearbyNpcInventory(); // 플레이어 → NPC
                    isSelectedItem = null;
                }
                else
                {
                    clickedSlot.TryMoveItemToPlayerInventory(); // NPC → 플레이어
                }
            }
        }
    }

    public List<Inventory> GetPlayerInventorySlots()
    {
        return playerInventorySlots;
    }

    public Inventory GetSelectedSlot()
    {
        if (SelectedSlotIndex >= 0 && SelectedSlotIndex < maxSlots &&
            SelectedSlotIndex < playerInventorySlots.Count)
        {
            return playerInventorySlots[SelectedSlotIndex];
        }
        return null;
    }
}
}
