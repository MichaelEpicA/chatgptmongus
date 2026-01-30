using AmongUs.GameOptions;
using BetterAmongUs.Helpers;
using BetterAmongUs.Interfaces;
using System.Text.Json.Serialization;

namespace BetterAmongUs.Data.Replay.Events;

[Serializable]
internal sealed class VanishReplayEvent : IReplayEvent<int>
{
    public string Id => "player_vanish";

    [JsonInclude]
    public int EventData { get; set; }

    public void Play()
    {
        var player = Utils.PlayerFromPlayerId(EventData);
        if (player?.Data.RoleType is RoleTypes.Phantom)
        {
            player?.SetRoleInvisibility(true, true, true);
        }
    }

    public void Record(PlayerControl player)
    {
        EventData = player.PlayerId;
    }
}
