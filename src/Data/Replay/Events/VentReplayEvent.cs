using BetterAmongUs.Helpers;
using BetterAmongUs.Interfaces;
using System.Text.Json.Serialization;

namespace BetterAmongUs.Data.Replay.Events;

[Serializable]
internal sealed class VentReplayEvent : IReplayEvent<(int playerId, bool exit, int ventId)>
{
    public string Id => "player_vent";

    [JsonInclude]
    public (int playerId, bool exit, int ventId) EventData { get; set; }

    public void Play()
    {
        var vent = ShipStatus.Instance.AllVents.First(v => v.Id == EventData.ventId);
        var player = Utils.PlayerFromPlayerId(EventData.playerId);
        if (vent != null && player != null)
        {
            if (EventData.exit)
            {
                vent.ExitVent(player);
            }
            else
            {
                vent.EnterVent(player);
            }
        }
    }

    public void Record(PlayerControl player, bool exit, int ventId)
    {
        EventData = (player.PlayerId, exit, ventId);
    }
}
