using BetterAmongUs.Helpers;
using BetterAmongUs.Interfaces;
using System.Text.Json.Serialization;

namespace BetterAmongUs.Data.Replay.Events;

[Serializable]
internal sealed class MurderReplayEvent : IReplayEvent<(int killerId, int targetId)>
{
    public string Id => "player_murder";

    [JsonInclude]
    public (int killerId, int targetId) EventData { get; set; }

    public void Play()
    {
        var killer = Utils.PlayerFromPlayerId(EventData.killerId);
        var target = Utils.PlayerFromPlayerId(EventData.targetId);
        killer?.MurderPlayer(target, MurderResultFlags.Succeeded);
    }

    public void Record(PlayerControl killer, PlayerControl target)
    {
        EventData = (killer.PlayerId, target.PlayerId);
    }
}
