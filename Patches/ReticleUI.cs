using AsmResolver.PE.DotNet.Cil;
using FG.Common;
using HarmonyLib;
using UnityEngine;

namespace FraggleExpansion.Patches.Reticle
{
    public class ReticleUI
    {
        #region Budget Bar Modifications
        [HarmonyPatch(typeof(LevelEditorResourceBarViewModel), nameof(LevelEditorResourceBarViewModel.TotalPointsText), MethodType.Getter), HarmonyPrefix]
        public static bool ResourceBarTotalUsablePoints(out string __result)
        {
            __result = FraggleExpansionData.RemoveCostAndStock ? "  ∞" : CMSGlobalSettings.UGCBudgetLimit.ToString();
            return false;
        }

        [HarmonyPatch(typeof(LevelEditorCarrouselItemViewModel), nameof(LevelEditorCarrouselItemViewModel.UpdateCostsStocks)), HarmonyPrefix]
        public static bool ItemCostNStockDisplay(LevelEditorCarrouselItemViewModel __instance)
        {
            __instance.IsCostDynamic = false;
            __instance.SetCost(0, __instance._placeableObject, true);
            __instance.SetStock(-1, false);

            return !FraggleExpansionData.RemoveCostAndStock;
        }

        [HarmonyPatch(typeof(LevelEditorCarrouselItemViewModel), nameof(LevelEditorCarrouselItemViewModel.UpdateObjectOverlapping)), HarmonyPrefix]
        public static bool ItemOverlapDisplay(LevelEditorCarrouselItemViewModel __instance)
        {
            __instance.CanObjectOverlap = false;

            return !FraggleExpansionData.CanClipObjects;
        }

        [HarmonyPatch(typeof(LevelEditorManagerUI), nameof(LevelEditorManagerUI.DisplayHudMessage)), HarmonyPrefix]
        public static bool RemoveBudgetNotice(string key, bool shouldAnimateEnter = true, bool shouldAnimateLeave = true)
        { 
            return key != "Over Budget";
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
        } // oops
        #endregion
    }
}
