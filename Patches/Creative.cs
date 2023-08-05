using FG.Common.LevelEditor.Serialization;
using FG.Common;
using FGClient;
using HarmonyLib;
using UnhollowerBaseLib;
using UnityEngine;
using Wushu.Framework.ExtensionMethods;
using FMODUnity;

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

                AudioLevelEditorStateListener._instance._randomMusicEvent = FraggleExpansionData.MusicEventPlayMode;
                AudioLevelEditorStateListener._instance.OnStartGameplayMusic(new StartGameplayMusic());
            }
            return !FraggleExpansionData.CustomTestMusic;
        }

        [HarmonyPatch(typeof(LevelEditorStateExplore), nameof(LevelEditorStateExplore.DisableState)), HarmonyPrefix]
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
                MiscData.CurrentPositionDisplay.GetComponent<BoxCollider>().isTrigger = true;
                MiscData.CurrentPositionDisplay.transform.position = UnityEngine.Object.FindObjectOfType<FallGuysCharacterController>().transform.position;
            }
            UnityEngine.Object.FindObjectOfType<LevelEditorNavigationScreenViewModel>().SetPlayVisible(true);

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LevelEditorStateTest), nameof(LevelEditorStateTest.Initialise))]
        [HarmonyPatch(typeof(LevelEditorStateExplore), nameof(LevelEditorStateExplore.Initialise))]
        public static bool LastPositionDisplayOnPlayState()
        {
            if (FraggleExpansionData.LastPostion)
            {
                if (MiscData.CurrentPositionDisplay != null) UnityEngine.Object.Destroy(MiscData.CurrentPositionDisplay);
            }

            UnityEngine.Object.FindObjectOfType<LevelEditorNavigationScreenViewModel>().SetPlayVisible(false);

            return true;
        }

        [HarmonyPatch(typeof(LevelEditorManager), nameof(LevelEditorManager.InitialiseLocalCharacter)), HarmonyPrefix]
        public static bool MainSkinInFraggle(LevelEditorManager __instance, GameObject playerGameObject, out FallGuysCharacterController characterController, out ClientPlayerUpdateManager playerUpdateManager)
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
            return !FraggleExpansionData.CanClipObjects;
        }

        [HarmonyPatch(typeof(LevelLoader), nameof(LevelLoader.LoadObjects)), HarmonyPostfix]
        public static void BoundsOnExistingRound(LevelLoader __instance, Il2CppReferenceArray<UGCObjectDataSchema> schemas)
        {
            if (FraggleExpansionData.BypassBounds)
                LevelEditorManager.Instance.MapPlacementBounds = new Bounds(LevelEditorManager.Instance.MapPlacementBounds.center, new Vector3(100000, 100000, 100000));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ThemeManager), nameof(ThemeManager.IsDevTheme), MethodType.Getter)]
        [HarmonyPatch(typeof(ThemeData), nameof(ThemeData.IsDev), MethodType.Getter)]
        public static bool AllDevelopperTheme(bool __result)
        {
            __result = true;
            return false;
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
            scale = scale.Abs();
            return true;
        }
    }
}
