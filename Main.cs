using BepInEx;
using BepInEx.IL2CPP;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using FGClient;
using FG.Common;
using FG.Common.Fraggle;
using FMODUnity;
using BepInEx.IL2CPP.Utils.Collections;
using FG.Common.CMS;
using System.Collections.Generic;
using ScriptableObjects;
using System.Net;
using System;
using Levels.Obstacles;
using TreeView;
using FMOD.Studio;
using FG.Common.Loadables;
using UnityEngine.UI;
using FraggleExpansion.Patches.Creative;
using FraggleExpansion.Patches.Reticle;
using FraggleExpansion.Patches;
using static UnityEngine.UI.GridLayoutGroup;
using FGClient.UI.Core;
using FGClient.UI;

namespace FraggleExpansion
{
    [BepInPlugin("FraggleExpansion", "Creative Expansion Pack", "2.2")]
    public class Main : BasePlugin
    {
        public Harmony _Harmony = new Harmony("com.simp.fraggleexpansion");
        public static Main Instance;
        public SlimeGamemodesManager _SlimeGamemodeManager;

        public override void Load()
        {
            Log.LogMessage("Creative Expansion Pack | RELEASE | HUNTER BUILD");
            Log.LogMessage("This mod is an extension Fall Guys Creative.");

            Instance = this;

            new PropertiesReader();
            _SlimeGamemodeManager = new SlimeGamemodesManager();

            // Requirement to Intialize Creative Expansion Pack
            _Harmony.PatchAll(typeof(Requirements));

            // Within Creative Patches
            _Harmony.PatchAll(typeof(MainFeaturePatches));
            _Harmony.PatchAll(typeof(FeaturesPatches));
            _Harmony.PatchAll(typeof(BypassesPatches));

            // UI Stuff
            _Harmony.PatchAll(typeof(ReticleUI));

            // Misc.
            _Harmony.PatchAll(typeof(MiscPatches));
        }


        public void OnSceneWasLoaded() 
        {
            if (SceneManager.GetActiveScene().name == "MainMenu")
            { 
                StartCouroutineIl2Cpp(OnMainMenu().WrapToIl2Cpp());
                _SlimeGamemodeManager.LoadGamemodes();
            }
            StartCouroutineIl2Cpp(FraggleExpansionReticle().WrapToIl2Cpp());
        }

        public void StartCouroutineIl2Cpp(Il2CppSystem.Collections.IEnumerator Enumerator)
       => ExplorerBehaver.Instance.StartCoroutine(Enumerator);

        public IEnumerator FraggleExpansionReticle()
        {
            yield return new WaitForFixedUpdate();
            
            if (ThemeManager.CurrentThemeData == null) yield break;
            if (ThemeManager.CurrentThemeData.BackgroundSceneName != SceneManager.GetActiveScene().name) yield break;

            while (!ThemeManager.CurrentThemeData.ObjectListRef.IsDone)
                yield return null;

            if(FraggleExpansionData.RemoveCostAndStock)
            AudioLevelEditorStateListener._instance.OnResourcesBarChanged(new BudgetResourcesBarChanged(1000));

            if (FraggleExpansionData.AddUnusedObjects)
            {
                if(ThemeManager.CurrentThemeData.ID == "THEME_RETRO")
                AddObjectToCurrentList("placeable_obstacle_spinningbeamshort_retro_large", LevelEditorPlaceableObject.Category.MovingSurfaces, 2, 838);
                AddObjectToCurrentList("placeable_special_goo_slide_large", LevelEditorPlaceableObject.Category.Platforms, 2, 261);
                
            }
            if (ThemeManager.CurrentThemeData.ID == "THEME_RETRO" && GameModeManager.CurrentGameModeData.ID == "GAMEMODE_SURVIVAL")
                AddObjectToCurrentList("placeable_rule_floorstart_survival_large", LevelEditorPlaceableObject.Category.Platforms, 2, 172);



            AddCMSStringKeys();
            ManageCostRotationStockForAllObjects(FraggleExpansionData.RemoveCostAndStock, FraggleExpansionData.RemoveRotation);
            ManagePlaceableExtras();
             
            while (!UnityEngine.Object.FindObjectOfType<LevelEditorManager>())
                yield return null;
            while (LevelEditorManager.Instance.MapPlacementBounds == null)
                yield return null;

            // Only works when a new round is created, but you can run this in a round load postfix like done here
            if(FraggleExpansionData.BypassBounds)
            LevelEditorManager.Instance.MapPlacementBounds = new Bounds(LevelEditorManager.Instance.MapPlacementBounds.center, new Vector3(100000, 100000, 100000));

            while (!UnityEngine.Object.FindObjectOfType<LevelEditorNavigationScreenViewModel>())
                yield return null;

            if(GameModeManager.CurrentGameModeData.ID != "GAMEMODE_GAUNTLET")
            UnityEngine.Object.FindObjectOfType<LevelEditorNavigationScreenViewModel>().SetCheckListVisible(false);
            yield break;
        }

