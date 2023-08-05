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
            Log.LogMessage("Creative Expansion Pack | RELEASE | HUNTER HOTFIX");
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
        }


        public void OnSceneWasLoaded() 
        {
            if (SceneManager.GetActiveScene().name == "MainMenu")
            { 
                StartCouroutineIl2Cpp(LoadBootSplash().WrapToIl2Cpp());
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

        IEnumerator LoadBootSplash()
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


