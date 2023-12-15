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
using BepInEx.Unity.IL2CPP.Utils.Collections;
using FG.Common.CMS;
using System.Collections.Generic;
using ScriptableObjects;
using System.Net;
using System;
using Levels.Obstacles;
using TreeView;
using FMOD.Studio;
using FG.Common.Loadables;
//using UnityEngine.InputSystem;
using UnityEngine.UI;
using FraggleExpansion.Patches.Creative;
using FraggleExpansion.Patches.Reticle;
using FraggleExpansion.Patches;
using FGClient.UI;
using BepInEx.Unity.IL2CPP;
using static UnityEngine.UI.GridLayoutGroup;
using Microsoft.Win32.SafeHandles;
using UnityEngine.AddressableAssets;
using System.Text;

namespace FraggleExpansion
{
    [BepInPlugin("FraggleExpansion", "Creative Expansion Pack", "2.2")]
    public class Main : BasePlugin
    {
        public Harmony _Harmony = new Harmony("com.simp.fraggleexpansion");
        public static Main Instance;
        public SlimeGamemodesManager _SlimeGamemodeManager;
        public int CurrentBudget;


        public override void Load()
        {
            Log.LogMessage("Creative Expansion Pack | PLAYTEST | HUNTER STAGE 2");
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

            if (FraggleExpansionData.AddUnusedObjects)
            {
                if (ThemeManager.CurrentThemeData.ID == "THEME_RETRO") // doesn't work OOPS!!!!!
                    AddObjectToCurrentList("placeable_obstacle_spinningbeamshort_retro_large", LevelEditorPlaceableObject.Category.MovingSurfaces, 2, 838);
                AddObjectToCurrentList("placeable_special_goo_slide_large", LevelEditorPlaceableObject.Category.Platforms, 2, 261);

            }
            if (ThemeManager.CurrentThemeData.ID == "THEME_RETRO" && GameModeManager.CurrentGameModeData.ID == "GAMEMODE_SURVIVAL")
                AddObjectToCurrentList("placeable_rule_floorstart_survival_large", LevelEditorPlaceableObject.Category.Platforms, 2, 172);

            AddCMSStringKeys();
            RemoveStockAndRotationLimitsForAllObjects(FraggleExpansionData.RemoveCostAndStock, FraggleExpansionData.RemoveRotation);
            ManagePlaceableExtras();

            ObjectAndCosts = new Dictionary<LevelEditorPlaceableObject, int>();

            while (!UnityEngine.Object.FindObjectOfType<LevelEditorManager>())
                yield return null;

            // Only works when a new round is created, but you can run this in a round load postfix like done here
            if (FraggleExpansionData.BypassBounds)
                LevelEditorManager.Instance.MapPlacementBounds = new Bounds(LevelEditorManager.Instance.MapPlacementBounds.center, new Vector3(100000, 100000, 100000));

            while (!UnityEngine.Object.FindObjectOfType<LevelEditorNavigationScreenViewModel>())
                yield return null;

            if (GameModeManager.CurrentGameModeData.ID != "GAMEMODE_GAUNTLET")
                UnityEngine.Object.FindObjectOfType<LevelEditorNavigationScreenViewModel>().SetCheckListVisible(false);

            UnityEngine.Object.FindObjectOfType<LevelEditorReticleViewModel>().gameObject.FindChild("Reticle_Linking").transform.localScale = new Vector3(1.5f, 1.5f, 1);

            if (FraggleExpansionData.RemoveCostAndStock)
            {
                var _OverBudgetNewColor = new Color(1, 0.51372549019f, 0);
                var RessourceBar = UnityEngine.Object.FindObjectOfType<LevelEditorResourceBarViewModel>();
                LevelEditorManager.Instance.UI._overBudgetColor = _OverBudgetNewColor;
                RessourceBar._barData = LevelEditorManager.Instance.UI.GetResourceBarData();
                RessourceBar.RaiseAllPropertiesChanged();
            }

            yield break;
        }

        // These value pairs are only here for experimenting for the making of the upcoming "Universal Object" feature.

