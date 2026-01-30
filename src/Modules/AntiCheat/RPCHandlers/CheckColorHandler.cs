using BetterAmongUs.Attributes;
using Hazel;

namespace BetterAmongUs.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class CheckColorHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.CheckColor;

    internal override bool HandleAntiCheatCancel(PlayerControl? sender, MessageReader reader)
    {
        if (!GameState.IsHost)
        {
            LogRpcInfo($"Non-host attempted CheckColor RPC");
            return false;
        }

        return true;
    }
}