        public void AddCMSStringKeys()
        {
            Dictionary<string, string> StringsToAdd = new Dictionary<string, string>()
            {
                {"wle_rulebook_noofwinners", "Number of Winners"}, 
                {"wle_checklist_spawnPoints", "Place a holographic Start Line"},
                {"wle_item_holographicstartname", "The Braindead Start Line"},
                {"wle_item_holographicstartdesc", "A holographic platform that defines where players are located at the Start of the Round!"},
                {"wle_creativeexpansion_stop", "STOP!"},
                {"wle_creativeexpansion_stop_description", "Creative Expansion Pack does make upcoming gamemodes work.\nHowever, they do not work in-game! Due to this issue publishing a level in the gamemode you're in has been disabled."},
                {"wle_creative_expansion_stop_confirm", "UNDERSTOOD..."},
            };

            foreach (var ToAdd in StringsToAdd) AddNewStringToCMS(ToAdd.Key, ToAdd.Value);
        }

        public void AddNewStringToCMS(string Key, string Value)
        {
            if (!CMSLoader.Instance._localisedStrings.ContainsString(Key))
                CMSLoader.Instance._localisedStrings._localisedStrings.Add(Key, Value);
        }

        public void ManagePlaceableExtras()
        {
            foreach (var Placeable in LevelEditorObjectList.CurrentObjects.Cast<Il2CppSystem.Collections.Generic.List<PlaceableObjectData>>())
            {
                var Prefab = Placeable.defaultVariant.Prefab;

                if (Prefab.GetComponent<LevelEditorDrawableData>())
                {
                    var Drawable = Prefab.GetComponent<LevelEditorDrawableData>();

                    if (Prefab.GetComponent<LevelEditorCheckpointFloorData>())
                    {
                        Drawable._painterMaxSize = new Vector3(30, 30, 30);
                        Drawable._canBePainterDrawn = true;
                        Drawable.FloorType = LevelEditorDrawableData.DrawableSemantic.FloorObject;
                        Drawable._restrictedDrawingAxis = LevelEditorDrawableData.DrawRestrictedAxis.Up;

                        UnityEngine.Object.Destroy(Prefab.GetComponent<LevelEditorFloorScaleParameter>());
                        Prefab.GetComponent<LevelEditorPlaceableObject>().hasParameterComponents = false;
                    }
                    if (FraggleExpansionData.InsanePainterSize)
                    {
                        Drawable._painterMaxSize = new Vector3(100000, 100000, 100000);
                    }
                }

                if (Prefab.GetComponent<LevelEditorDrawablePremadeWallSurface>())
                {
                    var DrawableWallSurface = Prefab.GetComponent<LevelEditorDrawablePremadeWallSurface>();
                    DrawableWallSurface._useBetaWalls = FraggleExpansionData.BetaWalls && ThemeManager.CurrentThemeData.ID != "THEME_RETRO";
                }

                if (Prefab.name == "POD_SpawnBasket_Vanilla")
                {
                    var ParameterComponent = Prefab.GetComponent<LevelEditorCarryTypeParameter>();
                    foreach (var CarryType in ParameterComponent._carryTypes)
                    {
                        CarryType.CarryPrefab.GetComponent<COMMON_SelfRespawner>()._respawnTriggerY = -120;
                    }
                }

                if (Placeable.name == "POD_Rule_Floor_Start_Survival")
                {
                    Placeable.objectNameKey = "wle_item_holographicstartname";
                    Placeable.objectDescriptionKey = "wle_item_holographicstartdesc";

                    Placeable.defaultVariant.Prefab.GetComponent<LevelEditorPlaceableObject>().ParameterTypes = LevelEditorParametersManager.LegacyParameterTypes.None;
                }

                if (Prefab.GetComponent<LevelEditorGenericBuoyancy>())
                    UnityEngine.Object.Destroy(Prefab.GetComponent<LevelEditorGenericBuoyancy>());
            }
        }

