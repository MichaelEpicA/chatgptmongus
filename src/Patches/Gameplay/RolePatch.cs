using BetterAmongUs.Mono;
using HarmonyLib;

namespace BetterAmongUs.Patches.Gameplay;

[HarmonyPatch]
internal static class RolePatch
{
    [HarmonyPatch(typeof(NoisemakerRole), nameof(NoisemakerRole.OnDeath))]
    [HarmonyPrefix]
    private static bool NoisemakerRole_NotifyOfDeath_Prefix(NoisemakerRole __instance)
    {
        if (__instance.Player.BetterData().RoleInfo.HasNoisemakerNotify)
        {
            return false;
        }

        __instance.Player.BetterData().RoleInfo.HasNoisemakerNotify = true;

        return true;
    }
}
