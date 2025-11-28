using Project;

/// <summary>
/// Interface that all events must implement.
/// Defines the event lifecycle: InitializeEvent → StartEvent → ResetEvent
/// Uses EventState from Project namespace (see GameTypes.cs)
/// </summary>
public interface IEvent
{
    /// <summary>
    /// Initialize the event - robot navigates autonomously to event location
    /// Called when button is pressed in MonitoringScene UI
    /// </summary>
    void InitializeEvent();

    /// <summary>
    /// Start the event - activate obstacles, give player control
    /// Called automatically when robot reaches event location (via trigger)
    /// </summary>
    void StartEvent();

    /// <summary>
    /// Reset the event - clean up, disable player control, return to idle state
    /// Called when reset button is clicked after event completion
    /// </summary>
    void ResetEvent();

    /// <summary>
    /// Get the event's current state
    /// </summary>
    EventState GetState();

    /// <summary>
    /// Get the event name for debugging
    /// </summary>
    string GetEventName();

    /// <summary>
    /// Get the event boundaries (box colliders defining safe zones)
    /// Returns array of EventBoundary components - robot must stay within ANY of them
    /// </summary>
    EventBoundary[] GetEventBoundaries();
}
