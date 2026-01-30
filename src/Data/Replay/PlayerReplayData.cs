using AmongUs.GameOptions;
using UnityEngine;

namespace BetterAmongUs.Data.Replay;

/// <summary>
/// Represents player data for replay functionality.
/// </summary>
[Serializable]
internal class PlayerReplayData
{
    /// <summary>
    /// Gets or sets the player's ID.
    /// </summary>
    public int PlayerId;

    /// <summary>
    /// Gets or sets the player's name.
    /// </summary>
    public string PlayerName = "";

    /// <summary>
    /// Gets or sets the player's role type.
    /// </summary>
    public RoleTypes Role;

    /// <summary>
    /// Gets or sets the buffer of movement data containing timestamps and positions.
    /// </summary>
    public (float timeStamp, Vector2 pos)[] MovementDataBuffer = [];

    /// <summary>
    /// Gets or sets the player's cosmetic data.
    /// </summary>
    public (int colorId, string skinId, string visorId, string petId, string namePlateId) CosmeticData = new();
}