using HarmonyLib;
using System.Reflection;

namespace BetterAmongUs.Patches.Unity;

[HarmonyPatch]
internal static class StandaloneSteamPatch
{
    private static readonly Type? _type = Type.GetType("Steamworks.SteamAPI, Assembly-CSharp-firstpass", false);

    [HarmonyPrepare]
    private static bool Prepare()
    {
        return _type != null;
    }

    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(_type, "RestartAppIfNecessary");
    }

    private static bool Prefix(out bool __result)
    {
        const string file = "steam_appid.txt";

        if (!File.Exists(file))
        {
            File.WriteAllText(file, "945360");
        }

        return __result = false;
    }
}
