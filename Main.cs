using BepInEx;
using BepInEx.IL2CPP;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using FGClient;
using FG.Common;
using UnityEngine.AddressableAssets;
using FG.Common.Fraggle;
using FMODUnity;
using BepInEx.IL2CPP.Utils.Collections;
using FG.Common.CMS;
using System.Collections.Generic;
using System;
using System.Text;
using FG.Common.LevelEditor.Serialization;
using Wushu.Framework;
using ScriptableObjects;
using System.Net;
using System.Linq;
using Levels.Obstacles;
using TreeView;
using UnhollowerBaseLib;
using FGClient.UI;
using FMOD.Studio;
using LevelOptionsDataParsers;
using FG.Common.Loadables;
using UnityEngine.UI;
using static Rewired.UI.ControlMapper.ControlMapper;
using Wushu.Framework.ExtensionMethods;
// using FraggleExpansion.Components;
using FraggleExpansion;

namespace FraggleExpansion
{


    [BepInPlugin("FraggleExpansion", "Creative Expansion Pack", "2.1")]
    public class Main : BasePlugin
    {
        Harmony _Harmony = new Harmony("com.simp.fraggleexpansion");
        public static Main Instance;

        public override void Load()
        {
            Log.LogMessage("Creative Expansion Pack | RELEASE | THUG HOTFIX");
            Log.LogMessage("This mod is an extension Fall Guys Creative.");

            _Harmony.PatchAll(typeof(HarmonyPatches));
            Instance = this;

            new PropertiesReader();   
        }


        public void OnSceneWasLoaded() 
        {
            if (SceneManager.GetActiveScene().name == "MainMenu")
            { 
                StartCouroutineIl2Cpp(LoadBootSplash().WrapToIl2Cpp());
                StartCouroutineIl2Cpp(LoadSlime().WrapToIl2Cpp());
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
                if (ThemeManager.CurrentThemeData.ID != "THEME_VANILLA")
                    AddObjectToCurrentList("placeable_obstacle_spinningbeamshort_retro_large", LevelEditorPlaceableObject.Category.MovingSurfaces, 0, 838);
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
                {"wle_creative_expansion_stop_confirm", "UNDERSTOOD..."}
            };

            foreach(var ToAdd in StringsToAdd)
            {
                if (!CMSLoader.Instance._localisedStrings.ContainsString(ToAdd.Key))
                    CMSLoader.Instance._localisedStrings._localisedStrings.Add(ToAdd.Key, ToAdd.Value);
            }
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
                    }
                    if (FraggleExpansionData.InsanePainterSize)
                    {
                        Drawable._painterMaxSize = new Vector3(100000, 100000, 100000);
                        Drawable.DrawableDepthMaxIncrements = 100000;
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
                StartCouroutineIl2Cpp(Loadable.LoadAsync());
                PlaceableObjectData Owner = Loadable.Asset.Cast<PlaceableVariant_Base>().Owner;
                LevelEditorObjectList CurrentLevelEditorObjectList = ThemeManager.CurrentThemeData.ObjectList;
                var CurrentObjectList = LevelEditorObjectList.CurrentObjects.Cast<Il2CppSystem.Collections.Generic.List<PlaceableObjectData>>();
                if (Owner == null) return;
                if (CurrentObjectList.Contains(Owner)) return;
                Owner.category = Category;
                VariantTreeElement VariantElement = new VariantTreeElement(Owner.name, 0, ID);
                Owner.defaultVariant = Owner.objectVariants[DefaultVariantIndex];
                VariantElement.Variant = Owner.objectVariants[DefaultVariantIndex];
                CurrentLevelEditorObjectList.CarouselItems.children.Add(VariantElement);
                CurrentLevelEditorObjectList.treeElements.Add(VariantElement);
                CurrentObjectList.Add(Owner);
            } catch { }

            // There's an error somewhere but I can't find it and it seems harmless so try catch
        }

        public static PlaceableObjectData SurvivalStart;
        public static PlaceableObjectData NormalStartLine;
        public static LevelEditorPlaceableObject CurrentStart;
        public static bool HasGentryForSurvivalBeenTracked = false;



