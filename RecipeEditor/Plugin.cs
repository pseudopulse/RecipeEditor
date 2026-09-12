global using static RecipeEditor.Main;

using BepInEx;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Reflection;
using RoR2.UI;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets.ResourceLocators;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using R2API.Networking;
using MonoMod.Cil;
using System;
using RoR2.UI;
using UnityEngine.UI;
using System.Collections;
using System.Reflection;
using BepInEx;
using RoR2.Hologram;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.PostProcessing;
using IL.RoR2.Navigation;
using HG;
using Newtonsoft.Json;
using RoR2.ContentManagement;
using static RoR2.ContentManagement.ContentManager;
using TMPro;

namespace RecipeEditor {
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Main : BaseUnityPlugin {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "pseudopulse";
        public const string PluginName = "RecipeEditor";
        public const string PluginVersion = "1.0.0";
        public static GameObject CraftingUI;
        public static GameObject ButtonPrefab;

        public static BepInEx.Logging.ManualLogSource ModLogger;
        public static RecipePack pack;
        public static bool showButton;
        public static bool highlightItemsWithOutputs;
        public static GameObject SearchBarPrefab;

        public static List<string[]> allRecipes = new();
        public void Awake() {
            // set logger
            ModLogger = Logger;

            pack = new();
            pack.Initialize();

            CraftingUI = Paths.GameObject.ScrapperPickerPanel.InstantiateClone("CraftingPanel");

            CraftingUI.RemoveComponent<PickupPickerPanel>();

            showButton = Config.Bind<bool>("Configuration", "Enable Editor Button", true, "Enables the recipe editor button appearing in the Logbook. Disable if you are done editing and only want recipes to be loaded instead.").Value;
            highlightItemsWithOutputs = Config.Bind<bool>("Configuration", "Highlight Crafting Results", false, "Highlights items in green if there is already a recipe that outputs that item.").Value;

            //
            Transform MainPanel = CraftingUI.transform.Find("MainPanel");
            Transform Juice = MainPanel.Find("Juice");
            Transform Icons = Juice.Find("IconContainer");
            Transform Template = Juice.Find("PickupButtonTemplate");
            LanguageTextMeshController Text = Juice.Find("Label").GetComponent<LanguageTextMeshController>();
            //

            CraftingHandler handler = CraftingUI.AddComponent<CraftingHandler>();
            Text.token = "Recipe Builder";

            // GameObject uiInstance = GameObject.Instantiate(Content.MULT.CraftingTable.CraftingUI, HUD.instancesList[0].mainContainer.transform);

            SceneManager.activeSceneChanged += OnSceneChanged;

            string path = BepInEx.Paths.ConfigPath + "/saved_recipes.json";
            string backupPath = Assembly.GetExecutingAssembly().Location.Replace("RecipeEditor.dll", "saved_recipes.json");

            if (File.Exists(backupPath) && !File.Exists(path)) {
                File.Move(backupPath, path);
            }

            var nullItem = Paths.ItemDef.AACannon;

            if (File.Exists(path)) {
                try {
                    RecipeHolder holder = JsonConvert.DeserializeObject<RecipeHolder>(File.ReadAllText(path));
                    allRecipes = holder.recipes.ToList();

                    for (int i = 0; i < allRecipes.Count; i++) {
                        CraftableDef def = ScriptableObject.CreateInstance<CraftableDef>();
                        def.pickup = nullItem;
                        def.recipes = new Recipe[0];
                        RecipePack.craftableDefs.Add(def);
                    }
                }
                catch (Exception err) {
                    Debug.LogError("caught exc");
                    Debug.LogError(err.ToString());
                }
            }

            PickupCatalog.availability.CallWhenAvailable(() => {
                int i = 0;
                List<string[]> toRemove = new();
                foreach (string[] recipe in allRecipes) {
                    var p1 = recipe[0];
                    var p2 = recipe[1];
                    var o = recipe[2];
                    int count = int.Parse(recipe[3]);

                    var p1_iDef = ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex(p1));
                    var p1_eDef = EquipmentCatalog.GetEquipmentDef(EquipmentCatalog.FindEquipmentIndex(p1));
                    if (!p1_iDef && !p1_eDef) {
                        i++;
                        toRemove.Add(recipe);
                        continue;
                    }

                    var p2_iDef = ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex(p2));
                    var p2_eDef = EquipmentCatalog.GetEquipmentDef(EquipmentCatalog.FindEquipmentIndex(p2));
                    if (!p2_iDef && !p2_eDef) {
                        i++;
                        toRemove.Add(recipe);
                        continue;
                    }

                    var o_iDef = ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex(o));
                    var o_eDef = EquipmentCatalog.GetEquipmentDef(EquipmentCatalog.FindEquipmentIndex(o));
                    if (!o_iDef && !o_eDef) {
                        i++;
                        toRemove.Add(recipe);
                        continue;
                    }

                    CraftableDef cd = RecipePack.craftableDefs[i];
                    cd.pickup = o_iDef ? o_iDef : o_eDef;
                    cd.recipes = new Recipe[1];
                    Recipe rec = new Recipe();
                    rec.amountToDrop = count;
                    rec.ingredients = new RecipeIngredient[] {
                        new RecipeIngredient() {
                            pickup = p1_iDef ? p1_iDef : p1_eDef
                        },
                        new RecipeIngredient() {
                            pickup = p2_iDef ? p2_iDef : p2_eDef
                        },
                    };
                    cd.recipes[0] = rec;
                    i++;
                }

                foreach (var recipe in toRemove) {
                    allRecipes.Remove(recipe);
                }
            });

            On.RoR2.UI.MainMenu.MainMenuController.Start += (orig, self) => {
                orig(self);

                if (SearchBarPrefab) {
                    return;
                }
                
                GameObject baseObj = GameObject.Find("MainMenu").transform.Find("MENU:  Signup").Find("Signup Menu").Find("PopupPanel").Find("SubmitPanel").Find("ContentArea").Find("Panel").Find("EmailInputField").gameObject;

                SearchBarPrefab = PrefabAPI.InstantiateClone(baseObj, "RESearchBar");
                SearchBarPrefab.GetComponent<Image>().color = Color.black;
                SearchBarPrefab.transform.Find("Text Area").Find("Placeholder").gameObject.SetActive(false);
                SearchBarPrefab.transform.Find("Text Area").Find("Text").GetComponent<HGTextMeshProUGUI>().color = Color.white;
            };
        }

        public class RecipePack : IContentPackProvider
        {
            internal ContentPack contentPack = new ContentPack();
            public static List<CraftableDef> craftableDefs = new List<CraftableDef>();
            public string identifier => "RE.CustomRecipes";

            public void Initialize()
            {
                ContentManager.collectContentPackProviders += ContentManager_collectContentpackProviders;
            }

            private void ContentManager_collectContentpackProviders(AddContentPackProviderDelegate addContentPackProvider)
            {
                addContentPackProvider(this);
            }

            public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
            {
                contentPack.identifier = identifier;
                contentPack.craftableDefs.Add(craftableDefs.ToArray());
                args.ReportProgress(1f);
                yield break;
            }

            public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
            {
                ContentPack.Copy(contentPack, args.output);
                args.ReportProgress(1f);
                yield break;
            }

            public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
            {
                args.ReportProgress(1f);
                yield break;
            }
        }

        private void OnSceneChanged(Scene arg0, Scene arg1)
        {
            if (SceneManager.GetActiveScene().name == "logbook") {
                this.StartCoroutine(SetupLogbookUI());          
            }
        }

        public IEnumerator SetupLogbookUI() {
            yield return new WaitForSeconds(0.2f);
            
            var root = GameObject.Find("LogbookMenu").transform.Find("LogbookPanel").Find("NavigationPanel (JUICED)").Find("CategoryHeader").Find("CategoriesList");
            var template = root.transform.Find("LogbookCategoryButton(Clone)").gameObject;
            
            var button = GameObject.Instantiate(template, GameObject.Find("LogbookMenu").transform);
            button.transform.Find("PageNumberArea").gameObject.SetActive(false);
            if (button.transform.Find("NewViewableTag(Clone)")) {
                button.transform.Find("NewViewableTag(Clone)").gameObject.SetActive(false);
            }
            button.GetComponent<ViewableTag>().enabled = false;
            if (!ButtonPrefab) {
                ButtonPrefab = PrefabAPI.InstantiateClone(button, "REButton", false);
            }
            button.GetComponentInChildren<HGTextMeshProUGUI>().text = "Edit Crafting Recipes";

            var hgb = button.GetComponent<HGButton>();
            hgb.onClick.RemoveAllListeners();
            hgb.onClick.AddListener(() => {
                GameObject uiInstance = GameObject.Instantiate(CraftingUI, GameObject.Find("LogbookMenu").transform);
            });

            if (!showButton) {
                button.gameObject.SetActive(false);
            }
        }

        public static void SaveRecipes() {
            string path = Assembly.GetExecutingAssembly().Location.Replace("RecipeEditor.dll", "saved_recipes.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(new RecipeHolder() { recipes = allRecipes.ToArray() }, Formatting.Indented));
        }
    }

    public class RecipeHolder {
        public string[][] recipes;
    }
}