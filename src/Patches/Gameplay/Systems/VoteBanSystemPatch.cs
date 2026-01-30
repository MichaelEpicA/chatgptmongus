using BetterAmongUs.Helpers;
using BetterAmongUs.Managers;
using BetterAmongUs.Modules;
using BetterAmongUs.Modules.Support;
using HarmonyLib;

namespace BetterAmongUs.Patches.Gameplay.Systems;

[HarmonyPatch]
internal static class VoteBanSystemPatch
{
    private static readonly Dictionary<VoteBanSystem, List<(int ClientId, (ushort HashPuid, string FriendCode) Voter)>> _voteData = [];

    private static bool DoLog;

    [HarmonyPatch(typeof(VoteBanSystem), nameof(VoteBanSystem.AddVote))]
    [HarmonyPrefix]
    private static bool VoteBanSystem_AddVote_Prefix(VoteBanSystem __instance, int srcClient, int clientId)
    {
        if (BAUModdedSupportFlags.HasFlag(BAUModdedSupportFlags.Disable_Anticheat)) return true;

        if (!GameState.IsHost)
        {
            DoLog = true;
            return true;
        }

        var client = Utils.ClientFromClientId(srcClient);
        if (client == null) return false;

        // Skip if host client
        if (client.Id == AmongUsClient.Instance.GetHost().Id)
        {
            return true;
        }

        void TryFlagPlayer()
        {
            var player = client.Character;
            if (player != null)
            {
                BetterNotificationManager.NotifyCheat(player, string.Format(Translator.GetString("AntiCheat.InvalidLobbyRPC"), "VoteKick"));
            }
        }

        if (GameState.IsLobby)
        {
            TryFlagPlayer();
            return false;
        }

        if (!_voteData.TryGetValue(__instance, out var voters))
        {
            _voteData.Clear();
            _voteData[__instance] = voters = [];
        }

        if (string.IsNullOrEmpty(client.ProductUserId) && string.IsNullOrEmpty(client.FriendCode))
        {
            DoLog = true;
            return true;
        }

        var clientHash = Utils.GetHashUInt16(client.ProductUserId);

        foreach (var (targetClientId, (existingHash, existingFriendCode)) in voters)
        {
            if (targetClientId != clientId)
                continue;

            bool isDuplicateVote = existingHash == clientHash ||
                                  !string.IsNullOrEmpty(client.FriendCode) &&
                                   existingFriendCode == client.FriendCode;

            if (isDuplicateVote)
            {
                return false;
            }
        }

        voters.Add((clientId, (clientHash, client.FriendCode)));
        DoLog = true;
        return true;
    }


    [HarmonyPatch(typeof(VoteBanSystem), nameof(VoteBanSystem.AddVote))]
    [HarmonyPostfix]
    private static void VoteBanSystem_AddVote_Postfix(VoteBanSystem __instance, int srcClient, int clientId)
    {
        if (BAUModdedSupportFlags.HasFlag(BAUModdedSupportFlags.Disable_Anticheat)) return;

        if (DoLog)
        {
            LogVote(__instance, srcClient, clientId);
            DoLog = false;
        }
    }

    private static void LogVote(VoteBanSystem voteBanSystem, int srcClient, int clientId)
    {
        var src = Utils.ClientFromClientId(srcClient);
        var client = Utils.ClientFromClientId(clientId);

        int currentVotes = 0;
        int maxVotes = 0;

        if (voteBanSystem.Votes.TryGetValue(clientId, out var votes))
        {
            currentVotes = votes.Count(v => v != 0);
            maxVotes = votes.Length;
        }

        Logger_.InGame(
            $"{src.Character?.GetPlayerNameAndColor() ?? src.PlayerName} " +
            $"voted to kick {client.Character?.GetPlayerNameAndColor() ?? client.PlayerName} " +
            $"<#6F6F6F>(</color><#FFFFFF>{currentVotes}</color><#6F6F6F>/</color><#FFFFFF>{maxVotes}</color><#6F6F6F>)</color>"
        );
    }
}