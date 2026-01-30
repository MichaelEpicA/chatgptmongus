using BepInEx.Unity.IL2CPP.Utils;
using BetterAmongUs.Helpers;
using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace BetterAmongUs.Patches.Gameplay.UI;

[HarmonyPatch]
internal static class IntroCutscenePatch
{
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
    [HarmonyPostfix]
    private static void IntroCutscene_CoBegin_Postfix(IntroCutscene __instance)
    {
        __instance.StartCoroutine(CoWaitForShowRole(__instance));
    }

    private static IEnumerator CoWaitForShowRole(IntroCutscene __instance)
    {
        while (!__instance.YouAreText.gameObject.active)
        {
            yield return null;
        }

        var introCutscene = __instance;
        Color RoleColor = Utils.HexToColor32(PlayerControl.LocalPlayer.Data.RoleType.GetRoleHex());
        introCutscene.ImpostorText.gameObject.SetActive(false);
        introCutscene.TeamTitle.gameObject.SetActive(false);
        introCutscene.BackgroundBar.material.color = RoleColor;
        introCutscene.BackgroundBar.transform.SetLocalZ(-15);
        introCutscene.transform.Find("BackgroundLayer").transform.SetLocalZ(-16);
        introCutscene.YouAreText.color = RoleColor;
        introCutscene.RoleText.color = RoleColor;
        introCutscene.RoleBlurbText.color = RoleColor;
    }
}