using BetterAmongUs.Helpers;
using BetterAmongUs.Modules;
using BetterAmongUs.Mono;
using BetterAmongUs.Patches.Gameplay.UI.Chat;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace BetterAmongUs.Patches.Gameplay.UI;

[HarmonyPatch]
internal static class MeetingHudPatch
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPostfix]
    private static void MeetingHud_Start_Postfix(MeetingHud __instance)
    {
        foreach (var pva in __instance.playerStates)
        {
            var target = Utils.PlayerFromPlayerId(pva.TargetPlayerId);
            pva.gameObject.AddComponent<MeetingInfoDisplay>().Init(target, pva);
        }

        if (!GameState.IsOnlineGame) return;

        // Add host icon to meeting hud
        __instance.ProceedButton.gameObject.transform.localPosition = new(-2.5f, 2.2f, 0);
        __instance.ProceedButton.gameObject.GetComponent<SpriteRenderer>().enabled = false;
        __instance.ProceedButton.GetComponent<PassiveButton>().enabled = false;
        __instance.HostIcon.enabled = true;
        __instance.HostIcon.gameObject.SetActive(true);
        __instance.ProceedButton.gameObject.SetActive(true);
        MeetingHud.Instance.ProceedButton.DestroyTextTranslators();
        UpdateHostIcon();

        Logger_.LogHeader("Meeting Has Started");
    }

    internal static void UpdateHostIcon()
    {
        if (MeetingHud.Instance == null) return;

        PlayerMaterial.SetColors(GameData.Instance.GetHost().Color, MeetingHud.Instance.HostIcon);
        MeetingHud.Instance.ProceedButton.gameObject.GetComponentInChildren<TextMeshPro>().text = string.Format(Translator.GetString("HostInMeeting"), GameData.Instance.GetHost().BetterData().RealName);
    }

    internal static float timeOpen = 0f;

    // Set player meeting info
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    [HarmonyPostfix]
    private static void MeetingHud_Update_Postfix(MeetingHud __instance)
    {
        timeOpen += Time.deltaTime;
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
    [HarmonyPostfix]
    private static void MeetingHud_Close_Postfix()
    {
        timeOpen = 0f;
        Logger_.LogHeader("Meeting Has Ended");

        if (BAUPlugin.ChatInGameplay.Value && !GameState.IsFreePlay && PlayerControl.LocalPlayer.IsAlive())
        {
            ChatPatch.ClearPlayerChats();
        }
    }
}