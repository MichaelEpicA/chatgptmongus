using BetterAmongUs.Helpers;
using BetterAmongUs.Modules.Support;
using HarmonyLib;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace BetterAmongUs.Patches.Unity;

[HarmonyPatch]
internal static class UnityWebRequestPatch
{
    public static string GetHeader()
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.Append(ModInfo.PLUGIN_VERSION);
        stringBuilder.Append(';');
        stringBuilder.Append(Enum.GetName(ModInfo.ReleaseBuildType));
        stringBuilder.Append(';');
        stringBuilder.Append(ModInfo.IS_HOTFIX);
        stringBuilder.Append('/');
        stringBuilder.Append(ModInfo.HOTFIX_NUM);
        stringBuilder.Append('/');
        stringBuilder.Append(ModInfo.BETA_NUM);

        return stringBuilder.ToString();
    }

    [HarmonyPatch(typeof(UnityWebRequest), nameof(UnityWebRequest.SendWebRequest))]
    [HarmonyPrefix]
    private static void UnityWebRequest_SendWebRequest_Prefix(UnityWebRequest __instance)
    {
        if (BAUModdedSupportFlags.HasFlag(BAUModdedSupportFlags.Disable_BAUHttpHeader)) return;

        var path = new Uri(__instance.url).AbsolutePath;
        if (path.Contains("/api/games"))
        {
            __instance.SetRequestHeader("BAU-Mod", GetHeader());
        }
    }

    [HarmonyPatch(typeof(UnityWebRequest), nameof(UnityWebRequest.SendWebRequest))]
    [HarmonyPostfix]
    private static void UnityWebRequest_SendWebRequest_Postfix(UnityWebRequest __instance, UnityWebRequestAsyncOperation __result)
    {
        if (BAUModdedSupportFlags.HasFlag(BAUModdedSupportFlags.Disable_BAUHttpHeader)) return;

        var path = new Uri(__instance.url).AbsolutePath;
        if (path.Contains("/api/games"))
        {
            __result.add_completed((Action<AsyncOperation>)(_ =>
            {
                if (!HttpUtils.IsSuccess(__instance.responseCode)) return;

                var responseHeader = __instance.GetResponseHeader("BAU-Mod-Processed");

                if (responseHeader != null)
                {
                    Logger_.Log("Connected to a supported Better Among Us matchmaking server");
                }
            }));
        }
    }
}
