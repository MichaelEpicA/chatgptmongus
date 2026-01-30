using AmongUs.Data;
using BetterAmongUs.Modules;
using BetterAmongUs.Modules.Support;
using Discord;
using HarmonyLib;

namespace BetterAmongUs.Patches.Client.Discord;

[HarmonyPatch]
internal static class DiscordPatch
{
    private static string lobbycode = "";
    private static string region = "";

    [HarmonyPatch(typeof(ActivityManager), nameof(ActivityManager.UpdateActivity))]
    [HarmonyPrefix]
    private static void ActivityManager_UpdateActivity_Prefix(Activity activity)
    {
        if (BAUModdedSupportFlags.HasFlag(BAUModdedSupportFlags.Disable_DiscordRP)) return;
        if (activity == null) return;

        string details = $"BAU {BAUPlugin.GetVersionText()}";
        activity.Details = details;

        if (activity.State == "In Menus") return;

        try
        {
            if (!DataManager.Settings.Gameplay.StreamerMode)
            {
                UpdateRegionAndLobbyCode();
                if (!string.IsNullOrEmpty(lobbycode) && !string.IsNullOrEmpty(region))
                {
                    if (GameState.IsNormalGame)
                        details = $"BAU - {lobbycode} ({region})";
                    else if (GameState.IsHideNSeek)
                        details = $"BAU Hide & Seek - {lobbycode} ({region})";
                }
            }
            else
            {
                if (GameState.IsHideNSeek)
                    details = $"BAU v{ModInfo.PLUGIN_VERSION} - Hide & Seek";
            }
        }
        catch
        {
        }

        activity.Details = details;
    }

    private static void UpdateRegionAndLobbyCode()
    {
        if (GameState.IsLobby)
        {
            if (GameStartManager.Instance?.GameRoomNameCode != null)
            {
                lobbycode = GameStartManager.Instance.GameRoomNameCode.text;
                region = ServerManager.Instance.CurrentRegion.Name;
                region = region switch
                {
                    "North America" => "NA",
                    "Europe" => "EU",
                    "Asia" => "AS",
                    _ when region.Contains("MNA") => "MNA",
                    _ when region.Contains("MEU") => "MEU",
                    _ when region.Contains("MAS") => "MAS",
                    _ => region
                };
            }
        }
    }
}
