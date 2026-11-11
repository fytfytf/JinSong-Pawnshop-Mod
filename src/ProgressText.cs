using HarmonyLib;

namespace JinSongPoc
{
    // Native callers continue to select the key and calculate every formatting argument.
    // Raw/provider MOD perk text never enters these native table helpers.
    internal static class ProgressText
    {
        private static void Replace(string key, object[] arguments, ref string result)
        {
            if (Mod.Chinese && key != null)
                result = Mod.Text("progress_" + key, arguments) ?? result;
        }

        [HarmonyPatch(typeof(LocHelper), "GetLocalizedPerkTable")]
        private static class StartingTraits
        {
            private static void Postfix(string __0, object[] __1, ref string __result)
            { Replace(__0, __1, ref __result); }
        }

        [HarmonyPatch(typeof(LocHelper), "GetLocalizedRepPerkTable")]
        private static class ReputationTraits
        {
            private static void Postfix(string __0, object[] __1, ref string __result)
            { Replace(__0, __1, ref __result); }
        }

        // Newer native starting traits use the public fallback helper instead of the table wrapper.
        [HarmonyPatch(typeof(LocHelper), "GetLocalizedOrDefault")]
        private static class StartingTraitFallback
        {
            private static void Postfix(string __0, string __1, object[] __3, ref string __result)
            {
                if (__0 == "PerkTable") Replace(__1, __3, ref __result);
            }
        }
    }
}
