using HarmonyLib;

namespace FraggleExpansion.Patches
{
    public class Requirements
    {
        [HarmonyPatch(typeof(StandaloneClientInitialisation), nameof(StandaloneClientInitialisation.Awake)), HarmonyPrefix]
        public static bool NothingToSee()
        {
            ExplorerBehaver.Init();
            return true;
        }
    }
}
