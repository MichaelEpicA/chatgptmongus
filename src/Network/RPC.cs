using BetterAmongUs.Enums;
using BetterAmongUs.Helpers;
using BetterAmongUs.Modules;
using BetterAmongUs.Mono;
using Hazel;

namespace BetterAmongUs.Network;

/// <summary>
/// Handles custom RPC (Remote Procedure Call) messages for BetterAmongUs.
/// </summary>
internal static class RPC
{
    /// <summary>
    /// Processes custom RPC messages received from other players.
    /// </summary>
    /// <param name="player">The player who sent the RPC.</param>
    /// <param name="callId">The ID of the RPC call.</param>
    /// <param name="oldReader">The message reader containing the RPC data.</param>
    /// <remarks>
    /// Handles both defined custom RPCs and protects against unknown RPCs in modded lobbies.
    /// </remarks>
    internal static void HandleCustomRPC(PlayerControl player, byte callId, MessageReader oldReader)
    {
        if (player == null || player.IsLocalPlayer() || player.Data == null || Enum.IsDefined(typeof(RpcCalls), callId)) return;

        if (Enum.IsDefined(typeof(CustomRPC), (int)unchecked(callId)))
        {
            MessageReader reader = MessageReader.Get(oldReader);

            switch (callId)
            {
                /*
                case (byte)CustomRPC.LegacyBetterCheck:
                    {
                        var SetBetterUser = reader.ReadBoolean();
                        var Signature = reader.ReadString();
                        var Version = reader.ReadString();
                        var IsVerified = Signature == Main.ModSignature.ToString();

                        if (string.IsNullOrEmpty(Signature) || string.IsNullOrEmpty(Version))
                        {
                            BetterNotificationManager.NotifyCheat(player, $"Invalid Action RPC: {Enum.GetName((CustomRPC)callId)} called with invalid info");
                            break;
                        }

                        player.BetterData().IsBetterUser = SetBetterUser;

                        if (IsVerified)
                        {
                            player.BetterData().IsVerifiedBetterUser = true;
                        }

                        Logger.Log($"Received better user RPC from: {player.Data.PlayerName}:{player.Data.FriendCode}:{Utils.GetHashPuid(player)} - " +
                            $"BetterUser: {SetBetterUser} - " +
                            $"Version: {Version} - " +
                            $"Verified: {IsVerified} - " +
                            $"Signature: {Signature}");

                        Utils.DirtyAllNames();
                    }
                    break;
                */
                case (byte)CustomRPC.SendSecretToPlayer:
                    {
                        player.BetterData().HandshakeHandler.HandleSecretFromSender(reader);
                    }
                    break;
                case (byte)CustomRPC.CheckSecretHashFromPlayer:
                    {
                        player.BetterData().HandshakeHandler.HandleSecretHashFromPlayer(reader);
                    }
                    break;
            }
        }
        else if (!Enum.IsDefined(typeof(CustomRPC), (int)unchecked(callId)))
        {
            try
            {
                if (!GameState.IsHost)
                {
                    if (player.IsHost())
                    {
                        var Icon = Translator.GetString("BAUMark");
                        var BAU = $"<color=#278720>{Icon}</color><color=#0ed400><b>{Translator.GetString("BAU")}</b></color><color=#278720>{Icon}</color>";
                        Utils.DisconnectSelf(string.Format(Translator.GetString("ModdedLobbyMsg"), BAU));
                    }
                }
            }
            catch { }
        }
    }
}