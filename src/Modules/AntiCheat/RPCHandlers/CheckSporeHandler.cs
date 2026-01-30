using BetterAmongUs.Attributes;
using Hazel;

namespace BetterAmongUs.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class CheckSporeHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.CheckSpore;

    internal override bool HandleAntiCheatCancel(PlayerControl? sender, MessageReader reader)
    {
        if (!GameState.IsHost)
        {
            LogRpcInfo($"Non-host attempted CheckSpore RPC");
            return false;
        }

        return true;
    }
}