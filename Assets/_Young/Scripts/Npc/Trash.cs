using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Kinnly;

namespace RedstoneinventeGameStudio
{
    public class Trash : MonoBehaviour
    {
        [SerializeField] private Animator animator; // 쓰레기통 애니메이터
        [SerializeField] private float radius = 100f; // 쓰레기통 감지 반경
        private bool isHovering = false; // 쓰레기통 위에 있는지 여부

        private void Update()
        {
            if (ItemDraggingManager.Instance.draggingItem != null)
            {
                // 쓰레기통과의 거리 계산
                float distance = Vector2.Distance(Input.mousePosition, transform.position);
                
                if (distance <= radius)
                {
                    // 쓰레기통 위에 아이템이 있다면
                    if (!isHovering)
                    {
                        isHovering = true;
                        if (animator != null)
                        {
                            animator.SetBool("State", true);
                        }
                    }

                    // 왼쪽 마우스 버튼을 놓았을 때
                    if (Input.GetMouseButtonUp(0))
                    {
                        DeleteItem();
                    }
                }
                else
                {
                    // 쓰레기통에서 벗어났다면
                    if (isHovering)
                    {
                        isHovering = false;
                        if (animator != null)
                        {
                            animator.SetBool("State", false);
                        }
                    }
                }
            }
            else
            {
                // 드래그 중이 아닐 때
                if (isHovering)
                {
                    isHovering = false;
                    if (animator != null)
                    {
                        animator.SetBool("State", false);
                    }
                }
            }
        }
        private void DeleteItem()
        {
            if (ItemDraggingManager.Instance.draggingItem != null)
            {
                // 드래그 중인 아이템을 찾는다
                var draggingItem = ItemDraggingManager.Instance.draggingItem;
                if (draggingItem != null)
                {
                    // 드래그 중인 아이템을 인벤토리에서 제거
                    foreach (var inventory in InventoryUIManager.Instance.playerInventorySlots)
                    {
                        if (inventory.item == draggingItem)
                        {
                            // 인벤토리에서 아이템 제거
                            inventory.item = null;
                            inventory.isOccupied = false;
                            break;
                        }
                    }
                }

                // 드래그 해제
                ItemDraggingManager.Instance.ClearDragging();
            }

            // 애니메이션 상태 초기화
            if (animator != null)
            {
                animator.SetBool("State", false);
            }
        }
    }
}
