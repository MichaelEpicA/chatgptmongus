using BetterAmongUs.Data;
using BetterAmongUs.Helpers;
using BetterAmongUs.Managers;
using BetterAmongUs.Modules;
using BetterAmongUs.Patches.Gameplay.UI.Settings;
using HarmonyLib;
using Hazel;
using InnerNet;

namespace BetterAmongUs.Patches.Client;

[HarmonyPatch]
internal static class InnerNetClientPatch
{
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.SendOrDisconnect))]
    [HarmonyPrefix]
    private static bool InnerNetClient_SendOrDisconnect_Prefix(InnerNetClient __instance, MessageWriter msg)
    {
        NetworkManager.SendToServer(msg);

        return false;
    }

    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.HandleGameData))]
    [HarmonyPrefix]
    private static bool InnerNetClient_HandleGameDataInner_Prefix([HarmonyArgument(0)] MessageReader oldReader)
    {
        NetworkManager.HandleGameData(oldReader);
        return false;
    }

    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.CanBan))]
    [HarmonyPrefix]
    private static bool InnerNetClient_CanBan_Prefix(ref bool __result)
    {
        __result = GameState.IsHost;
        return false;
    }

    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.CanKick))]
    [HarmonyPrefix]
    private static bool InnerNetClient_CanKick_Prefix(ref bool __result)
    {
        __result = GameState.IsHost || GameState.IsInGamePlay && (GameState.IsMeeting || GameState.IsExilling);
        return false;
    }

    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.KickPlayer))]
    [HarmonyPrefix]
    private static void InnerNetClient_KickPlayer_Prefix(ref int clientId, ref bool ban)
    {
        if (ban && BetterGameSettings.UseBanPlayerList.GetBool())
        {
            NetworkedPlayerInfo info = Utils.PlayerFromClientId(clientId).Data;
            BetterDataManager.AddToBanList(info.FriendCode, info.Puid);
        }
    }
}
