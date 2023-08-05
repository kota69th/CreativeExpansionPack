using HarmonyLib;

namespace FraggleExpansion.Patches.Reticle
{
    public class ReticleUI
    {
        #region Budget Bar Modifications
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

        #endregion

        #region Multiselect Text Patch
        [HarmonyPatch(typeof(LevelEditorObjectInfoViewModel), nameof(LevelEditorObjectInfoViewModel.ObjectsSelectedText), MethodType.Getter), HarmonyPrefix]
        public static bool RemoveMultiselectLimitText(LevelEditorObjectInfoViewModel __instance, out string __result)
        {
            __result = LevelEditor.LevelEditorMultiSelectionHandler.Selection().Count + __instance._localisedStrings.GetString("wle_objectsselected").Replace("<number>", "");
            return false;
        }
        #endregion
    }
}
