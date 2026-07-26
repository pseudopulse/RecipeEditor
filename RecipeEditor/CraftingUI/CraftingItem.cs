using System;
using RoR2.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RecipeEditor {
    public class CraftingItem : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        public Vector2 startPosition;
        public Transform originalParent;
        public Image image;
        public string itemName;
        public string visibleName;
        public CraftingSlot ourSlot;
        public bool resultSlot = false;
        public CraftingHandler handler;
        public RectTransform rect;
        public GameObject drag = null;
        public bool clone = false;
        public void Start() {
            startPosition = transform.position;
            image = GetComponent<Image>();
            originalParent = transform.parent;
            rect = GetComponent<RectTransform>();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (drag) {
                drag.transform.position = eventData.position + CraftingHandler.instance.offset;
            }
        }

        public void FixedUpdate() {
            
        }

        public static void UpdateTooltip(GameObject target, string name, string desc, ColorCatalog.ColorIndex color) {
            TooltipProvider tp = target.GetComponent<TooltipProvider>();

            if (!tp) {
                tp = target.AddComponent<TooltipProvider>();
            }

            TooltipContent c = new();
            c.bodyColor = ColorCatalog.GetColor(color);
            c.titleColor = ColorCatalog.GetColor(color);
            c.overrideTitleText = Language.GetString(name);
            c.bodyToken = desc;
            c.titleToken = "gdfgdfgdfghgh";
            tp.SetContent(c);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (this.clone) {
                GameObject.Destroy(this.gameObject);
                return;
            }
            
            drag = GameObject.Instantiate(this.gameObject, CraftingHandler.instance.transform);
            drag.transform.Find("BaseOutline").gameObject.SetActive(false);
            drag.transform.Find("HoverOutline").gameObject.SetActive(false);
            foreach (Image image in drag.GetComponentsInChildren<Image>()) {
                image.raycastTarget = false;
            }
            drag.GetComponent<CraftingItem>().clone = true;
            drag.GetComponent<Image>().raycastTarget = false;
            this.GetComponent<Image>().raycastTarget = false;
            if (ourSlot) {
                ourSlot.storedItem = null;
                ourSlot = null;
            }
            
            drag.transform.position = this.transform.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            this.GetComponent<Image>().raycastTarget = true;
            if (drag) {
                drag.GetComponent<Image>().raycastTarget = true;
            }
            if (!ourSlot && drag) {
                GameObject.Destroy(drag);
            }
        }
    }
}