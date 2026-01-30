using AmongUs.GameOptions;
using BetterAmongUs.Helpers;
using BetterAmongUs.Modules;
using BetterAmongUs.Mono;
using BetterAmongUs.Patches.Gameplay.UI.Settings;
using HarmonyLib;
using Hazel;

namespace BetterAmongUs.Patches.Gameplay.Managers;

[HarmonyPatch]
internal static class RoleManagerPatch
{
    internal static Dictionary<string, int> ImpostorMultiplier = []; // HashPuid, Multiplier
    private static readonly Random random = new();

    static readonly Func<InnerNet.ClientData, bool> clientCheck = (clientData) =>
    {
        return clientData?.BetterData()?.IsVerifiedBetterUser != true;
    };

    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SetRole))]
    [HarmonyPrefix]
    private static void RoleManager_SetRole_Prefix(RoleManager __instance, PlayerControl targetPlayer, RoleTypes roleType)
    {
        if (RoleManager.IsGhostRole(roleType))
        {
            if (!RoleManager.IsGhostRole(targetPlayer.Data.RoleType))
            {
                targetPlayer.BetterData().RoleInfo.DeadDisplayRole = targetPlayer.Data.RoleType;
            }
        }
    }

    // Better role algorithm
    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    [HarmonyPrefix]
    private static bool RoleManager_SelectRoles_Prefix()
    {
        if (!GameState.IsHideNSeek)
        {
            RegularBetterRoleAssignment();
        }
        else
        {
            HideAndSeekBetterRoleAssignment();
        }

        return false;
    }

    internal static void RegularBetterRoleAssignment()
    {
        Logger_.LogHeader($"Better Role Assignment Has Started", "RoleManager");

        // Set roles up
        foreach (var addplayer in BAUPlugin.AllPlayerControls.Where(pc => !ImpostorMultiplier.ContainsKey(Utils.GetHashPuid(pc))))
            ImpostorMultiplier[Utils.GetHashPuid(addplayer)] = 0;

        int NumImpostors = GameOptionsManager.Instance.CurrentGameOptions.NumImpostors;

        int NumPlayers = BAUPlugin.AllPlayerControls.Count;

        var impostorLimits = new Dictionary<int, int>
        {
            { 3, 1 },
            { 5, 2 },
            { 7, 3 }
        };

        foreach (var limit in impostorLimits)
        {
            if (NumPlayers <= limit.Key)
            {
                NumImpostors = Math.Min(NumImpostors, limit.Value);
                break;
            }
        }

        List<PlayerControl> Impostors = [];
        List<PlayerControl> Crewmates = [];

        Dictionary<RoleTypes, int> ImpostorRoles = new() // Role, Amount
        {
            { RoleTypes.Shapeshifter, 0 },
            { RoleTypes.Phantom, 0 },
            { RoleTypes.Viper, 0 }
        };

        Dictionary<RoleTypes, int> CrewmateRoles = new() // Role, Amount
        {
            { RoleTypes.Engineer, 0 },
            { RoleTypes.Scientist, 0 },
            { RoleTypes.Tracker, 0 },
            { RoleTypes.Noisemaker, 0 },
            { RoleTypes.Detective, 0 }
        };

        List<RoleTypes> Roles = [.. ImpostorRoles.Keys, .. CrewmateRoles.Keys];

        foreach (RoleTypes role in Roles)
        {
            if (IsImpostorRole(role))
                ImpostorRoles[role] = GameOptionsManager.Instance.CurrentGameOptions.RoleOptions.GetNumPerGame(role);
            else
                CrewmateRoles[role] = GameOptionsManager.Instance.CurrentGameOptions.RoleOptions.GetNumPerGame(role);
        }

        // Get players in random order
        List<PlayerControl> players = [.. BAUPlugin.AllPlayerControls.Where(player => !Impostors.Contains(player) && !Crewmates.Contains(player) && player.roleAssigned == false)];

        int n = players.Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            PlayerControl value = players[k];
            players[k] = players[n];
            players[n] = value;
        }

        // Assign roles
        foreach (PlayerControl pc in players)
        {
            if (pc == null || pc.roleAssigned == true) continue;

            if (Impostors.Count < NumImpostors && RNG() > ImpostorMultiplier[Utils.GetHashPuid(pc)])
            {
                var impRoles = ImpostorRoles.Shuffle();
                foreach (var kvp in impRoles)
                {
                    if (RNG() <= GameOptionsManager.Instance.CurrentGameOptions.RoleOptions.GetChancePerGame(kvp.Key) && kvp.Value > 0)
                    {
                        ImpostorMultiplier[Utils.GetHashPuid(pc)] += 15;
                        ImpostorRoles[kvp.Key]--;
                        Impostors.Add(pc);
                        pc.RpcSetRole(kvp.Key);

                        // Desync role to other clients to prevent revealing the true role
                        if (BetterGameSettings.DesyncRoles.GetBool())
                        {
                            if (kvp.Key is not RoleTypes.Phantom or RoleTypes.Viper)
                            {
                                List<MessageWriter> messageWriter = AmongUsClient.Instance.StartRpcDesync(pc.NetId, (byte)RpcCalls.SetRole, SendOption.None, pc.GetClientId(), clientCheck);
                                messageWriter.ForEach(mW => mW.Write((ushort)RoleTypes.Impostor));
                                messageWriter.ForEach(mW => mW.Write(false));
                                AmongUsClient.Instance.FinishRpcDesync(messageWriter);
                            }
                        }

                        Logger_.LogPrivate($"Assigned {kvp.Key.GetRoleName()} role to {pc.Data.PlayerName}", "RoleManager");
                        break;
                    }
                }

                if (!Impostors.Contains(pc))
                {
                    ImpostorMultiplier[Utils.GetHashPuid(pc)] += 15;
                    Impostors.Add(pc);
                    pc.RpcSetRole(RoleTypes.Impostor);
                    Logger_.LogPrivate($"Assigned {RoleTypes.Impostor.GetRoleName()} role to {pc.Data.PlayerName}", "RoleManager");
                }
            }
            else
            {
                var crewRoles = CrewmateRoles.Shuffle();
                foreach (var kvp in crewRoles)
                {
                    if (RNG() <= GameOptionsManager.Instance.CurrentGameOptions.RoleOptions.GetChancePerGame(kvp.Key) && kvp.Value > 0)
                    {
                        ImpostorMultiplier[Utils.GetHashPuid(pc)] = 0;
                        CrewmateRoles[kvp.Key]--;
                        Crewmates.Add(pc);
                        pc.RpcSetRole(kvp.Key);

                        // Desync role to other clients to prevent revealing the true role
                        if (BetterGameSettings.DesyncRoles.GetBool())
                        {
                            if (kvp.Key is not RoleTypes.Noisemaker)
                            {
                                List<MessageWriter> messageWriter = AmongUsClient.Instance.StartRpcDesync(pc.NetId, (byte)RpcCalls.SetRole, SendOption.None, pc.GetClientId(), clientCheck);
                                messageWriter.ForEach(mW => mW.Write((ushort)RoleTypes.Crewmate));
                                messageWriter.ForEach(mW => mW.Write(false));
                                AmongUsClient.Instance.FinishRpcDesync(messageWriter);
                            }
                        }

                        Logger_.LogPrivate($"Assigned {kvp.Key.GetRoleName()} role to {pc.Data.PlayerName}", "RoleManager");
                        break;
                    }
                }

                if (!Crewmates.Contains(pc))
                {
                    ImpostorMultiplier[Utils.GetHashPuid(pc)] = 0;
                    Crewmates.Add(pc);
                    pc.RpcSetRole(RoleTypes.Crewmate);
                    Logger_.LogPrivate($"Assigned {RoleTypes.Crewmate.GetRoleName()} role to {pc.Data.PlayerName}", "RoleManager");
                }
            }
        }

        Logger_.LogHeader($"Better Role Assignment Has Finished", "RoleManager");
    }

    internal static void HideAndSeekBetterRoleAssignment()
    {
        Logger_.LogHeader($"Better Role Assignment Has Started", "RoleManager");

        int NumImpostors = BetterGameSettings.HideAndSeekImpNum?.GetInt() ?? 1;

        if (NumImpostors > BAUPlugin.AllPlayerControls.Count)
            NumImpostors = BAUPlugin.AllPlayerControls.Count;

        List<NetworkedPlayerInfo> Impostors = [];
        List<NetworkedPlayerInfo> Crewmates = [];
        List<NetworkedPlayerInfo> CrewAndImps() => [.. Impostors, .. Crewmates];

        // Set imp from settings
        int[] betterImpostorSettings =
        [
            GameOptionsManager.Instance.currentHideNSeekGameOptions.ImpostorPlayerID,
            BetterGameSettingsTemp.HideAndSeekImp2?.GetInt() ?? -1,
            BetterGameSettingsTemp.HideAndSeekImp3?.GetInt() ?? -1,
            BetterGameSettingsTemp.HideAndSeekImp4?.GetInt() ?? -1,
            BetterGameSettingsTemp.HideAndSeekImp5?.GetInt() ?? -1
        ];

        for (int i = 0; i < NumImpostors; i++)
        {
            int tempSetImpostor = betterImpostorSettings[i];

            if (tempSetImpostor >= 0)
            {
                var player = Utils.PlayerFromPlayerId(tempSetImpostor);
                if (player != null)
                {
                    if (Impostors.Count < NumImpostors)
                    {
                        Impostors.Add(player.Data);
                        Logger_.LogPrivate($"Settings Assigned {RoleTypes.Impostor.GetRoleName()} role to {player.Data.PlayerName}", "RoleManager");
                    }
                }
            }
        }

        // Get players in random order
        List<PlayerControl> players = [.. BAUPlugin.AllPlayerControls.Where(player => !CrewAndImps().Contains(player.Data))];

        int n = players.Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            PlayerControl value = players[k];
            players[k] = players[n];
            players[n] = value;
        }

        // Assign roles
        foreach (PlayerControl pc in players)
        {
            if (pc == null || pc.roleAssigned == true) continue;

            if (Impostors.Count < NumImpostors)
            {
                Impostors.Add(pc.Data);
                Logger_.LogPrivate($"Assigned {RoleTypes.Impostor.GetRoleName()} role to {pc.Data.PlayerName}", "RoleManager");
            }
            else
            {
                Crewmates.Add(pc.Data);
                Logger_.LogPrivate($"Assigned {RoleTypes.Engineer.GetRoleName()} role to {pc.Data.PlayerName}", "RoleManager");
            }
        }

        foreach (var data in Impostors)
        {
            var player = Utils.PlayerFromPlayerId(data.PlayerId);
            player.RpcSetRole(RoleTypes.Impostor);
        }

        foreach (var data in Crewmates)
        {
            var player = Utils.PlayerFromPlayerId(data.PlayerId);
            player.RpcSetRole(RoleTypes.Engineer);
        }

        Logger_.LogHeader($"Better Role Assignment Has Finished", "RoleManager");
    }

    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.AssignRoleOnDeath))]
    [HarmonyPrefix]
    internal static bool RoleManager_AssignRoleOnDeath_Prefix(PlayerControl player)
    {
        Dictionary<RoleTypes, int> GhostRoles = new() // Role, Amount
        {
            { RoleTypes.GuardianAngel, 0 },
        };

        List<RoleTypes> Roles = [.. GhostRoles.Keys];

        foreach (RoleTypes role in Roles)
        {
            GhostRoles[role] = GameOptionsManager.Instance.CurrentGameOptions.RoleOptions.GetNumPerGame(role);
        }

        foreach (var allDeadPlayers in BAUPlugin.AllPlayerControls.Where(pc => !pc.IsAlive()))
        {
            for (int i = 0; i < Roles.Count; i++)
            {
                if (allDeadPlayers.Is(Roles[i]))
                {
                    GhostRoles[Roles[i]]--;
                }
            }
        }

        var ghostRoles = GhostRoles.Shuffle();

        foreach (var kvp in ghostRoles)
        {
            if (player.IsImpostorTeam() && kvp.Key is RoleTypes.GuardianAngel) continue;

            if (kvp.Value > 0 && RNG() <= GameOptionsManager.Instance.CurrentGameOptions.RoleOptions.GetChancePerGame(kvp.Key))
            {
                player.RpcSetRole(kvp.Key);

                // Desync role to other clients to prevent revealing the true role
                if (BetterGameSettings.DesyncRoles.GetBool())
                {
                    List<MessageWriter> messageWriter = AmongUsClient.Instance.StartRpcDesync(player.NetId, (byte)RpcCalls.SetRole, SendOption.None, player.GetClientId(), clientCheck);
                    messageWriter.ForEach(mW => mW.Write((ushort)player.Data.Role.DefaultGhostRole));
                    messageWriter.ForEach(mW => mW.Write(false));
                    AmongUsClient.Instance.FinishRpcDesync(messageWriter);
                }

                return false;
            }
        }

        player.RpcSetRole(player.Data.Role.DefaultGhostRole);

        return false;
    }

    private static bool IsImpostorRole(RoleTypes role) => role.GetBehaviour().IsImpostor;

    internal static int RNG()
    {
        switch (BetterGameSettings.RoleRandomizer.GetStringValue())
        {
            case 1:
                return UnityEngine.Random.Range(0, 100);

            default:
                Random Random = new Random();
                return Random.Next(0, 100);
        }
    }
}