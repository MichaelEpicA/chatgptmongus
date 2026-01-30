namespace BetterAmongUs.Helpers;

/// <summary>
/// Represents a delayed task that executes after a specified time interval.
/// </summary>
internal sealed class LateTask
{
    private readonly Action _action;
    private float _remainingTime;
    private readonly string _name;
    private readonly bool _shouldLog;

    private static readonly List<LateTask> Tasks = new();

    /// <summary>
    /// Initializes a new instance of the LateTask class.
    /// </summary>
    /// <param name="action">The action to execute after the delay.</param>
    /// <param name="delay">The delay in seconds before executing the action.</param>
    /// <param name="name">The name of the task for logging purposes.</param>
    /// <param name="shouldLog">Whether to log task completion.</param>
    private LateTask(Action action, float delay, string name = "Unnamed Task", bool shouldLog = true)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _remainingTime = delay;
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _shouldLog = shouldLog;

        Tasks.Add(this);
    }

    /// <summary>
    /// Updates the task timer and executes the action if the delay has elapsed.
    /// </summary>
    /// <param name="deltaTime">The time in seconds since the last update.</param>
    /// <returns>True if the task has completed and should be removed, false otherwise.</returns>
    private bool Update(float deltaTime)
    {
        _remainingTime -= deltaTime;

        if (_remainingTime > 0)
            return false;

        try
        {
            _action.Invoke();

            if (_shouldLog)
            {
                Logger_.Log($"{_name} has finished", nameof(LateTask));
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger_.Error($"Error executing task '{_name}': {ex}");
            return true;
        }
    }

    /// <summary>
    /// Schedules a new delayed task.
    /// </summary>
    /// <param name="action">The action to execute after the delay.</param>
    /// <param name="delay">The delay in seconds before executing the action.</param>
    /// <param name="name">The name of the task for logging purposes.</param>
    /// <param name="shouldLog">Whether to log task completion.</param>
    internal static void Schedule(Action action, float delay, string name = "Unnamed Task", bool shouldLog = true)
    {
        _ = new LateTask(action, delay, name, shouldLog);
    }

    /// <summary>
    /// Updates all scheduled tasks with the elapsed time.
    /// </summary>
    /// <param name="deltaTime">The time in seconds since the last update.</param>
    internal static void UpdateAll(float deltaTime)
    {
        if (Tasks.Count == 0)
            return;

        var completedTasks = new List<LateTask>(Tasks.Count);

        foreach (var task in Tasks.ToArray())
        {
            if (task.Update(deltaTime))
            {
                completedTasks.Add(task);
            }
        }

        foreach (var task in completedTasks)
        {
            Tasks.Remove(task);
        }
    }

    /// <summary>
    /// Cancels and removes all scheduled tasks.
    /// </summary>
    public static void CancelAll()
    {
        Tasks.Clear();
    }

    /// <summary>
    /// Gets the number of currently active tasks.
    /// </summary>
    public static int ActiveTaskCount => Tasks.Count;
}