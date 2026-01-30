using HarmonyLib;
using UnityEngine;

namespace BetterAmongUs.Patches.Gameplay.UI;

[HarmonyPatch]
internal static class MiniMapBehaviourPatch
{
    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowNormalMap))]
    [HarmonyPostfix]
    private static void MapBehaviour_ShowNormalMap_Postfix(MapBehaviour __instance)
        => __instance.ColorControl.SetColor(new Color(0.05f, 0.6f, 1f, 1f));

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowDetectiveMap))]
    [HarmonyPostfix]
    private static void MapBehaviour_ShowDetectiveMap_Postfix(MapBehaviour __instance)
        => __instance.ColorControl.SetColor(new Color(0.05f, 0.6f, 1f, 1f));

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowSabotageMap))]
    [HarmonyPostfix]
    private static void MapBehaviour_ShowSabotageMap_Postfix(MapBehaviour __instance)
        => __instance.ColorControl.SetColor(new Color(1f, 0.3f, 0f, 1f));

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowCountOverlay))]
    [HarmonyPostfix]
    private static void MapBehaviour_ShowCountOverlay_Postfix(MapBehaviour __instance)
        => __instance.ColorControl.SetColor(new Color(0.2f, 0.5f, 0f, 1f));

    [HarmonyPatch(typeof(MapConsole), nameof(MapConsole.Use))]
    [HarmonyPostfix]
    private static void MapConsole_ShowCountOverlay_Postfix()
        => MapBehaviour.Instance.ColorControl.SetColor(new Color(0.2f, 0.5f, 0f, 1f));
}
