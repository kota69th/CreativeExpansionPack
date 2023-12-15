using FG.Common.LevelEditor.Serialization;
using FG.Common;
using FGClient;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using Wushu.Framework.ExtensionMethods;
using FMODUnity;
using System.Runtime.InteropServices;
using FG.Common.Fraggle;

namespace FraggleExpansion.Patches.Creative
{
    public class FeaturesPatches
    {

        [HarmonyPatch(typeof(LevelEditorManagerAudio), nameof(LevelEditorManagerAudio.StartGameplayMusic)), HarmonyPrefix]
        public static bool CustomMusicOnPublish(LevelEditorManagerAudio __instance)
        {
            if (FraggleExpansionData.CustomTestMusic)
            {
                if (!RuntimeManager.HasBankLoaded(FraggleExpansionData.MusicBankPlayMode))
                {
                    RuntimeManager.LoadBank(FraggleExpansionData.MusicBankPlayMode);
                    RuntimeManager.LoadBank(FraggleExpansionData.MusicBankPlayMode + ".assets");
                }

                AudioLevelEditorStateListener._instance._musicEvent = FraggleExpansionData.MusicEventPlayMode;
                AudioLevelEditorStateListener._instance.OnStartGameplayMusic(new StartGameplayMusic());
            }
            return !FraggleExpansionData.CustomTestMusic;
        }