        Dictionary<string, string> OriginalListPairs = new Dictionary<string, string>()
        {
            { "POD_FanPlatform_ON_Vanilla", "placeable_obstacle_fanplatform_beam_vanilla_medium" },
            { "POD_SnowVanilla_Flat", "placeable_feature_snowblock_vanilla_left_flat_medium" },
            { "POD_SnowVanilla_Gentle", "placeable_feature_snowblock_vanilla_left_gentle_medium" },
            { "POD_SnowVanilla_Moderate", "placeable_feature_snowblock_vanilla_left_moderate_medium" },
            { "POD_SnowVanilla_Strong", "placeable_feature_snowblock_vanilla_left_strong_medium" },
            { "POD_SpinningBeam_Normal_Vanilla", "placeable_obstacle_spinningbeam_vanilla_medium" },
            { "POD_Arch_Vanilla", "placeable_feature_arch_vanilla_medium" },
            { "POD_Barrier_Vanilla", "placeable_block_barrier_vanilla_medium" },
            { "POD_Blizzard_Fan_Vanilla", "placeable_obstacle_blizzardfan_vanilla_medium" },
            { "POD_BoomBlaster_Vanilla", "placeable_obstacle_boomblaster_vanilla_medium" },
            { "POD_BounceBoard_Vanilla", "placeable_bounceboard_medium_vanilla" },
            { "POD_Bowl_Platform_Vanilla", "placeable_block_bowl_platform_vanilla_medium" },
            { "POD_Break_Vanilla", "placeable_feature_break_vanilla_medium" },
            { "POD_Bumper_Vanilla", "placeable_obstacle_bumper_vanilla_medium" },
            { "POD_Circular_Platform_Vanilla", "placeable_block_circular_platform_vanilla_medium" },
            { "POD_Conveyor_Vanilla", "placeable_feature_conveyor_medium" },
            { "POD_Cylinder_Vanilla", "placeable_feature_block_cylinder_vanilla_medium" },
            { "POD_Diamond_Vanilla", "placeable_feature_block_diamond_vanilla_medium" },
            { "POD_Drawable_Edge_Curve_Vanilla", "placeable_drawable_edge_curve_vanilla" },
            { "POD_Drawable_Edge_Divider_Vanilla", "placeable_drawable_edge_divider_vanilla" },
            { "POD_Drawable_Ramp_Hard_Vanilla", "placeable_drawable_ramp_hard_vanilla" },
            { "POD_Drawable_Ramp_Soft_Vanilla", "placeable_drawable_ramp_soft_vanilla" },
            { "POD_Drawbridge_Vanilla", "placeable_feature_drawbridge_vanilla_medium" },
            { "POD_Flag_Vanilla", "placeable_decoration_flag_vanilla_medium" },
            { "POD_Flipper_Vanilla", "placeable_obstacle_flipper_vanilla_medium" },
            { "POD_Floor_Conveyor_Vanilla", "placeable_floor_conveyor_vanilla" },
            { "POD_Floor_Fan_Vanilla", "placeable_obstacle_floorfan_vanilla_medium" },
            { "POD_Floor_Goop_Vanilla", "placeable_floor_goop_vanilla" },
            { "POD_Floor_Hoop_Vanilla", "placeable_floor_hoop_vanilla_medium" },
            { "POD_Floor_Soft_Vanilla", "placeable_floor_soft_vanilla" },
            { "POD_Halfpipe_Vanilla", "placeable_feature_halfpipe_vanilla_large" },
            { "POD_Hover_Arrow_Vanilla", "placeable_decoration_hover_arrow_vanilla_medium" },
            { "POD_Pachinko_Pillar_Vanilla", "placeable_feature_pachinkopillar_vanilla_medium" },
            { "POD_Pillar_Vanilla", "placeable_block_pillar_square_vanilla_medium" },
            { "POD_PressurePlate_Vanilla", "placeable_obstacle_pressureplate_vanilla" },
            { "POD_QuarterPipe_Vanilla", "placeable_floor_quarterpipe_vanilla_medium" },
            { "POD_Rainbow_Vanilla", "placeable_feature_rainbow_vanilla_medium" },
            { "POD_Rectangle_Vanilla", "placeable_feature_block_rectangle_vanilla_medium" },
            { "POD_Rule_Checkpoint_Revised_Vanilla", "placeable_rule_checkpoint_vanilla" },
            { "POD_Semicircle_Vanilla", "placeable_feature_block_semicircle_vanilla_medium" },
            { "POD_Soft_Hill_Vanilla", "placeable_block_softhill_vanilla_medium" },
            { "POD_Spin_Disc_Vanilla", "placeable_obstacle_spindisc_vanilla_medium" },
            { "POD_Spinning_Hammer_Vanilla", "placeable_obstacle_spinninghammer_vanilla_medium" },
            { "POD_Square_Vanilla", "placeable_feature_square_vanilla_medium" },
            { "POD_Swinging_Axe_Vanilla", "placeable_obstacle_swingingaxe_vanilla_medium" },
            { "POD_Swinging_Club_Vanilla", "placeable_obstacle_swingingclub_vanilla_medium" },
            { "POD_Trench_Vanilla", "placeable_block_trench_vanilla_medium" },
            { "POD_Wall_Inflatable_Vanilla", "placeable_wall_inflatable_vanilla_post_combined" },
            { "POD_Wedge_Vanilla", "placeable_feature_block_wedge_vanilla_medium" },
            { "POD_Wheel_Maze_Revised_Common", "placeable_wheelmaze_revised" },
            { "POD_Wrecking_Ball_Vanilla", "placeable_obstacle_wrecking_ball_vanilla_1_medium" },
            { "POD_SeeSaw_unification_Vanilla", "placeable_seesaw_vanilla" },
            { "POD_Drum_Vanilla", "placeable_drum_medium_vanilla" },
            { "POD_ForceField_Vanilla", "placeable_obstacle_forcefield_vanilla_medium" }
        };