        public void ManageCostRotationStockForAllObjects(bool RemoveCostAndStock, bool RemoveRotation)
        {
            foreach (var Placeable in LevelEditorObjectList.CurrentObjects.Cast<Il2CppSystem.Collections.Generic.List<PlaceableObjectData>>())
            {
                RemoveCostAndRotationForObject(Placeable, RemoveCostAndStock, RemoveRotation);

                if (!RemoveCostAndStock) return;

                var DefaultVariantPrefab = Placeable.defaultVariant.Prefab;

                if (Placeable.name == "POD_Wheel_Maze_Revised")
                {
                    LevelEditorCostOverrideWheelMaze Comp = DefaultVariantPrefab.GetComponent<LevelEditorCostOverrideWheelMaze>();
                    Comp._chevronPatternModifier._costModifier = 0;
                    Comp._diamondPatternModifier._costModifier = 0;
                    Comp._hexagonPatternModifier._costModifier = 0;
                    Comp._hourglassPatternModifier._costModifier = 0;
                    Comp._rhomboidPatternModifier._costModifier = 0;
                    Comp._largeSizeModifier._costModifier = 0;
                    Comp._smallSizeModifier._costModifier = 0;
                    Comp._mediumSizeModifier._costModifier = 0;
                    Comp._trianglePatternModifier._costModifier = 0;
                }

                if (DefaultVariantPrefab.GetComponent<LevelEditorDrawablePremadeWallSurface>())
                {
                     var DrawableWallSurface = DefaultVariantPrefab.GetComponent<LevelEditorDrawablePremadeWallSurface>();
                     DrawableWallSurface._shouldAddToCost = false;
                     DrawableWallSurface._useStaticAddedWallCost = false;
                }

                if (Placeable.name == "POD_SpawnBasket_Vanilla")
                {
                    if (DefaultVariantPrefab.GetComponent<LevelEditorSpawnBasketCostOverride>())
                        UnityEngine.Object.Destroy(DefaultVariantPrefab.GetComponent<LevelEditorSpawnBasketCostOverride>());
                }
            }
        }    

        public void RemoveCostAndRotationForObject(PlaceableObjectData Owner, bool RemoveCost = true, bool RemoveRotation = true)
        {
            foreach (var Variant in Owner.objectVariants)
            {
                var TrueOwner = Variant.Prefab.GetComponent<LevelEditorPlaceableObject>().ObjectDataOwner; 
                var CostHandler = TrueOwner.GetCostHandler();
                var RotationHandler = TrueOwner.RotationHandler;
                if (CostHandler != null && RemoveCost)
                {
                    CostHandler._baseCost = 0;
                    CostHandler._additiveBaseCost = 0;
                    if (CostHandler.UseStock)
                        CostHandler._stockCountAllowed = 10000;
                }

                if (RotationHandler != null && RemoveRotation)
                {
                    RotationHandler.xAxisIsLocked = false;
                    RotationHandler.yAxisIsLocked = false;
                    RotationHandler.zAxisIsLocked = false;
                }
            }
        }