        [HarmonyPatch(typeof(LevelEditorStateExplore), "DisableState"), HarmonyPrefix]
        public static bool LastPositionDisplayOnReticle(ILevelEditorState nextState)
        {
            if (FraggleExpansionData.LastPostion)
            {
                if (MiscData.CurrentPositionDisplay != null) UnityEngine.Object.Destroy(MiscData.CurrentPositionDisplay);
                MiscData.CurrentPositionDisplay = GameObject.CreatePrimitive(PrimitiveType.Cube);
                MiscData.CurrentPositionDisplay.name = "PositionDisplay";
                MiscData.CurrentPositionDisplay.GetComponent<MeshRenderer>().material = ThemeManager._currentTheme.FilletMaterial;
                MiscData.CurrentPositionDisplay.GetComponent<MeshRenderer>().material.color = new Color(0, 5, 5, 1);
                MiscData.CurrentPositionDisplay.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                UnityEngine.Object.Destroy(MiscData.CurrentPositionDisplay.GetComponent("BoxCollider"));
                MiscData.CurrentPositionDisplay.transform.position = UnityEngine.Object.FindObjectOfType<FallGuysCharacterController>().transform.position;
            }
            UnityEngine.Object.FindObjectOfType<LevelEditorNavigationScreenViewModel>().SetPlayVisible(true);

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LevelEditorStateTest), "Initialise")]
        [HarmonyPatch(typeof(LevelEditorStateExplore), "Initialise")]
        public static bool LastPositionDisplayOnPlayState()
        {
            if (FraggleExpansionData.LastPostion)
            {
                if (MiscData.CurrentPositionDisplay != null) UnityEngine.Object.Destroy(MiscData.CurrentPositionDisplay);
            }

            UnityEngine.Object.FindObjectOfType<LevelEditorNavigationScreenViewModel>().SetPlayVisible(false);

            return true;
        }


        /*
        [HarmonyPatch(typeof(LevelEditorManager), nameof(LevelEditorManager.SetPlayerGameObject)), HarmonyPostfix]
        public static void MainSkinInFraggle(LevelEditorManager __instance, GameObject playerObject)
        {
            if (FraggleExpansionData.UseMainSkinInExploreState)
            {
                var CustomisationSelection = GlobalGameStateClient.Instance.PlayerProfile.CustomisationSelections;
                CustomisationManager.Instance.ApplyCustomisationsToFallGuy(playerObject, CustomisationSelection, -1);
            }
        }
        */

        [HarmonyPatch(typeof(LevelEditorManager), nameof(LevelEditorManager.CanAffordObjectCost)), HarmonyPrefix]
        public static bool CanAffordAnyCost(LevelEditorManager __instance, out bool __result, int cost)
        {
            __result = true;
            
            return !FraggleExpansionData.RemoveCostAndStock;
        }
        [HarmonyPatch(typeof(LevelEditorManager), nameof(LevelEditorManager.IsOverBudget)), HarmonyPrefix]
        public static bool IsNeverOverBudget(LevelEditorManager __instance, out bool __result)
        {
            __result = false;

            return !FraggleExpansionData.RemoveCostAndStock;
        }


        [HarmonyPatch(typeof(LevelEditorPlaceableObject), nameof(LevelEditorPlaceableObject.CanBeClipped)), HarmonyPrefix]
        public static bool CanBeClippedTheCEPWay(LevelEditorPlaceableObject __instance, out bool __result)
        {
            __result = false;
            
            return !LevelEditorManager.Instance.UI._radialDefinition.RadialDefinitions[2].IsToggleOn();
        }

        [HarmonyPatch(typeof(LevelEditorPlaceableObject), nameof(LevelEditorPlaceableObject.CanBeDeleted)), HarmonyPrefix]
        public static bool DeletionForBraindeadStartLine(LevelEditorPlaceableObject __instance, out bool __result)
        {
            bool StartLineValidation = LevelEditorManager.Instance.CostManager.GetCount(__instance.ObjectDataOwner) > 1 && __instance.IsActionValid(LevelEditorPlaceableObject.Action.Delete);
            bool UseStartLineValidation = ThemeManager.CurrentThemeData.ObjectList.GetStartGantry() == __instance.ObjectDataOwner;
            __result = UseStartLineValidation ? StartLineValidation : __instance.IsActionValid(LevelEditorPlaceableObject.Action.Delete);
            return false;
        }
    }

    public class BypassesPatches
    {
        [HarmonyPatch(typeof(LevelEditor.LevelEditorMultiSelectionHandler), nameof(LevelEditor.LevelEditorMultiSelectionHandler.CanSelectMore), MethodType.Getter), HarmonyPrefix]
        public static bool RemoveMaxMultiSelect(out bool __result)
        {
            __result = true;
            return false;
        }

        [HarmonyPatch(typeof(LevelEditorStateReticleBase), nameof(LevelEditorStateReticleBase.CanPlaceSelectedObject)), HarmonyPrefix]
        public static bool Clipping(LevelEditorStateReticleBase __instance, out bool __result)
        {
            __result = true;
            return !LevelEditorManager.Instance.UI._radialDefinition.RadialDefinitions[2].IsToggleOn();
        }

        [HarmonyPatch(typeof(LevelLoader), nameof(LevelLoader.LoadObjects)), HarmonyPostfix]
        public static void BoundsOnExistingRound(LevelLoader __instance, Il2CppReferenceArray<UGCObjectDataSchema> schemas)
        {
            if (FraggleExpansionData.BypassBounds && FraggleCommonManager.Instance.IsInLevelEditor)
                LevelEditorManager.Instance.MapPlacementBounds = new Bounds(LevelEditorManager.Instance.MapPlacementBounds.center, new Vector3(100000, 100000, 100000));
        }
    }

    public class MainFeaturePatches
    {
        [HarmonyPatch(typeof(LevelEditorDrawableData), nameof(LevelEditorDrawableData.ApplyScaleToObject)), HarmonyPrefix]
        public static bool FixCheckpointZoneWithPainterScaling(LevelEditorDrawableData __instance, bool subObj = false)
        {
            // Basically there's issues if the semantic type is CheckpointFloor, but if it's not then the size of the Checkpoint Collider is not changed, so we do it before scaling the object here
            if (__instance.FloorType == LevelEditorDrawableData.DrawableSemantic.FloorObject && __instance.gameObject.GetComponent<LevelEditorCheckpointFloorData>() && __instance._checkpointZone != null)
                __instance._checkpointZone.SetCheckpointZoneColliderScale(__instance.GetShaderScale(), LevelEditorDrawableData.DrawableSemantic.FloorObject);

            return true;
        }

        [HarmonyPatch(typeof(LevelEditorCheckpointFloorData), nameof(LevelEditorCheckpointFloorData.UpdateChevron)), HarmonyPrefix]
        public static bool FixChevronScaling(ref Vector3 scale)
        {
            // light work no reaction
            scale = scale.Abs();
            return true;
        }
    }
}
