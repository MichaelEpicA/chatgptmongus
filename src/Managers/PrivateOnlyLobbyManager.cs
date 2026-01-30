using BetterAmongUs.Modules;
using BetterAmongUs.Patches.Gameplay.UI.Settings;
using HarmonyLib;

namespace BetterAmongUs.Managers;

/// <summary>
/// Manages lobby-specific behaviors for private lobbies
/// </summary>
[HarmonyPatch]
internal static class PrivateOnlyLobbyManager
{
    [HarmonyPatch(typeof(PlayerControl))]
    [HarmonyPatch(nameof(PlayerControl.Die))]
    [HarmonyPostfix]
    internal static void PlayerControlDie_Postfix(PlayerControl __instance)
    {
        if (GameState.IsPrivateOnlyLobby && BetterGameSettings.RemovePetOnDeath.GetBool())
        {
            __instance.RpcSetPet(PetData.EmptyId);
        }
    }
}