        public void AddObjectToCurrentList(string AssetRegistryName, LevelEditorPlaceableObject.Category Category = LevelEditorPlaceableObject.Category.Advanced, int DefaultVariantIndex = 0, int ID = 0)
        {
            try
            {
                AddressableLoadableAsset Loadable = AssetRegistry.Instance.LoadAsset(AssetRegistryName);
                PlaceableObjectData Owner = Loadable.Asset.Cast<PlaceableVariant_Base>().Owner;
                LevelEditorObjectList CurrentLevelEditorObjectList = ThemeManager.CurrentThemeData.ObjectList;
                var CurrentObjectList = LevelEditorObjectList.CurrentObjects.Cast<Il2CppSystem.Collections.Generic.List<PlaceableObjectData>>();
                if (Owner == null) return;
                if (CurrentObjectList.Contains(Owner) && HasCarouselDataForObject(Owner)) return;
                Owner.category = Category;
                VariantTreeElement VariantElement = new VariantTreeElement(Owner.name, 0, ID);
                Owner.defaultVariant = Owner.objectVariants[DefaultVariantIndex];
                VariantElement.Variant = Owner.objectVariants[DefaultVariantIndex];
                CurrentLevelEditorObjectList.CarouselItems.children.Add(VariantElement);
                CurrentLevelEditorObjectList.treeElements.Add(VariantElement);
                CurrentObjectList.Add(Owner);
            }
            catch { }
        }

        public bool HasCarouselDataForObject(PlaceableObjectData Data)
        {
            LevelEditorObjectList CurrentLevelEditorObjectList = ThemeManager.CurrentThemeData.ObjectList;
            foreach(var CarouselItem in CurrentLevelEditorObjectList.CarouselItems.children)
            {
                if (CarouselItem.Cast<VariantTreeElement>().Variant.Owner == Data)
                    return true;
            }

            return false;
        }

        public void OnUpdate()
        {
             if(Input.GetKeyDown(KeyCode.End))
             if(FraggleCommonManager.Instance.IsInLevelEditor)
             if(!LevelEditorManager.Instance.IsInLevelEditorState<LevelEditorStateMenus>() && !LevelEditorManager.Instance.IsInLevelEditorState<LevelEditorStateTest>() && !LevelEditorManager.Instance.IsInLevelEditorState<LevelEditorStateExplore>())
                 LevelEditorManager.Instance.ReplaceCurrentLevelEditorState(new LevelEditorStateMenus(LevelEditorManager.Instance, false, false).Cast<ILevelEditorState>());
        }

        #region Main Menu Management
        IEnumerator OnMainMenu()
        {
            StartCouroutineIl2Cpp(LoadBootSplash().WrapToIl2Cpp());

            while (!UnityEngine.Object.FindObjectOfType<MainMenuManager>()) yield return null;
            while(CMSLoader.Instance.State != CMSLoaderState.Ready) yield return null;
            while (!UnityEngine.Object.FindObjectOfType<MainMenuManager>().IsOnMainMenu) yield return null;

            try
            {
                if (!IL2CPPChainloader.Instance.Plugins.ContainsKey("Lithium"))
                {
                    GameObject.Find("Prime_UI_MainMenu_Canvas(Clone)").FindChild("SafeArea").SetActive(false);
                    GameObject.Find("SeasonPassButton").SetActive(false);
                    GameObject.Find("ShopButton").SetActive(false);

                    if (GameObject.Find("LiveEventButton"))
                        GameObject.Find("LiveEventButton").SetActive(false);
                }

                EnteredMainMenuPrompt();

                // Upcoming Gamemodes Strings
                AddNewStringToCMS("wle_creative_expansion_confirmation_upcoming_title", "Are you sure?");
                AddNewStringToCMS("wle_creative_expansion_confirmation_upcoming_desc", "The round type you selected only works within Creative Expansion Pack,\nThe way the mod interprets these gamemodes might not be 100% accurate.");
                AddNewStringToCMS("wle_creative_expansion_confirmation_upcoming_confirm", "Yes");
            }
            catch { }
        }

