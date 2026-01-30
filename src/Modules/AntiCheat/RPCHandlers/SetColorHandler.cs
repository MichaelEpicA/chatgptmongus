using BetterAmongUs.Attributes;

namespace BetterAmongUs.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class SetColorHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.SetColor;
}
