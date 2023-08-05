using FG.Common;
using FG.Common.Fraggle;
using FG.Common.LevelEditor.Serialization;
using FGClient;
using HarmonyLib;
using LevelOptionsDataParsers;
using Levels.Obstacles;
using ScriptableObjects;
using System;
using System.Linq;
using UnhollowerBaseLib;
using UnityEngine;

namespace FraggleExpansion.Patches.Gamemodes
{

    public class SlimeGamemodesPatches
    {
        #region Object Fixes
        [HarmonyPatch(typeof(COMMON_SelfRespawner), nameof(COMMON_SelfRespawner.FixedUpdate)), HarmonyPrefix]
        public static bool SelfRespawnSurvival(COMMON_SelfRespawner __instance)
        {
            if (FraggleCommonManager.Instance.IsInLevelEditor && LevelEditorOptionsSingleton.Instance.GameModeID != "GAMEMODE_GAUNTLET")
            {
                if (__instance.transform.position.y < -120 && __instance.CanRespawn && !__instance._isWaitingForRespawn)
                {
                    __instance.TryToRespawn();
                }
            }
            else if (__instance.transform.position.y < __instance._respawnTriggerY && __instance.CanRespawn && !__instance._isWaitingForRespawn)
            {
                __instance.TryToRespawn();
            }
            return false;
        }
        #endregion


        #region Extra Fixes
        [HarmonyPatch(typeof(LevelEditorManager), nameof(LevelEditorManager.ReplaceCurrentLevelEditorState)), HarmonyPrefix]
        public static bool CanPublish(ILevelEditorState nextState, bool newState = true)
        {
            bool IsInAStateWhereNotSupposedToPublish = GameModeManager.CurrentGameModeData.ID != "GAMEMODE_GAUNTLET" && nextState.TryCast<LevelEditorStateTest>() != null;
            if (IsInAStateWhereNotSupposedToPublish) SlimeGamemodesManager.Instance.PublishState();
            return !IsInAStateWhereNotSupposedToPublish;
        }
        #endregion

        [HarmonyPatch(typeof(LevelEditorManagerUI), nameof(LevelEditorManagerUI.ShowCheckList)), HarmonyPrefix]
        public static bool ChecklistRemoval(ref bool show)
        {
            if (GameModeManager.CurrentGameModeData.ID != "GAMEMODE_GAUNTLET")
                show = false;
            return true;
        }
    }

    /// 
    /// Survival Mode
    ///

    public class SurvivalModePatches
    {
        
        //
        // Gamemode Enabling
        //

        [HarmonyPatch(typeof(GameModeDataSurvival), nameof(GameModeDataSurvival.IsGameModeEnabled), MethodType.Getter), HarmonyPostfix]
        public static void EnableSurvivalForCreative(out bool __result) => __result = true;



        #region Braindead Start Line Management
        [HarmonyPatch(typeof(LevelEditorObjectList), nameof(LevelEditorObjectList.GetStartGantry)), HarmonyPrefix]
        public static bool StartGentryForSurvival(LevelEditorObjectList __instance, out PlaceableObjectData __result)
        {
            var Loadable = AssetRegistry.Instance.LoadAsset("placeable_rule_floorstart_survival_large");
            Main.Instance.StartCouroutineIl2Cpp(Loadable.LoadAsync());
            SlimeGamemodesManager.SurvivalStart = Loadable.Asset.Cast<PlaceableVariant_Base>().Owner;

            __result = SlimeGamemodesManager.SurvivalStart;

            return GameModeManager.CurrentGameModeData.ID != "GAMEMODE_SURVIVAL";
        }

        [HarmonyPatch(typeof(LevelEditorManager), nameof(LevelEditorManager.InstantiateStartGantry)), HarmonyPrefix]
        public static bool StartGentryForSurvival(LevelEditorManager __instance)
        {
            LevelEditorObjectList CurrentObjectList = ThemeManager.CurrentThemeData.ObjectList;
            PlaceableObjectData StartPOD = CurrentObjectList != null ? CurrentObjectList.GetStartGantry() : null;

            if (StartPOD != null)
            {
                var PrefabToGet = (StartPOD.GetObjectVariant(PlaceableObjectData.ObjectSize.Large) ?? StartPOD.DefaultVariant).Prefab;
                GameObject StartPODInstantiated = UnityEngine.Object.Instantiate(PrefabToGet);
                if (StartPODInstantiated != null)
                {
                    __instance.RegisterObject(StartPODInstantiated.GetComponent<LevelEditorPlaceableObject>(), false);
                    StartPODInstantiated.transform.position = new Vector3(__instance.StartGantrySpawnPosition.x, LevelEditorOptionsSingleton.Instance.StartFloorSpawnHeight, __instance.StartGantrySpawnPosition.z);

                    UnityEngine.Object.FindObjectOfType<LevelEditorCameraController>().SetFocusPosition(StartPODInstantiated.transform.position, 0);

                    SlimeGamemodesManager.CurrentStart = StartPODInstantiated.GetComponent<LevelEditorPlaceableObject>();
                }
            }
            return false;
        }

