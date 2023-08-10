using FG.Common;
using HarmonyLib;

namespace FraggleExpansion.Patches.Reticle
{
    public class ReticleUI
    {
        #region Budget Bar Modifications
        [HarmonyPatch(typeof(LevelEditorResourceBarViewModel), nameof(LevelEditorResourceBarViewModel.TotalPointsText), MethodType.Getter), HarmonyPrefix]
        public static bool ResourceBarTotalUsablePoints(out string __result)
        {
            __result = FraggleExpansionData.RemoveCostAndStock ? "  ∞" : "1000";
            return false;
        }

        [HarmonyPatch(typeof(LevelEditorResourceBarViewModel), nameof(LevelEditorResourceBarViewModel.BuildPointsUsedText), MethodType.Getter), HarmonyPrefix]
        public static bool ResourceBarUsedBuildPoints(out string __result)
        {
            __result = "NA  ";
            return !FraggleExpansionData.RemoveCostAndStock;
        }

        #endregion

        #region Text Patches
        [HarmonyPatch(typeof(LevelEditorObjectInfoViewModel), nameof(LevelEditorObjectInfoViewModel.ObjectsSelectedText), MethodType.Getter), HarmonyPrefix]
        public static bool RemoveMultiselectLimitText(LevelEditorObjectInfoViewModel __instance, out string __result)
        {
            __result = LevelEditor.LevelEditorMultiSelectionHandler.Selection().Count + __instance._localisedStrings.GetString("wle_objectsselected").Replace("<number>", "");
            return false;
        }

        [HarmonyPatch(typeof(LevelEditorPlaceableObject), nameof(LevelEditorPlaceableObject.IsFloorOverlappingFloor), MethodType.Getter), HarmonyPrefix]
        public static bool NeverOverlappingFloor(out bool __result)
        {
            __result = false;
            return false;
        }
        #endregion
    }
}
