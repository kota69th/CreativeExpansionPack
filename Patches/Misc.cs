using FG.Common;
using FGClient;
using HarmonyLib;
using ScriptableObjects;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.EventSystems;

namespace FraggleExpansion.Patches
{
    public class MiscPatches
    {
        [HarmonyPatch(typeof(LevelEditorModeElementViewModel), nameof(LevelEditorModeElementViewModel.OnClicked)), HarmonyPrefix]
        public static bool StartGentryForSurvival(LevelEditorModeElementViewModel __instance, BaseEventData eventData)
        {
            if (__instance._selectionType == "GAMEMODE_SURVIVAL" || __instance._selectionType == "GAMEMODE_SLIMECLIMB")
                LevelEditorManagerUI.ShowGenericPopup("wle_creative_expansion_confirmation_upcoming_title", "wle_creative_expansion_confirmation_upcoming_desc", "wle_creative_expansion_confirmation_upcoming_confirm", modelType: FGClient.UI.UIModalMessage.ModalType.MT_OK, type: FGClient.UI.PopupInteractionType.Warning);
            return true;
        }
    }
}