        [HarmonyPatch(typeof(LevelEditorManager), nameof(LevelEditorManager.SetObjectiveCompletionCriteriaValue)), HarmonyPostfix]
        public static void CheckBeforeSettingCompletion(LevelEditorManager __instance, LevelEditorOptionsSingleton.Criteria criteria, bool val)
        {
            if (criteria != LevelEditorOptionsSingleton.Criteria.HexSpawnPoints) return;
            LevelEditorPlaceableObject StartToMaybeUse = null;
            var Checkpts = UnityEngine.Object.FindObjectsOfType<LevelEditorCheckpointZone>();
            if (Checkpts.All(x => x == null)) return;
            var StartToBaseOn = LevelEditorOptionsSingleton.Instance.GameModeID == "GAMEMODE_SURVIVAL" ? SlimeGamemodesManager.SurvivalStart : ThemeManager.CurrentStartGantry;
            foreach (var Checkpoint in Checkpts)
                if (Checkpoint.GetComponentInParent<LevelEditorPlaceableObject>().ObjectDataOwner.name == StartToBaseOn.name)
                    StartToMaybeUse = Checkpoint.GetComponentInParent<LevelEditorPlaceableObject>();
            SlimeGamemodesManager.CurrentStart = SlimeGamemodesManager.CurrentStart != null ? SlimeGamemodesManager.CurrentStart : StartToMaybeUse;

            __instance._completionCriteria[LevelEditorOptionsSingleton.Criteria.HexSpawnPoints] = SlimeGamemodesManager.CurrentStart != null;
        }

        [HarmonyPatch(typeof(LevelEditorManager), nameof(LevelEditorManager.GetStartAndEndPlatforms)), HarmonyPrefix]
        public static bool OnExploreSurvival(LevelEditorManager __instance, ILevelEditorState currentState, out LevelEditorPlaceableObject start, out LevelEditorPlaceableObject end)
        {
            start = SlimeGamemodesManager.CurrentStart;
            end = __instance.EndPlatform;
            return GameModeManager.CurrentGameModeData.ID != "GAMEMODE_SURVIVAL";
        }

        [HarmonyPatch(typeof(LevelEditorManager), nameof(LevelEditorManager.StartPlatform), MethodType.Getter), HarmonyPrefix]
        public static bool StartPlatformSurvivalFix(LevelEditorManager __instance, out LevelEditorPlaceableObject __result)
        {
            __result = SlimeGamemodesManager.CurrentStart;
            return false;
        }
        #endregion
    }

    /// 
    /// Rising Slime Patches
    /// 


    public class RisingSlimeModePatches
    {

        #region GamemodeData Fixes
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

        // Prevent other gamemodes of somehow having Slime Rise...
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
        #endregion

        #region Existing Round Loading
        [HarmonyPatch(typeof(LevelLoader), nameof(LevelLoader.LoadObjects)), HarmonyPrefix]
        public static bool LoadSlimeRisingData(LevelLoader __instance, Il2CppReferenceArray<UGCObjectDataSchema> schemas)
        {
            if (__instance.LevelSchema != null)
            {
                DataRisingSlime.SlimeHeightPercentage = __instance.LevelSchema.SlimeHeight.Value * 100;
                DataRisingSlime.SlimeSpeedPercentage = __instance.LevelSchema.SlimeSpeed.Value * 100;
            }
            return true;
        }
        #endregion

        #region Slime Data Management
        [HarmonyPatch(typeof(LevelEditorManager), nameof(LevelEditorManager.InstantiateStartGantry)), HarmonyPrefix]
        public static bool DefaultRisingSlimeData(LevelEditorManager __instance)
        {
            // Theorically there's no problem putting these here since InstantiateStartGantry is only called when creating a new round.
            DataRisingSlime.SlimeHeightPercentage = 100;
            DataRisingSlime.SlimeSpeedPercentage = 100;
            return true;
        }

        [HarmonyPatch(typeof(LevelEditorOptionsSingleton), nameof(LevelEditorOptionsSingleton.CurrentSlimeSpeedValue), MethodType.Getter), HarmonyPrefix]
        public static bool SetSlimeSpeedBaseOnRisingSlimeData(out float __result)
        {
            __result = DataRisingSlime.SlimeSpeedPercentage / 100;
            return false;
        }

        [HarmonyPatch(typeof(LevelEditorOptionsSingleton), nameof(LevelEditorOptionsSingleton.CurrentSlimeHeightPercentage), MethodType.Getter), HarmonyPrefix]
        public static bool SetSlimeMAXHeightBasedOnRisingSlimeData(out float __result)
        {
            __result = DataRisingSlime.SlimeHeightPercentage / 100;
            return false;
        }
        #endregion
    }
}
