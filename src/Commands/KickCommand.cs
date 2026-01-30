using BetterAmongUs.Attributes;
using BetterAmongUs.Commands.Arguments;
using BetterAmongUs.Helpers;
using BetterAmongUs.Modules;

namespace BetterAmongUs.Commands;

[RegisterCommand]
internal sealed class KickCommand : BaseCommand
{
    internal override string Name => "kick";
    internal override string Description => "Kick a player from the game";
    internal override bool CanRunCommand(out string reason)
    {
        if (!GameState.IsHost)
        {
            reason = "Can only run as host";
            return false;
        }

        return base.CanRunCommand(out reason);
    }
    public KickCommand()
    {
        playerArgument = new PlayerArgument(this);
        boolArgument = new BoolArgument(this, "{ban}");
        Arguments = [playerArgument, boolArgument];
    }
    private PlayerArgument playerArgument { get; }
    private BoolArgument boolArgument { get; }

    internal override void Run()
    {
        var player = playerArgument.TryGetTarget();
        var isBan = boolArgument.GetBool();
        if (player != null && isBan != null && !player.IsHost())
        {
            player.Kick((bool)isBan);
        }
    }
}
