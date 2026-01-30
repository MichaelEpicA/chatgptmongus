using BetterAmongUs.Interfaces;

namespace BetterAmongUs.Data.Replay.Events;

internal sealed class StartMeetingReplayEvent : IReplayEvent<(int playerId, int targetId)>
{
    public string Id => "start_meeting";
    public (int playerId, int targetId) EventData { get; set; }

    public void Play()
    {
    }

    public void Record(PlayerControl player, PlayerControl? target)
    {
        EventData = (player.PlayerId, target?.PlayerId ?? -1);
    }
}
