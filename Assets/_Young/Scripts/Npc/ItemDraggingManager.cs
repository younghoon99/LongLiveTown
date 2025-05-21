using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Kinnly;

namespace RedstoneinventeGameStudio
{
    public class ItemDraggingManager : MonoBehaviour
    {
        public static ItemDraggingManager Instance;

        public Item draggingItem;
        public Image draggingImageUI; // 마우스를 따라다닐 이미지 UI


        private void Awake()
        {

            Instance = this;
            if (draggingImageUI == null)
            {

            }
            draggingImageUI.gameObject.SetActive(false);

        }

        // 임시로 조건을 완화해서 이미지 따라다니는지 확인
        private void Update()
        {
            if (draggingItem != null)
            {
                draggingImageUI.transform.position = Input.mousePosition;
            }
        }



        public void StartDragging(Item item, Sprite icon)
        {

            draggingItem = item;
            if (draggingImageUI != null)
            {
                draggingImageUI.rectTransform.localScale = new Vector3(0.4f, 0.4f, 1f); //드래그 중 아이템 크기
                draggingImageUI.sprite = icon;
                draggingImageUI.enabled = true;
                draggingImageUI.gameObject.SetActive(true);
            }
            else
            {

            }
        }

        public void ClearDragging()
        {

            draggingItem = null;
            if (draggingImageUI != null)
            {
                draggingImageUI.sprite = null;
                draggingImageUI.enabled = false;
                draggingImageUI.gameObject.SetActive(false);
            }

        }

        public bool IsDragging()
        {
            return draggingItem != null;
        }
    }
}
