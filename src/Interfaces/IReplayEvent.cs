using System.Text.Json.Serialization;

namespace BetterAmongUs.Interfaces;

/// <summary>
/// Interface for replay events in BetterAmongUs.
/// </summary>
public interface IReplayEvent
{
    /// <summary>
    /// Gets the unique identifier for the replay event.
    /// </summary>
    [JsonIgnore]
    string Id { get; }

    /// <summary>
    /// Plays or executes the replay event.
    /// </summary>
    void Play();
}

/// <summary>
/// Generic interface for replay events with associated data.
/// </summary>
/// <typeparam name="T">The type of data associated with the event.</typeparam>
public interface IReplayEvent<T> : IReplayEvent
{
    /// <summary>
    /// Gets or sets the event data.
    /// </summary>
    T? EventData { get; set; }
}