        Dictionary<string, string> DigitalListPairs = new Dictionary<string, string>()
        {
            { "POD_Arch_Retro", "placeable_feature_arch_retro_medium" },
            { "POD_BoomBlaster_Retro", "placeable_obstacle_boomblaster_retro_medium" },
            { "POD_BounceBoard_Retro", "placeable_bounceboard_medium_retro" },
            { "POD_Circular_Platform_Retro", "placeable_block_circular_platform_retro_medium"},
            { "POD_Cliff_Revised_Common", "placeable_feature_cliff_vanilla_medium_revised"},
            { "POD_Cloud_Revised_Common", "placeable_decoration_cloud_vanilla_medium_revised"},
            { "POD_Compound_Halfpipe_Common", "placeable_obstacle_compoundhalfpipe_vanilla_large"},
            { "POD_DoubleSpeedArch_Vanilla", "placeable_obstacle_doublespeedarch_vanilla_medium"},
            { "POD_Drawbridge_Retro", "placeable_feature_drawbridge_retro_medium"},
            { "POD_Hard_Cube_Retro", "placeable_feature_hard_block_cube_retro_medium"},
            { "POD_Move_Box_Vanilla", "placeable_feature_move_box_vanilla_medium"},
            { "POD_Move_Box_With_Fan_Vanilla", "placeable_feature_move_box_with_fan_vanilla_medium"},
            { "POD_Move_Ramp_Vanilla", "placeable_feature_move_ramp_vanilla_medium"},
            { "POD_Move_Step_Ramp_Vanilla", "placeable_feature_step_ramp_vanilla_medium"},
            { "POD_PowerupPickup_Vanilla", "placeable_obstacle_poweruppickup_vanilla_medium"},
            { "POD_PressurePlate_Retro", "placeable_obstacle_pressureplate_retro"},
            { "POD_QuarterPipe_Retro", "placeable_floor_quarterpipe_retro_medium"},
            { "POD_Rainbow_Retro", "placeable_feature_rainbow_retro_medium"},
            { "POD_SpawnBasket_Vanilla", "placeable_obstacle_spawnbasket_vanilla_medium"},
            { "POD_SpeedArch_Vanilla", "placeable_obstacle_speedarch_vanilla_medium"},
            { "POD_Wheel_Maze_Revised_Common", "placeable_wheelmaze_revised"},
            { "POD_CompoundDoorDash_Retro", "placeable_obstacle_compounddoordash_retro_large"},
            { "POD_SeeSaw_Retro", "placeable_seesaw_retro"},
            { "POD_SnowRetro_Flat", "placeable_feature_snowblock_retro_left_flat_medium"},
            { "POD_SnowRetro_Gentle", "placeable_feature_snowblock_retro_left_gentle_medium"},
            { "POD_SnowRetro_Moderate", "placeable_feature_snowblock_retro_left_moderate_medium"},
            { "POD_Barrier_Retro", "placeable_block_barrier_retro_medium"},
            { "POD_Blizzard_Fan_Retro", "placeable_obstacle_blizzardfan_retro_medium"},
            { "POD_Bowl_Platform_Retro", "placeable_block_bowl_platform_retro_medium"},
            { "POD_Break_Retro", "placeable_feature_break_retro_medium"},
            { "POD_Bumper_Retro", "placeable_obstacle_bumper_retro_medium"},
            { "POD_Cylinder_Retro", "placeable_feature_block_cylinder_retro_medium"},
            { "POD_Diamond_Retro", "placeable_feature_block_diamond_retro_medium"},
            { "POD_Drawable_Edge_Curve_Retro", "placeable_drawable_edge_curve_retro"},
            { "POD_Drawable_Edge_Divider_Retro", "placeable_drawable_edge_divider_retro"},
            { "POD_Drawable_Ramp_Hard_Retro", "placeable_drawable_ramp_hard_retro"},
            { "POD_Drawable_Ramp_Soft_Retro", "placeable_drawable_ramp_soft_retro"},
            { "POD_Drum_Retro", "placeable_drum_medium_retro"},
            { "POD_Flag_Retro", "placeable_decoration_flag_retro_medium"},
            { "POD_Flipper_Retro", "placeable_obstacle_flipper_retro_medium"},
            { "POD_Floating_CannonMulti_Retro", "placeable_feature_floating_multicannon_retro"},
            { "POD_Floor_Conveyor_Retro", "placeable_floor_conveyor_retro"},
            { "POD_Floor_Fan_Retro", "placeable_obstacle_floorfan_retro_medium"},
            { "POD_Floor_Goop_Retro", "placeable_floor_goop_retro"},
            { "POD_Floor_Hoop_Retro", "placeable_floor_hoop_retro_medium"},
            { "POD_Floor_Soft_Retro", "placeable_floor_soft_retro"},
            { "POD_Halfpipe_Retro", "placeable_feature_halfpipe_retro_large"},
            { "POD_Hover_Arrow_Retro", "placeable_decoration_hover_arrow_retro_medium"},
            { "POD_Pachinko_Pillar_Retro", "placeable_feature_pachinkopillar_retro_medium"},
            { "POD_Pillar_Retro", "placeable_block_pillar_square_retro_medium"},
            { "POD_Punch_Retro", "placeable_obstacle_punch_retro_medium"},
            { "POD_Rectangle_Retro", "placeable_feature_block_rectangle_retro_medium"},
            { "POD_Rule_Checkpoint_Retro", "placeable_rule_checkpoint_retro"},
            { "POD_Rule_Floor_End_Retro", "placeable_rule_floorend_retro_large"},
            { "POD_Rule_Floor_Start_Retro", "placeable_rule_floorstart_retro_large"},
            { "POD_Semicircle_Retro", "placeable_feature_block_semicircle_retro_medium"},
            { "POD_Soft_Hill_Retro", "placeable_block_softhill_retro_medium"},
            { "POD_Spin_Disc_Retro", "placeable_obstacle_spindisc_retro_medium"},
            { "POD_Spin_Door_Retro", "placeable_obstacle_spindoor_retro_medium"},
            { "POD_Spinning_Hammer_Retro", "placeable_obstacle_spinninghammer_retro_medium"},
            { "POD_SpinningBeam_Normal_Retro", "placeable_obstacle_spinningbeam_retro_medium"},
            { "POD_SpinningBeam_Short_Retro", "placeable_obstacle_spinningbeamshort_retro_medium"},
            { "POD_Square_Retro", "placeable_feature_square_retro_medium"},
            { "POD_Swinging_Axe_Retro", "placeable_obstacle_swingingaxe_retro_medium"},
            { "POD_Swinging_Club_Retro", "placeable_obstacle_swingingclub_retro_medium"},
            { "POD_Trench_Retro", "placeable_block_trench_retro_medium"},
            { "POD_Triangle_Retro", "placeable_feature_triangle_retro_medium"},
            { "POD_Wall_Inflatable_Retro", "placeable_wall_inflatable_retro_post_combined"},
            { "POD_Wedge_Retro", "placeable_feature_block_wedge_retro_medium"},
            { "POD_Wrecking_Ball_Retro", "placeable_obstacle_wrecking_ball_retro_1_medium"},
            { "POD_ForceField_Retro", "placeable_obstacle_forcefield_retro_medium"},
            { "POD_Special_Goo_Slide", "placeable_special_goo_slide_large"}
        };

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
                {"wle_creative_expansion_event_shoot", "Toggle Shooting"}
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
                        Drawable._painterMaxSize = new Vector3(int.MaxValue, int.MaxValue, int.MaxValue);
                        Drawable._canBePainterDrawn = true;
                        Drawable.FloorType = LevelEditorDrawableData.DrawableSemantic.FloorObject;
                        Drawable._restrictedDrawingAxis = LevelEditorDrawableData.DrawRestrictedAxis.Up;

