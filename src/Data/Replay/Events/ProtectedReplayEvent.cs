using BetterAmongUs.Helpers;
using BetterAmongUs.Interfaces;
using System.Text.Json.Serialization;

namespace BetterAmongUs.Data.Replay.Events;

[Serializable]
internal sealed class ProtectedReplayEvent : IReplayEvent<(int killerId, int targetId)>
{
    public string Id => "player_protected";

    [JsonInclude]
    public (int killerId, int targetId) EventData { get; set; }

    public void Play()
    {
        var target = Utils.PlayerFromPlayerId(EventData.targetId);

        RoleEffectAnimation roleEffectAnimation = UnityEngine.Object.Instantiate(RoleManager.Instance.protectAnim, target.transform);
        roleEffectAnimation.SetMaskLayerBasedOnWhoShouldSee(true);
        roleEffectAnimation.Play(target, null, target.cosmetics.FlipX, RoleEffectAnimation.SoundType.Global, 0f, true, 0f);
    }

    public void Record(PlayerControl killer, PlayerControl target)
    {
        EventData = (killer.PlayerId, target.PlayerId);
    }
}