        public bool ShowMainMenuPrompt = true;
        void EnteredMainMenuPrompt()
        {

            #region CMS Strings Addition
            // Q&A CMS Strings
            AddNewStringToCMS("mainmenu_creativeexpansiontitle", "Hey there,");
            AddNewStringToCMS("mainmenu_creativeexpansiondesc", "Seems like you are using Creative Expansion Pack!\nPlease mind reading the Q&A on the github page of the mod before playing!\nClick the \"Q&A Page\" button to get open the Q&A page.");
            AddNewStringToCMS("mainmenu_creativeexpansionok", "Q&A Page");
            AddNewStringToCMS("mainmenu_creativeexpansionskip", "Skip");
            #endregion

            void OnClickedPopUp(bool Clicked)
            {
                if (Clicked)
                    Application.OpenURL("https://github.com/kota69th/CreativeExpansionPack#questions-and-answers");
            }

            Il2CppSystem.Action<bool> OnClickedPopUpAction = new System.Action<bool>(OnClickedPopUp);

            var ModalMessageDataDisclaimer = new ModalMessageData
            {
                Title = "mainmenu_creativeexpansiontitle",
                Message = "mainmenu_creativeexpansiondesc",
                ModalType = UIModalMessage.ModalType.MT_OK_CANCEL,
                OkButtonType = UIModalMessage.OKButtonType.Positive,
                OkTextOverrideId = "mainmenu_creativeexpansionok",
                CancelTextOverrideId = "mainmenu_creativeexpansionskip",
                OnCloseButtonPressed = OnClickedPopUpAction
            };

            if (FraggleExpansionData.LetFirstTimePopUpHappen)
            {
                PopupManager.Instance.Show(PopupInteractionType.Warning, ModalMessageDataDisclaimer);
                ShowMainMenuPrompt = false;
                PropertiesReader.WriteFirstTimePopUpGone("letfirsttimepopuphappen");
                FraggleExpansionData.LetFirstTimePopUpHappen = false;
            }
        }

        public Sprite AwesomeLoading;
        IEnumerator LoadBootSplash()
        {
            // AWESOME LOADING IS NOT A REFERENCE TO...

            // All the figuring out
            while (!UnityEngine.Object.FindObjectOfType<MainMenuManager>()) yield return null;
            yield return new WaitForFixedUpdate();
            if (!UnityEngine.Object.FindObjectOfType<BootSplashScreenViewModel>()) yield break;

            // Adding the bootsplash
            var BootSplash = UnityEngine.Object.FindObjectOfType<BootSplashScreenViewModel>();
            AwesomeLoading = Tools.MakeOutAnIcon("https://github.com/kota69th/CreativeExpansionPack/blob/data-usage-branch/notblurredexpansion.png?raw=true", 1919, 1080);
            AwesomeLoading.name = "CreativeExpansion-Bootsplash";
            BootSplash._slides.Add(AwesomeLoading);

            // Manage Sound and Slide duration
            if (!RuntimeManager.HasBankLoaded("BNK_Emote_ExpressiveDance"))
            {
                RuntimeManager.LoadBank("BNK_Emote_ExpressiveDance");
                RuntimeManager.LoadBank("BNK_Emote_ExpressiveDance.assets");
            }

            Il2CppSystem.Nullable<EventInstance> AudioEvent = AudioManager.CreateAudioEvent("SFX_Emote_ExpressiveDance");
            

            // Wait until the Active Slide is the Creative Expansion bootsplash and start the audio
            while (BootSplash.ActiveSlide != AwesomeLoading)
                yield return null;
            
            BootSplash._slideWait.Duration = 2.9f;
            AudioEvent.Value.start();

            
            // Coordinate with the actual Bootsplash View Model to fade out the emote...
            yield return BootSplash._slideWait;

            while (GetAudioFromEventInstance(AudioEvent.Value) != 0)
            {
                AudioEvent.Value.setVolume(GetAudioFromEventInstance(AudioEvent.Value) - (0.01f * 0.125f));
                yield return new WaitForEndOfFrame();
            }

            Log.LogMessage("Bootsplash Sequence done!");

            yield break;
        }

        public float GetAudioFromEventInstance(EventInstance Event)
        {
            Event.getVolume(out float AudioVolume);
            return AudioVolume;
        }
        #endregion
    }
}