                        UnityEngine.Object.Destroy(Prefab.GetComponent<LevelEditorFloorScaleParameter>());
                        Prefab.GetComponent<LevelEditorPlaceableObject>().hasParameterComponents = false;
                    }
                    if (FraggleExpansionData.InsanePainterSize)
                    {
                        Drawable._painterMaxSize = new Vector3(int.MaxValue, int.MaxValue, int.MaxValue);
                    }
                }

                if (Prefab.name == "Placeable_Feature_Floating_Cannon_Vanilla_V2" || Placeable.name == "POD_Floating_Cannon_Retro")
                {
                    var Receiver = Prefab.GetComponent<LevelEditorReceiver>() ? Prefab.GetComponent<LevelEditorReceiver>() : Prefab.AddComponent<LevelEditorReceiver>();
                    Prefab.GetComponent<LevelEditorPlaceableObject>()._receiver = Receiver;
                    Prefab.GetComponentInChildren<CannonActiveStateEventResponders>()._eventResponderNameKey = "wle_creative_expansion_event_shoot";

                }

                if (Prefab.GetComponent<LevelEditorDrawablePremadeWallSurface>())
                {
                    var DrawableWallSurface = Prefab.GetComponent<LevelEditorDrawablePremadeWallSurface>();
                    DrawableWallSurface._useBetaWalls = FraggleExpansionData.BetaWalls && ThemeManager.CurrentThemeData.ID != "THEME_RETRO";
                }

                if (Prefab.GetComponentInChildren<LevelEditorTransmitter>())
                {
                    Prefab.GetComponentInChildren<LevelEditorTransmitter>()._maxNumReceivers = int.MaxValue;
                }

                if (Placeable.name == "POD_Rule_Floor_Start_Survival")
                {
                    Placeable.objectNameKey = "wle_item_holographicstartname";
                    Placeable.objectDescriptionKey = "wle_item_holographicstartdesc";

                    Placeable.defaultVariant.Prefab.GetComponent<LevelEditorPlaceableObject>().ParameterTypes = LevelEditorParametersManager.LegacyParameterTypes.None;
                }

                if (Prefab.GetComponent<LevelEditorScaleParameter>())
                {
                    var ScaleParam = Prefab.GetComponent<LevelEditorScaleParameter>();
                    ScaleParam._values._maximumScale = new Vector3(15, 15, 15);
                    ScaleParam._values.ParameterScaleToUnitsMultiplier = 0.1f;
                }
            }
        }

        public void RemoveStockAndRotationLimitsForAllObjects(bool RemoveStock, bool RemoveRotation)
        {
            foreach (var Placeable in LevelEditorObjectList.CurrentObjects.Cast<Il2CppSystem.Collections.Generic.List<PlaceableObjectData>>())
                try { RemoveStockAndRotationLimitsForObject(Placeable, RemoveStock, RemoveRotation); } catch { }
            // In 10.4 somehow some objects do not have object owners so catch exceptions (faster)

        }

        public void RemoveStockAndRotationLimitsForObject(PlaceableObjectData Owner, bool RemoveStock = true, bool RemoveRotation = true)
        {
            foreach (var Variant in Owner.objectVariants)
            {

                PlaceableObjectData TrueOwner = Variant.Prefab.GetComponent<LevelEditorPlaceableObject>().ObjectDataOwner;
                var RotationHandler = TrueOwner.RotationHandler;
                if (RotationHandler != null && RemoveRotation)
                {
                    RotationHandler.xAxisIsLocked = false;
                    RotationHandler.yAxisIsLocked = false;
                    RotationHandler.zAxisIsLocked = false;
                }
                var CostHandler = TrueOwner.GetCostHandler();
                if (CostHandler != null && RemoveStock)
                    CostHandler._stockCountAllowed = int.MaxValue;
            }
        }

        public void AddObjectToCurrentList(string AssetRegistryName, LevelEditorPlaceableObject.Category Category = LevelEditorPlaceableObject.Category.Hidden, int DefaultVariantIndex = 0, int ID = 0, bool AddedViaUniversalObjects = false)
        {
            try
            {
                AddressableLoadableAsset Loadable = AssetRegistry.Instance.LoadAsset(AssetRegistryName);
                PlaceableObjectData Owner = Loadable.Asset.Cast<PlaceableVariant_Base>().Owner;
                LevelEditorObjectList CurrentLevelEditorObjectList = ThemeManager.CurrentThemeData.ObjectList;
                var CurrentObjectList = LevelEditorObjectList.CurrentObjects.Cast<Il2CppSystem.Collections.Generic.List<PlaceableObjectData>>();
                if (Owner == null) return;
                if (CurrentObjectList.Contains(Owner) && HasCarouselDataForObject(Owner)) return;
                if (!AddedViaUniversalObjects)
                    Owner.category = Category;
                VariantTreeElement VariantElement = new VariantTreeElement(Owner.name, 0, ID);
                Owner.defaultVariant = Owner.objectVariants[DefaultVariantIndex];
                VariantElement.Variant = Owner.objectVariants[DefaultVariantIndex];
                //CurrentLevelEditorObjectList.CarouselItems.children.Add(VariantElement);
                CurrentLevelEditorObjectList.m_TreeElements.Add(VariantElement);
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


        public static Dictionary<LevelEditorPlaceableObject, int> ObjectAndCosts = null;


        public void OnUpdate()
        {
            if (!FraggleCommonManager.Instance.IsInLevelEditor) return;
            if (Input.GetKeyInt(UnityEngine.KeyCode.End) && !LevelEditorManager.Instance.IsInLevelEditorState<LevelEditorStateMenus>() && !LevelEditorManager.Instance.IsInLevelEditorState<LevelEditorStateTest>() && !LevelEditorManager.Instance.IsInLevelEditorState<LevelEditorStateExplore>())
                LevelEditorManager.Instance.ReplaceCurrentLevelEditorState(new LevelEditorStateMenus(LevelEditorManager.Instance, false).Cast<ILevelEditorState>());

            if (Input.GetKeyInt(UnityEngine.KeyCode.Insert) && LevelEditorManager.Instance.IsMultiselectEnabled())
            {
                var MultiselectHandler = LevelEditorManager.Instance.GetMultiselectHandler();

                foreach (var Placeable in LevelIO.PlaceableObjects)
                {
                    if (/*!Placeable.GetComponent<LevelEditorGroup>() &&*/ Placeable.ObjectDataOwner != ThemeManager.CurrentThemeData.ObjectList.Walls)
                        MultiselectHandler.AddToSelection(Placeable, 1);
                }
            }
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
            // awesome loading

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
            var AudioEvent = RuntimeManager.CreateInstance(AudioManager.GetGuidForKey("SFX_Emote_ExpressiveDance"));


            //while (!AudioEvent.hasValue) yield return null;

            // Wait until the Active Slide is the Creative Expansion bootsplash and start the audio
            while (BootSplash.ActiveSlide != AwesomeLoading)
                yield return null;

            BootSplash._slideWait.Duration = 2.9f;
            AudioEvent.start();


            // Coordinate with the actual Bootsplash View Model to fade out the emote...
            yield return BootSplash._slideWait;

            while (GetAudioFromEventInstance(AudioEvent) != 0)
            {
                AudioEvent.setVolume(GetAudioFromEventInstance(AudioEvent) - (0.01f * 0.125f));
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


