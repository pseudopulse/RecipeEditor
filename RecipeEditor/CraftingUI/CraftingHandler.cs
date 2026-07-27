using System;
using System.Collections;
using System.Linq;
using RoR2.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

namespace RecipeEditor {
    public class CraftingHandler : MonoBehaviour {
        public CraftingItem OutputSlot;
        public CraftingSlot[] CraftingSlots;
        public Button.ButtonClickedEvent onExit = new();
        public static CraftingHandler instance;
        private bool hasCraftedAlready = false;
        public Vector2 offset = new Vector2(-20, -10);
        public GameObject tabSwitchButton;
        public GameObject saveRecipeButton;
        public int tab = 0;
        private GameObject crafter;
        private GameObject recipeViewer;
        private Sprite deleteSprite;
        private List<CraftingItem> allIcons = new();
        private TMP_InputField field;
        private Sprite nullSprite;
        public void Start() {
            StartCoroutine(FixUI());
            instance = this;

            var tex = Paths.Texture2D.texCrosshairUnavailable;
            deleteSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0f, 0f));

            nullSprite = Paths.EquipmentDef.SoulCorruptor.pickupIconSprite;
        }
        public IEnumerator FixUI() {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            Transform MainPanel = this.transform.Find("MainPanel");
            Transform Juice = MainPanel.Find("Juice").Find("ScrapperDetailsVertical").Find("Scroll View").Find("Viewport");
            Transform Icons = Juice.Find("IconContainer");
            Transform Template = Icons.Find("PickupButtonTemplate");

            CraftingSlots = new CraftingSlot[3];

            // Crafting Grid
            crafter = GameObject.Instantiate(Icons.gameObject, MainPanel.Find("Juice"));
            crafter.transform.localPosition = new Vector3(300f, 50f, 0f);
            crafter.GetComponent<GridLayoutGroup>().enabled = false;

            Icons.GetComponent<RectTransform>().localPosition = new(0, -160, 0);
            crafter.GetComponent<RectTransform>().localPosition = new(300, 200, 0);

            for (int i = 0; i < 3; i++) {
                GameObject slot = GameObject.Instantiate(Template.gameObject, crafter.transform);
                slot.transform.Find("Icon").gameObject.SetActive(false);
                slot.gameObject.SetActive(true);
                CraftingSlot cSlot = slot.AddComponent<CraftingSlot>();
                CraftingSlots[i] = cSlot;
                slot.transform.Find("Quantity").gameObject.SetActive(false);
            }
            
            var plus = CraftingSlots[0].transform.Find("Quantity");
            plus.parent = crafter.transform;
            plus.gameObject.SetActive(true);
            plus.GetComponent<TextMeshProUGUI>().text = "+";
            plus.transform.localPosition = new Vector3(-153f, -230f, 0f);

            var eq = CraftingSlots[1].transform.Find("Quantity");
            eq.parent = crafter.transform;
            eq.gameObject.SetActive(true);
            eq.GetComponent<TextMeshProUGUI>().text = "=";
            eq.transform.localPosition = new Vector3(10f, -230f, 0f);

            CraftingSlots[0].transform.localPosition = new Vector3(-230f, -44f, 0f);
            CraftingSlots[1].transform.localPosition = new Vector3(-100f, -44f, 0f);
            CraftingSlots[2].transform.localPosition = new Vector3(100f, -44f, 0f);
            CraftingSlots[2].transform.Find("Quantity").gameObject.SetActive(true);
            CraftingSlots[2].GetComponent<CraftingSlot>().ChangeQuantity(1);

            GetComponent<ScrapperInfoPanelHelper>().enabled = false;
            GetComponent<MPButton>().enabled = false;
            GetComponent<HGGamepadInputEvent>().enabled = false;

            tabSwitchButton = GameObject.Instantiate(Main.ButtonPrefab, MainPanel.Find("Juice"));
            HGButton hgb = tabSwitchButton.GetComponent<HGButton>();
            hgb.GetComponentInChildren<HGTextMeshProUGUI>().text = "View Recipes";
            hgb.onClick.RemoveAllListeners();
            hgb.onClick.AddListener(TabSwitch);
            tabSwitchButton.transform.localPosition = new Vector3(560f, -340f, 0f);

            saveRecipeButton = GameObject.Instantiate(Main.ButtonPrefab, MainPanel.Find("Juice"));
            hgb = saveRecipeButton.GetComponent<HGButton>();
            hgb.GetComponentInChildren<HGTextMeshProUGUI>().text = "Save Recipe";
            hgb.onClick.RemoveAllListeners();
            hgb.onClick.AddListener(SaveRecipe);
            saveRecipeButton.transform.localPosition = new Vector3(300f, -60f, 0f);


            Setup();

            recipeViewer = GameObject.Instantiate(MainPanel.Find("Juice").Find("ScrapperDetailsVertical").gameObject, MainPanel.Find("Juice"));
            recipeViewer.SetActive(false);
            recipeViewer.transform.Find("RESearchBar(Clone)").gameObject.SetActive(false);
        }

        public void TabSwitch() {
            Transform MainPanel = this.transform.Find("MainPanel");
            Transform Juice = MainPanel.Find("Juice").Find("ScrapperDetailsVertical").Find("Scroll View").Find("Viewport");
            Transform Icons = Juice.Find("IconContainer");

            if (tab == 0) {
                tab = 1;
                tabSwitchButton.GetComponent<HGButton>().GetComponentInChildren<HGTextMeshProUGUI>().text = "View Editor";
                
                crafter.SetActive(false);
                MainPanel.Find("Juice").Find("ScrapperDetailsVertical").gameObject.SetActive(false);
                recipeViewer.SetActive(true);
                saveRecipeButton.SetActive(false);

                var icons = recipeViewer.transform.Find("Scroll View").Find("Viewport").Find("IconContainer");
                while (icons.childCount > 0) {
                    DestroyImmediate(icons.GetChild(0).gameObject);
                }

                var glg = icons.GetComponent<GridLayoutGroup>();
                glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                glg.constraintCount = 1;

                recipeViewer.transform.Find("Scroll View").GetComponent<ScrollRect>().verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
                recipeViewer.transform.Find("Scroll View").Find("Scrollbar Vertical").transform.localPosition = new Vector3(0f, 330f, 0f);

                if (CraftingSlots[0].storedItem) GameObject.Destroy(CraftingSlots[0].storedItem.gameObject);
                if (CraftingSlots[1].storedItem) GameObject.Destroy(CraftingSlots[1].storedItem.gameObject);
                if (CraftingSlots[2].storedItem) GameObject.Destroy(CraftingSlots[2].storedItem.gameObject);

                foreach (var recipe in Main.allRecipes) {
                    var p1 = recipe[0];
                    var p2 = recipe[1];
                    var o = recipe[2];
                    var c = int.Parse(recipe[3]);

                    var p1_iDef = ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex(p1));
                    var p1_eDef = EquipmentCatalog.GetEquipmentDef(EquipmentCatalog.FindEquipmentIndex(p1));

                    var p2_iDef = ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex(p2));
                    var p2_eDef = EquipmentCatalog.GetEquipmentDef(EquipmentCatalog.FindEquipmentIndex(p2));

                    var o_iDef = ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex(o));
                    var o_eDef = EquipmentCatalog.GetEquipmentDef(EquipmentCatalog.FindEquipmentIndex(o));

                    var crafter = GameObject.Instantiate(this.crafter, icons);
                    crafter.SetActive(true);
                    crafter.transform.GetChild(1).Find("Icon").gameObject.SetActive(true);
                    crafter.transform.GetChild(2).Find("Icon").gameObject.SetActive(true);
                    crafter.transform.GetChild(3).Find("Icon").gameObject.SetActive(true);
                    crafter.transform.GetChild(1).Find("Icon").GetComponent<Image>().sprite = p1_iDef ? p1_iDef.pickupIconSprite : p1_eDef.pickupIconSprite;
                    crafter.transform.GetChild(2).Find("Icon").GetComponent<Image>().sprite = p2_iDef ? p2_iDef.pickupIconSprite : p2_eDef.pickupIconSprite;
                    crafter.transform.GetChild(3).Find("Icon").GetComponent<Image>().sprite = o_iDef ? o_iDef.pickupIconSprite : o_eDef.pickupIconSprite;
                    crafter.transform.GetChild(3).Find("Quantity").GetComponent<TextMeshProUGUI>().text = c.ToString();
                    crafter.transform.GetChild(4).localPosition = new Vector3(-80f, -230f, 0f);
                    crafter.transform.GetChild(5).localPosition = new Vector3(85f, -230f, 0f);

                    var del = GameObject.Instantiate(crafter.transform.GetChild(1), crafter.transform);
                    var clear = del.AddComponent<RecipeClearButton>();
                    clear.holder = crafter;
                    clear.recipe = recipe;
                    del.GetComponent<HGButton>().onClick.AddListener(clear.OnClick);
                    del.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                    del.transform.localPosition = new Vector3(450f, -148f, 0f);
                    var img = del.transform.Find("Icon").GetComponent<Image>();
                    img.sprite = deleteSprite;
                    img.color = new Color32(255, 31, 0, 255);

                    CraftingItem.UpdateTooltip(crafter.transform.GetChild(1).gameObject, p1_iDef ? p1_iDef.nameToken : p1_eDef.nameToken, p1_iDef ? p1_iDef.descriptionToken : p1_eDef.descriptionToken, p1_iDef ? p1_iDef.colorIndex : p1_eDef.colorIndex);
                    CraftingItem.UpdateTooltip(crafter.transform.GetChild(2).gameObject, p2_iDef ? p2_iDef.nameToken : p2_eDef.nameToken, p2_iDef ? p2_iDef.descriptionToken : p2_eDef.descriptionToken, p2_iDef ? p2_iDef.colorIndex : p2_eDef.colorIndex);
                    CraftingItem.UpdateTooltip(crafter.transform.GetChild(3).gameObject, o_iDef ? o_iDef.nameToken : o_eDef.nameToken, o_iDef ? o_iDef.descriptionToken : o_eDef.descriptionToken, o_iDef ? o_iDef.colorIndex : o_eDef.colorIndex);
                }
            }
            else {
                tab = 0;
                tabSwitchButton.GetComponent<HGButton>().GetComponentInChildren<HGTextMeshProUGUI>().text = "View Recipes";

                crafter.SetActive(true);
                MainPanel.Find("Juice").Find("ScrapperDetailsVertical").gameObject.SetActive(true);
                recipeViewer.SetActive(false);
                saveRecipeButton.SetActive(true);
            }
        }

        public class RecipeClearButton : MonoBehaviour {
            public string[] recipe;
            public GameObject holder;
            public void Update() {
                transform.localPosition = new Vector3(450f, -148f, 0f);
            }
            public void OnClick() {
                Main.allRecipes.Remove(recipe);
                Main.SaveRecipes();
                GameObject.Destroy(holder);
            }
        }

        public void UpdateAllIcons() {
            if (!Main.highlightItemsWithOutputs) {
                return;
            }

            foreach (CraftingItem item in allIcons) {
                if (GetItemHasRecipe(item.itemName)) {
                    item.transform.Find("BaseOutline").GetComponent<Image>().color = Color.green;
                }
                else {
                    item.transform.Find("BaseOutline").GetComponent<Image>().color = Color.white;
                }
            }
        }

        public static bool GetItemHasRecipe(string name) {
            var iDef = ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex(name));
            var eDef = EquipmentCatalog.GetEquipmentDef(EquipmentCatalog.FindEquipmentIndex(name));
            UnityEngine.Object def = iDef ? iDef : eDef;

            foreach (CraftableDef cd in CraftableCatalog.craftableDefs) {
                if (cd.pickup == def) {
                    return true;
                }
            }

            foreach (string[] recipe in allRecipes) {
                if (recipe[2] == name) {
                    return true;
                }
            }

            return false;
        }

        public void SaveRecipe() {
            if (!CraftingSlots[0].storedItem || !CraftingSlots[1].storedItem || !CraftingSlots[2].storedItem) {
                return;
            }

            string[] recipe = new string[4] { CraftingSlots[0].storedItem.itemName, CraftingSlots[1].storedItem.itemName, CraftingSlots[2].storedItem.itemName, CraftingSlots[2].quantity.ToString()};

            Main.allRecipes.Add(recipe);

            GameObject.Destroy(CraftingSlots[0].storedItem.gameObject);
            GameObject.Destroy(CraftingSlots[1].storedItem.gameObject);
            GameObject.Destroy(CraftingSlots[2].storedItem.gameObject);

            CraftingSlots[2].GetComponent<CraftingSlot>().ChangeQuantity(1);

            UpdateAllIcons();

            Main.SaveRecipes();
        }

        public void UpdateFilter(string val) {
            if (!string.IsNullOrEmpty(val)) {
                foreach (CraftingItem item in allIcons) {
                    if (!item.visibleName.ToLower().Contains(val)) {
                        item.gameObject.SetActive(false);
                    }
                }
            }
            else {
                foreach (CraftingItem item in allIcons) {
                    item.gameObject.SetActive(true);
                }
            }
        }

        public void Setup() {
            Transform MainPanel = this.transform.Find("MainPanel");
            Transform Juice = MainPanel.Find("Juice").Find("ScrapperDetailsVertical").Find("Scroll View").Find("Viewport");
            Transform Icons = Juice.Find("IconContainer");
            Transform Template = Icons.Find("PickupButtonTemplate");

            var glg = Icons.GetComponent<GridLayoutGroup>();
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 7;

            MainPanel.Find("Juice").Find("ScrapperDetailsVertical").Find("Scroll View").Find("Scrollbar Vertical").transform.localPosition = new Vector3(300f, 330f, 0f);
            MainPanel.Find("Juice").Find("ScrapperDetailsVertical").transform.localPosition = new Vector3(-360f, -30f, 0f);

            var bar = GameObject.Instantiate(Main.SearchBarPrefab, MainPanel.Find("Juice").Find("ScrapperDetailsVertical"));
            bar.GetComponent<LayoutElement>().preferredHeight = 40f;
            field = bar.GetComponent<TMP_InputField>();
            field.textComponent = bar.transform.Find("Text Area").Find("Text").GetComponent<HGTextMeshProUGUI>();
            field.placeholder = bar.transform.Find("Text Area").Find("Placeholder").GetComponent<HGTextMeshProUGUI>();
            field.placeholder.gameObject.SetActive(true);
            field.placeholder.GetComponent<HGTextMeshProUGUI>().text = "Search...";
            field.text = "";
            field.onFocusSelectAll = false;
            field.isRichTextEditingAllowed = true;
            field.richText = true;
            field.enabled = false;
            field.enabled = true;
            field.onValueChanged.AddListener(UpdateFilter);

            foreach (ItemDef def in ItemCatalog.allItemDefs.OrderBy(x => TierSort(x.tier)).ToArray()) {
                if (def.hidden || def.tier == ItemTier.NoTier) continue;
                GameObject itemButton = GameObject.Instantiate(Template.gameObject, Icons);
                itemButton.gameObject.SetActive(true);
                itemButton.transform.Find("Icon").GetComponent<Image>().sprite = def.pickupIconSprite;
                itemButton.transform.Find("Quantity").gameObject.SetActive(false);
                    
                CraftingItem crafting = itemButton.AddComponent<CraftingItem>();
                crafting.itemName = def.name;
                crafting.image = itemButton.transform.Find("Icon").GetComponent<Image>();
                crafting.handler = this;
                crafting.visibleName = Language.GetString(def.nameToken);
                allIcons.Add(crafting);

                CraftingItem.UpdateTooltip(itemButton, def.nameToken, def.descriptionToken, def.colorIndex);
            }

            foreach (EquipmentIndex index in EquipmentCatalog.allEquipment.OrderBy(x => EquipmentCatalog.GetEquipmentDef(x).isLunar ? 1 : 0).ToArray()) {
                EquipmentDef def = EquipmentCatalog.GetEquipmentDef(index);
                if (!def.canDrop) {
                    var str = Language.GetString(def.nameToken);
                    var str2 = Language.GetString(def.descriptionToken);
                    if (str.Contains("_") || str2.Contains("_")) {
                        continue;
                    }

                    if (def.pickupIconSprite == nullSprite) {
                        continue;
                    }
                }

                GameObject itemButton = GameObject.Instantiate(Template.gameObject, Icons);
                itemButton.gameObject.SetActive(true);
                itemButton.transform.Find("Icon").GetComponent<Image>().sprite = def.pickupIconSprite;
                itemButton.transform.Find("Quantity").gameObject.SetActive(false);
                    
                CraftingItem crafting = itemButton.AddComponent<CraftingItem>();
                crafting.itemName = def.name;
                crafting.image = itemButton.transform.Find("Icon").GetComponent<Image>();
                crafting.visibleName = Language.GetString(def.nameToken);
                crafting.handler = this;
                allIcons.Add(crafting);

                CraftingItem.UpdateTooltip(itemButton, def.nameToken, def.descriptionToken, def.colorIndex);
            }

            UpdateAllIcons();

            int TierSort(ItemTier tier) {
                switch (tier) {
                    case ItemTier.Tier1:
                        return 0;
                    case ItemTier.Tier2:
                        return 1;
                    case ItemTier.Tier3:
                        return 2;
                    case ItemTier.Lunar:
                        return 3; 
                    case ItemTier.Boss:
                        return 4;
                    case ItemTier.FoodTier:
                        return 5;
                    case ItemTier.VoidTier1:
                        return 6;
                    case ItemTier.VoidTier2:
                        return 7;
                    case ItemTier.VoidTier3:
                        return 8;
                    default:
                        return 9;
                }
            }
        }
    }
}