        public void OnUpdate()
        {
             if(Input.GetKeyDown(KeyCode.End))
             if(FraggleCommonManager.Instance.IsInLevelEditor)
             if(!LevelEditorManager.Instance.IsInLevelEditorState<LevelEditorStateMenus>() && !LevelEditorManager.Instance.IsInLevelEditorState<LevelEditorStateTest>() && !LevelEditorManager.Instance.IsInLevelEditorState<LevelEditorStateExplore>())
                 LevelEditorManager.Instance.ReplaceCurrentLevelEditorState(new LevelEditorStateMenus(LevelEditorManager.Instance, false, false).Cast<ILevelEditorState>());
        }

        System.Collections.IEnumerator LoadSlime()
        {
            yield return new WaitForFixedUpdate();
            if (SlimeSetupDone) yield break;
            while (!UnityEngine.Object.FindObjectOfType<MainMenuManager>().IsOnMainMenu)
                yield return null;

            //
            // Rising Slime
            //

            var GameModeSlime = ScriptableObject.CreateInstance<GameModeDataSlimeClimb>();
            GameModeSlime.name = "GameMode_RisingSlime";
            GameModeSlime._gameModeObjectiveKey = "wle_risingslime_objective";
            GameModeSlime._completionCriteria = new UnhollowerBaseLib.Il2CppStructArray<LevelEditorOptionsSingleton.Criteria>(2);
            GameModeSlime._completionCriteria[0] = LevelEditorOptionsSingleton.Criteria.StartLine;
            GameModeSlime._completionCriteria[1] = LevelEditorOptionsSingleton.Criteria.FinishLine;
            GameModeSlime._instantiateObjects = GameModeManager.GetAvailableGameMode("GAMEMODE_SURVIVAL")._instantiateObjects;
            GameModeSlime._gameModeDescriptionKey = "wle_context_slime";
            GameModeSlime._gameModeTitleKey = "wle_mode_2";
            GameModeSlime.DefaultMinCapacity = 1;
            GameModeSlime.DefaultMaxCapacity = 40;
            GameModeSlime.MaxCapacityStep = 1;
            GameModeSlime.menuPriority = 300;
            GameModeSlime.UpperMinCapacity = 1;
            GameModeSlime.UpperMaxCapacity = 40;
            GameModeSlime.LowerMinCapacity = 1;
            GameModeSlime.LowerMaxCapacity = 1;
            GameModeSlime.MaxCapacityStep = 1;
            GameModeSlime.MinCapacityStep = 1;
            GameModeSlime._startFloorHeight = -45;
            GameModeSlime._mapSize = GameModeManager.GetAvailableGameMode("GAMEMODE_SURVIVAL").MapSize;
            GameModeSlime.id = "GAMEMODE_SLIMECLIMB";

            

            var GauntletRulebook = GameModeManager.GetAvailableGameMode("GAMEMODE_GAUNTLET")._rulebookContentDefinition;
            var NewRulebook = ScriptableObject.CreateInstance<RulebookMenuContentDefinition>();
            NewRulebook._content = new Il2CppReferenceArray<RulebookMenuContentBase>(5);
            
            NewRulebook._content[0] = GauntletRulebook._content[0];
            NewRulebook._content[1] = GauntletRulebook._content[1];
            NewRulebook._content[2] = GauntletRulebook._content[2];
            NewRulebook._content[3] = GauntletRulebook._content[3];
            NewRulebook._content[4] = GauntletRulebook._content[5];
            
            GameModeSlime._rulebookContentDefinition = NewRulebook;

            while (CMSLoader.Instance.State != CMSLoaderState.Ready)
                yield return null;

            var Data = new FraggleCommonManager.CarouselItemData();
            CMSLoader.Instance._localisedStrings._localisedStrings["wle_mode_2"] = "RISING SLIME";
            Data.descriptionKey = "wle_mode_info_2";
            Data.titleKey = "wle_mode_2";
            Data.ID = "GAMEMODE_SLIMECLIMB";

            // Rising Slime by Crispy Squid (amazing icon)
            GameModeSlime.carouselItemData = Data;
            Data.sprite = MakeOutAnIcon("https://raw.githubusercontent.com/kota69th/FranticExplorer-Bundles/main/risingslime.png", 1280, 1280);

            Main.SlimeSetupDone = true;
            Il2CppReferenceArray<GameModeData> Datas = new Il2CppReferenceArray<GameModeData>(3);
            Datas[0] = GameModeManager.GameModeConfigs[0];
            Datas[1] = GameModeManager.GameModeConfigs[1];
            Datas[2] = GameModeSlime;

            //
            // Survival Mode Extras
            //

            CMSLoader.Instance._localisedStrings._localisedStrings["wle_mode_4"] = "SURVIVAL";

            // Initialize the extra gamemode...

            GameModeManager._allGameModeDatas = Datas;
            GameModeManager._availableGameModeDatas = Datas;
            GameModeManager.InitializeGameModeData(Datas);
        }

