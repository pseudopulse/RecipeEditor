using System;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RecipeEditor {
    public class CraftingSlot : MonoBehaviour, IDropHandler, IScrollHandler
    {
        public CraftingItem storedItem;
        public int quantity = 1;
        public bool isDisplaySlot = false;
        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag.transform) {
                CraftingItem item = eventData.pointerDrag.GetComponent<CraftingItem>().drag.GetComponent<CraftingItem>();
                if (item) {
                    eventData.pointerDrag.GetComponent<CraftingItem>().ourSlot = this;
                    storedItem = item;
                    item.transform.SetParent(this.transform);
                    item.transform.localPosition = Vector3.zero;
                }
            }
        }

        public void ChangeQuantity(int newQuantity) {
            quantity = newQuantity;
            transform.Find("Quantity").GetComponent<TextMeshProUGUI>().text = quantity.ToString();
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (isDisplaySlot) {
                return;
            }
            
            float scroll = eventData.scrollDelta.y;
            int count = quantity;

            if (scroll > 0)
            {
                count++;
                ChangeQuantity(count);
            }
            else if (scroll < 0)
            {
                count--;
                if (count < 1) {
                    count = 1;
                }

                if (count != quantity) {
                    ChangeQuantity(count);
                }
            }
        }
    }
}