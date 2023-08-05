using FG.Common;
using FGClient.UI;
using FGClient;
using HarmonyLib;
using FraggleExpansion.Patches.Gamemodes;
using UnityEngine;
using FG.Common.CMS;
using FG.Common.Fraggle;
using UnhollowerBaseLib;
using System.Collections;
using BepInEx.IL2CPP.Utils.Collections;
using ScriptableObjects;

namespace FraggleExpansion
{
    public class SlimeGamemodesManager
    {
        public static SlimeGamemodesManager Instance;
        Harmony Harmony = Main.Instance._Harmony;
        public bool SlimeSetupDone = false;
        public static PlaceableObjectData SurvivalStart;
        public static LevelEditorPlaceableObject CurrentStart;

        public SlimeGamemodesManager()
        {
            Harmony.PatchAll(typeof(RisingSlimeModePatches));
            Harmony.PatchAll(typeof(SurvivalModePatches));
            Harmony.PatchAll(typeof(SlimeGamemodesPatches));

            Instance = this;
        }

        public void LoadGamemodes() => Main.Instance.StartCouroutineIl2Cpp(IntializeGamemodesEnumerator().WrapToIl2Cpp());

        public void PublishState()
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

        public void InitializeSurvivalMode(out GameModeData SurvivalGamemode)
        {
            var SurvivalMode = GameModeManager.GetAvailableGameMode("GAMEMODE_SURVIVAL");

            SurvivalMode.DefaultMinCapacity = 1;
            SurvivalMode.menuPriority = 100;
            SurvivalMode.UpperMinCapacity = 1;
            SurvivalMode.LowerMaxCapacity = 1;
            SurvivalMode.LowerMinCapacity = 1;
            SurvivalMode.Cast<GameModeDataSurvival>().DefaultNumberOfWinners = 20;
            SurvivalMode._startFloorHeight = -45;
            CMSLoader.Instance._localisedStrings._localisedStrings["wle_mode_4"] = "SURVIVAL";

            SurvivalGamemode = SurvivalMode;
        }

        public void InitializeRisingSlimeMode(out GameModeData RisingSlimeGamemode)
        {
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

            var Data = new FraggleCommonManager.CarouselItemData();
            CMSLoader.Instance._localisedStrings._localisedStrings["wle_mode_2"] = "RISING SLIME";
            Data.descriptionKey = "wle_mode_info_2";
            Data.titleKey = "wle_mode_2";
            Data.ID = "GAMEMODE_SLIMECLIMB";

            // Rising Slime by Crispy Squid (amazing icon)
            GameModeSlime.carouselItemData = Data;
            Data.sprite = Main.Instance.MakeOutAnIcon("https://raw.githubusercontent.com/kota69th/FranticExplorer-Bundles/main/risingslime.png", 1280, 1280);

            RisingSlimeGamemode = GameModeSlime;
        }

        System.Collections.IEnumerator IntializeGamemodesEnumerator()
        {
            yield return new WaitForFixedUpdate();
            if (SlimeSetupDone) yield break;
            while (!UnityEngine.Object.FindObjectOfType<MainMenuManager>().IsOnMainMenu)
                yield return null;

            while (CMSLoader.Instance.State != CMSLoaderState.Ready)
                yield return null;

            InitializeSurvivalMode(out GameModeData Survival);
            InitializeRisingSlimeMode(out GameModeData RisingSlime);

            SlimeSetupDone = true;
            Il2CppReferenceArray<GameModeData> Datas = new Il2CppReferenceArray<GameModeData>(3);
            Datas[0] = GameModeManager.GameModeConfigs[0];
            Datas[1] = Survival;
            Datas[2] = RisingSlime;

            GameModeManager._allGameModeDatas = Datas;
            GameModeManager._availableGameModeDatas = Datas;
            GameModeManager.InitializeGameModeData(Datas);
        }
    }
}