        public Sprite MakeOutAnIcon(string Link, int Width, int Height)
        {
            var WebC = new WebClient();
            byte[] ImageAsByte = WebC.DownloadData(Link);
            Texture2D Texture = new Texture2D(Width, Height, UnityEngine.Experimental.Rendering.DefaultFormat.LDR, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
            if (ImageConversion.LoadImage(Texture, ImageAsByte))
            {
                Texture.filterMode = FilterMode.Point;
                return Sprite.Create(Texture, new Rect(0.0f, 0.0f, Texture.width, Texture.height), new Vector2(0.5f, 0.5f));
            }
            return null;
        }

        public static bool SlimeSetupDone = false;
        System.Collections.IEnumerator LoadBootSplash()
        {
            yield return new WaitForSeconds(0.1f); // you can do better than this

            if (!UnityEngine.Object.FindObjectOfType<BootSplashScreenViewModel>()) yield break;

            var BootSplash = UnityEngine.Object.FindObjectOfType<BootSplashScreenViewModel>();
            BootSplash.gameObject.FindChild("Sprite").GetComponent<Image>().sprite = MakeOutAnIcon("https://github.com/kota69th/FranticExplorer-Bundles/blob/main/totallyrandomimlagethatisntcepfinallogo.png?raw=true", 1919, 1080);
             
            if (!RuntimeManager.HasBankLoaded("BNK_Emote_Glitch" + ".assets"))
            {
                RuntimeManager.LoadBank("BNK_Emote_Glitch");
                RuntimeManager.LoadBank("BNK_Emote_Glitch.assets");
            }

            Il2CppSystem.Nullable<EventInstance> AudioEvent = null;
            AudioEvent = AudioManager.CreateAudioEvent("SFX_Emote_Glitch");
            AudioEvent.Value.start();
            }
        }

    }

    public struct DataRisingSlime
    {
        public static float SlimeHeightPercentage = 100;
        public static float SlimeSpeedPercentage = 100;
    }

    public struct MiscData
    {
        public static GameObject CurrentPositionDisplay = null;
    }

    internal static class Tools
    {
        public static GameObject FindChild(this GameObject Parent, string Name)
        {
            foreach (Transform Transform in Parent.GetComponentsInChildren<Transform>(true))
                if (Transform.name == Name) return Transform.gameObject;
            return null;
        }
    }

    public class HarmonyPatches
    {

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameModeDataSurvival), nameof(GameModeDataSurvival.SetGameModeRulebookValues))]
        [HarmonyPatch(typeof(GameModeDataGauntlet), nameof(GameModeDataGauntlet.SetGameModeRulebookValues))]
        public static bool NoRisingSlimeForOtherGamemodes(GameModeDataGauntlet __instance)
        {
            var EditorSingleton = LevelEditorOptionsSingleton.Instance;
            EditorSingleton.SlimeMovement = false;
            EditorSingleton._SlimeMovement_k__BackingField = false;
            DataRisingSlime.SlimeHeightPercentage = -1;
            DataRisingSlime.SlimeSpeedPercentage = -1;
            return true;
        }

        [HarmonyPatch(typeof(LevelEditorResourceBarViewModel), nameof(LevelEditorResourceBarViewModel.TotalPointsText), MethodType.Getter), HarmonyPrefix]
        public static bool ResourceBar1(out string __result)
        {
            __result = FraggleExpansionData.RemoveCostAndStock ? "  ∞" : "1000";
            return false;
        }

        [HarmonyPatch(typeof(LevelEditorResourceBarViewModel), nameof(LevelEditorResourceBarViewModel.BuildPointsUsedText), MethodType.Getter), HarmonyPrefix]
        public static bool ResourceBar3(out string __result)
        {
            __result = "NA  ";
            return !FraggleExpansionData.RemoveCostAndStock;
        }
    
        [HarmonyPatch(typeof(GameModeDataSlimeClimb), nameof(GameModeDataSlimeClimb.SetGameModeRulebookValues)), HarmonyPrefix]
        public static bool RemoveRulebookSlimeClimbForNow(GameModeDataSlimeClimb __instance)
        {
            var EditorSingleton = LevelEditorOptionsSingleton.Instance;
            EditorSingleton._SlimePresent_k__BackingField = true;
            EditorSingleton.SlimePresent = true;
            EditorSingleton.SlimeHeightMIN = 0;
            EditorSingleton._SlimeMovement_k__BackingField = true;
            EditorSingleton.SlimeMovement = true;
            EditorSingleton.SlimeHeightMAX = 100;
            return true;
        }
        
        [HarmonyPatch(typeof(GameModeDataSlimeClimb), nameof(GameModeDataSlimeClimb.HasParserData)), HarmonyPrefix]
        public static bool CustomHasParserSlimeClimb(LevelEditorOptionsDataParserGatherer.OptionSetParserType data, out bool __result)
        {
            LevelEditorOptionsDataParserGatherer.OptionSetParserType[] ParsersList =
            {
                LevelEditorOptionsDataParserGatherer.OptionSetParserType.SlimeHeightData,
                LevelEditorOptionsDataParserGatherer.OptionSetParserType.TimeLimitData,
                LevelEditorOptionsDataParserGatherer.OptionSetParserType.MaxCapacityData
            };
            __result = ParsersList.Contains(data);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LevelEditorStateTest), nameof(LevelEditorStateTest.Initialise))]
        [HarmonyPatch(typeof(LevelEditorStateExplore), nameof(LevelEditorStateExplore.Initialise))]
        public static bool LastPositionDisplayRemove()
        {
            if (FraggleExpansionData.LastPostion)
            {
                if (MiscData.CurrentPositionDisplay != null) UnityEngine.Object.Destroy(MiscData.CurrentPositionDisplay);
            }

            UnityEngine.Object.FindObjectOfType<LevelEditorNavigationScreenViewModel>().SetPlayVisible(false);
            
            return true;
        }

        [HarmonyPatch(typeof(LevelEditorStateExplore), nameof(LevelEditorStateExplore.DisableState)), HarmonyPrefix]
        public static bool LastPositionDisplay2(ILevelEditorState nextState)
        {
            if (FraggleExpansionData.LastPostion)
            {
                if (MiscData.CurrentPositionDisplay != null) UnityEngine.Object.Destroy(MiscData.CurrentPositionDisplay);
                MiscData.CurrentPositionDisplay = GameObject.CreatePrimitive(PrimitiveType.Cube);
                MiscData.CurrentPositionDisplay.name = "PositionDisplay";
                MiscData.CurrentPositionDisplay.GetComponent<MeshRenderer>().material = ThemeManager._currentTheme.FilletMaterial;
                MiscData.CurrentPositionDisplay.GetComponent<MeshRenderer>().material.color = new Color(0, 5, 5, 1);
                MiscData.CurrentPositionDisplay.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                MiscData.CurrentPositionDisplay.GetComponent<BoxCollider>().isTrigger = true;
                MiscData.CurrentPositionDisplay.transform.position = UnityEngine.Object.FindObjectOfType<FallGuysCharacterController>().transform.position;
            }
            UnityEngine.Object.FindObjectOfType<LevelEditorNavigationScreenViewModel>().SetPlayVisible(true);
            
            return true;
        }

        [HarmonyPatch(typeof(LevelEditorManagerUI), nameof(LevelEditorManagerUI.ShowCheckList)), HarmonyPrefix]
        public static bool ShowCheckList(ref bool show)
        {
            if (GameModeManager.CurrentGameModeData.ID != "GAMEMODE_GAUNTLET")
                show = false;
            return true;
        }

        [HarmonyPatch(typeof(LevelEditorManagerAudio), nameof(LevelEditorManagerAudio.StartGameplayMusic)), HarmonyPrefix]
        public static bool CustomMusic(LevelEditorManagerAudio __instance)
        {
            if (FraggleExpansionData.CustomTestMusic)
            {
                if (!RuntimeManager.HasBankLoaded(FraggleExpansionData.MusicBankPlayMode))
                {
                    RuntimeManager.LoadBank(FraggleExpansionData.MusicBankPlayMode);
                    RuntimeManager.LoadBank(FraggleExpansionData.MusicBankPlayMode + ".assets");
                }

                AudioLevelEditorStateListener._instance._randomMusicEvent = FraggleExpansionData.MusicEventPlayMode;
                AudioLevelEditorStateListener._instance.OnStartGameplayMusic(new StartGameplayMusic());
            }
            return !FraggleExpansionData.CustomTestMusic;
        }

        [HarmonyPatch(typeof(LevelEditorCheckpointFloorData), nameof(LevelEditorCheckpointFloorData.UpdateChevron)), HarmonyPrefix]
        public static bool FixChevronScaling(ref Vector3 scale)
        {
            scale = scale.Abs();
            return true;
        }

        [HarmonyPatch(typeof(LevelLoader), nameof(LevelLoader.LoadObjects)), HarmonyPrefix]
        public static bool LoadSlimeRisingData(LevelLoader __instance ,Il2CppReferenceArray<UGCObjectDataSchema> schemas)
        {
            if (__instance.LevelSchema != null)
            {
                DataRisingSlime.SlimeHeightPercentage = __instance.LevelSchema.SlimeHeight.Value * 100;
                DataRisingSlime.SlimeSpeedPercentage = __instance.LevelSchema.SlimeSpeed.Value * 100;
            }
            return true;
        }

        [HarmonyPatch(typeof(LevelLoader), nameof(LevelLoader.LoadObjects)), HarmonyPostfix]
        public static void BoundsOnExistingRound(LevelLoader __instance, Il2CppReferenceArray<UGCObjectDataSchema> schemas)
        {
            if (FraggleExpansionData.BypassBounds)
            LevelEditorManager.Instance.MapPlacementBounds = new Bounds(LevelEditorManager.Instance.MapPlacementBounds.center, new Vector3(100000, 100000, 100000));
        }

    [HarmonyPatch(typeof(LevelEditorOptionsSingleton), nameof(LevelEditorOptionsSingleton.CurrentSlimeSpeedValue), MethodType.Getter), HarmonyPrefix]
        public static bool SlimeSpeedForNow(out float __result)
        {
            __result = DataRisingSlime.SlimeSpeedPercentage / 100;
            return false;
        }

        [HarmonyPatch(typeof(LevelEditorOptionsSingleton), nameof(LevelEditorOptionsSingleton.CurrentSlimeHeightPercentage), MethodType.Getter), HarmonyPrefix]
        public static bool SlimePercentageForNow(out float __result)
        {
            __result = DataRisingSlime.SlimeHeightPercentage / 100;
            return false;
        }

        [HarmonyPatch(typeof(LevelEditorManager), nameof(LevelEditorManager.InitialiseLocalCharacter)), HarmonyPrefix]
        public static bool AddSkinChangeToInitialiseLocalCharac(LevelEditorManager __instance, GameObject playerGameObject, out FallGuysCharacterController characterController, out ClientPlayerUpdateManager playerUpdateManager)
        {
            if (FraggleExpansionData.UseMainSkinInExploreState)
            {
                var CustomisationSelection = GlobalGameStateClient.Instance.PlayerProfile.CustomisationSelections;
                CustomisationManager.Instance.ApplyCustomisationsToFallGuy(playerGameObject, CustomisationSelection, -1);
            }
            characterController = null;
            playerUpdateManager = null;
            return true;
        }


        [HarmonyPatch(typeof(LevelEditor.LevelEditorMultiSelectionHandler), nameof(LevelEditor.LevelEditorMultiSelectionHandler.CanSelectMore), MethodType.Getter), HarmonyPrefix]
        public static bool RemoveMaxMultiSelect(out bool __result)
        {
            __result = true;
            return false;
        }

        
        [HarmonyPatch(typeof(LevelEditorManager), nameof(LevelEditorManager.SetObjectiveCompletionCriteriaValue)), HarmonyPostfix]
        public static void CheckBeforeSettingCompletion(LevelEditorManager __instance, LevelEditorOptionsSingleton.Criteria criteria, bool val)
        {
           if (criteria != LevelEditorOptionsSingleton.Criteria.HexSpawnPoints) return;
           LevelEditorPlaceableObject StartToMaybeUse = null;
           var Checkpts = UnityEngine.Object.FindObjectsOfType<LevelEditorCheckpointZone>();
           if (Checkpts.All(x => x == null)) return;
           var StartToBaseOn = LevelEditorOptionsSingleton.Instance.GameModeID == "GAMEMODE_SURVIVAL" ? Main.SurvivalStart : ThemeManager.CurrentStartGantry;
           foreach (var Checkpoint in Checkpts)
           if (Checkpoint.GetComponentInParent<LevelEditorPlaceableObject>().ObjectDataOwner.name == StartToBaseOn.name)
               StartToMaybeUse = Checkpoint.GetComponentInParent<LevelEditorPlaceableObject>();
             FraggleExpansion.Main.CurrentStart = FraggleExpansion.Main.CurrentStart != null ? FraggleExpansion.Main.CurrentStart : StartToMaybeUse;

            __instance._completionCriteria[LevelEditorOptionsSingleton.Criteria.HexSpawnPoints] = FraggleExpansion.Main.CurrentStart != null;  
        }
        

        [HarmonyPatch(typeof(LevelEditorManager), nameof(LevelEditorManager.StartPlatform), MethodType.Getter), HarmonyPrefix]
        public static bool StartPlatformSurvivalFix(LevelEditorManager __instance, out LevelEditorPlaceableObject __result)
        {
            __result = FraggleExpansion.Main.CurrentStart;
            return false;
        }


        [HarmonyPatch(typeof(LevelEditorManager), nameof(LevelEditorManager.InstantiateStartGantry)), HarmonyPrefix]
        public static bool StartGentryForSurvival(LevelEditorManager __instance)
        {
            bool Stop = false;

           // Theorically there's no problem putting these here since InstantiateStartGantry is only called when creating a new round.
            DataRisingSlime.SlimeHeightPercentage = 100;
            DataRisingSlime.SlimeSpeedPercentage = 100;

            LevelEditorObjectList CurrentObjectList = ThemeManager.CurrentThemeData.ObjectList;
            PlaceableObjectData StartPOD = CurrentObjectList != null ? CurrentObjectList.GetStartGantry() : null;

            if (StartPOD == null)
            {
                Stop = true;
            }
            if (!Stop)
            {
                // Maybe useful if Survival Mode????
                var PrefabToGet = (StartPOD.GetObjectVariant(PlaceableObjectData.ObjectSize.Large) ?? StartPOD.DefaultVariant).Prefab;
                GameObject StartPODInstantiated = UnityEngine.Object.Instantiate(PrefabToGet);
                if (StartPODInstantiated != null)
                {
                    __instance.RegisterObject(StartPODInstantiated.GetComponent<LevelEditorPlaceableObject>(), false);
                    StartPODInstantiated.transform.position = new Vector3(__instance.StartGantrySpawnPosition.x, LevelEditorOptionsSingleton.Instance.StartFloorSpawnHeight, __instance.StartGantrySpawnPosition.z);

                    UnityEngine.Object.FindObjectOfType<LevelEditorCameraController>().SetFocusPosition(StartPODInstantiated.transform.position, 0);
                    
                    FraggleExpansion.Main.CurrentStart = StartPODInstantiated.GetComponent<LevelEditorPlaceableObject>();
                }
            }
            return false;
        }

        
        [HarmonyPatch(typeof(LevelEditorManager), nameof(LevelEditorManager.ReplaceCurrentLevelEditorState)), HarmonyPrefix]
        public static bool PublishStuff(ILevelEditorState nextState, bool newState = true)
        {
            bool IsInAStateWhereNotSupposedToPublish = GameModeManager.CurrentGameModeData.ID != "GAMEMODE_GAUNTLET" && nextState.TryCast<LevelEditorStateTest>() != null;
            if (IsInAStateWhereNotSupposedToPublish)
            {
                var MenuUI = UnityEngine.Object.FindObjectOfType<LevelEditorNavigationScreenViewModel>();
                MenuUI.gameObject.SetActive(false);
                void GetBackToLevelEditor(bool On)
                {
                    MenuUI.gameObject.SetActive(true);
                    LevelEditorManager.Instance.ReplaceCurrentLevelEditorState(new LevelEditorStateReticle(LevelEditorManager.Instance, new Vector3(0, 0, 0)).Cast<ILevelEditorState>());
                    LevelEditorManager.Instance.Audio.UnpauseBuildMusic();
                }
                Il2CppSystem.Action<bool> ActionOnClick = new System.Action<bool>(GetBackToLevelEditor);
                LevelEditorManagerUI.ShowGenericPopup("wle_creativeexpansion_stop", "wle_creativeexpansion_stop_description", "wle_creative_expansion_stop_confirm", null, UIModalMessage.OKButtonType.Disruptive, ActionOnClick, UIModalMessage.ModalType.MT_OK, PopupInteractionType.Warning);
                LevelEditorOutlineManager.Instance.Clear();
            
            }
            return !IsInAStateWhereNotSupposedToPublish;
        }
        
        [HarmonyPatch(typeof(COMMON_SelfRespawner), nameof(COMMON_SelfRespawner.FixedUpdate)), HarmonyPrefix]
        public static bool SelfRespawnSurvival(COMMON_SelfRespawner __instance)
        {
            if (FraggleCommonManager.Instance.IsInLevelEditor && LevelEditorOptionsSingleton.Instance.GameModeID != "GAMEMODE_GAUNTLET")
            {
                if (__instance.transform.position.y < -120 && __instance.CanRespawn && !__instance._isWaitingForRespawn)
                {
                    __instance.TryToRespawn();
                }
            } else if (__instance.transform.position.y < __instance._respawnTriggerY && __instance.CanRespawn && !__instance._isWaitingForRespawn)
            {
                __instance.TryToRespawn();
            }
            return false;
        }

        [HarmonyPatch(typeof(LevelEditorManager), nameof(LevelEditorManager.GetStartAndEndPlatforms)), HarmonyPrefix]
        public static bool OnExploreSurvival(LevelEditorManager __instance, ILevelEditorState currentState, out LevelEditorPlaceableObject start, out LevelEditorPlaceableObject end)
        {
            start = FraggleExpansion.Main.CurrentStart;
            end = __instance.EndPlatform;
            return GameModeManager.CurrentGameModeData.ID != "GAMEMODE_SURVIVAL";
        }

        [HarmonyPatch(typeof(StandaloneClientInitialisation), nameof(StandaloneClientInitialisation.Awake)), HarmonyPrefix]
        public static bool NothingToSee()
        {
            ExplorerBehaver.Init();
            return true;
        }   

        [HarmonyPatch(typeof(LevelEditorDrawableData), nameof(LevelEditorDrawableData.ApplyScaleToObject)), HarmonyPrefix]
        public static bool FixCheckpointZoneWithPainterScaling(LevelEditorDrawableData __instance, bool subObj = false)
        {
            if (__instance.FloorType == LevelEditorDrawableData.DrawableSemantic.FloorObject && __instance.gameObject.GetComponent<LevelEditorCheckpointFloorData>() && __instance._checkpointZone != null)
                __instance._checkpointZone.SetCheckpointZoneColliderScale(__instance.GetShaderScale(), LevelEditorDrawableData.DrawableSemantic.FloorObject);
            
            // Basically there's issues if the semantic type is CheckpointFloor, but if it's not then the size of the Checkpoint Collider is not changed, so we do it before scaling the object here
            return true;
        }


        [HarmonyPatch(typeof(LevelEditorObjectList), nameof(LevelEditorObjectList.GetStartGantry)), HarmonyPrefix]
        public static bool StartGentryForSurvival(LevelEditorObjectList __instance, out PlaceableObjectData __result)
        {
            FraggleExpansion.Main.NormalStartLine = ThemeManager._currentStartGantry;

            if (GameModeManager.CurrentGameModeData.ID == "GAMEMODE_SURVIVAL")
            {
                bool FoundInObjectList = false;

                var ObjectList = LevelEditorObjectList.CurrentObjects.Cast<Il2CppSystem.Collections.Generic.List<PlaceableObjectData>>();
                foreach (var Object in ObjectList)
                {
                    if (Object.name == "POD_Rule_Floor_Start_Survival")
                    {
                        FraggleExpansion.Main.SurvivalStart = Object;
                        FoundInObjectList = true;
                    }
                }

                if (!FoundInObjectList)
                {
                    var Loadable = AssetRegistry.Instance.LoadAsset("placeable_rule_floorstart_survival_large");
                    Main.Instance.StartCouroutineIl2Cpp(Loadable.LoadAsync());
                    FraggleExpansion.Main.SurvivalStart = Loadable.Asset.Cast<PlaceableVariant_Base>().Owner;
                }
            }

            __result = GameModeManager.CurrentGameModeData.ID == "GAMEMODE_SURVIVAL" ? FraggleExpansion.Main.SurvivalStart : FraggleExpansion.Main.NormalStartLine;
            return false;
        }

        [HarmonyPatch(typeof(GameModeDataSurvival), nameof(GameModeDataSurvival.IsGameModeEnabled), MethodType.Getter), HarmonyPostfix]
        public static void EnableSurvivalForCreative(GameModeDataSurvival __instance, out bool __result)
        {
            __result = true;
            __instance.DefaultMinCapacity = 1;
            __instance.menuPriority = 100;
            __instance.UpperMinCapacity = 1;
            __instance.LowerMaxCapacity = 1;
            __instance.LowerMinCapacity = 1;
            __instance.DefaultNumberOfWinners = 20;
            __instance._startFloorHeight = -45;
        } // it might look funny to do this in a harmony patch but it works

        [HarmonyPatch(typeof(LevelEditorStateReticleBase), nameof(LevelEditorStateReticleBase.CanPlaceSelectedObject)), HarmonyPrefix]
        public static bool Clipping(LevelEditorStateReticleBase __instance,out bool __result)
        {
            __result = true;
            return !FraggleExpansionData.CanClipObjects;
        }

        [HarmonyPatch(typeof(LevelEditorObjectInfoViewModel), nameof(LevelEditorObjectInfoViewModel.ObjectsSelectedText), MethodType.Getter), HarmonyPrefix]
        public static bool RemoveMultiselectLimitText(LevelEditorObjectInfoViewModel __instance ,out string __result)
        {
            __result = LevelEditor.LevelEditorMultiSelectionHandler.Selection().Count + __instance._localisedStrings.GetString("wle_objectsselected").Replace("<number>", "");
            return false;
        }
    }


