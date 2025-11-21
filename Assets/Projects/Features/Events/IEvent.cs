/// <summary>
/// Interface that all events must implement.
/// Defines the event lifecycle: InitializeEvent → StartEvent → ResetEvent
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
}

/// <summary>
/// Event lifecycle states
/// </summary>
public enum EventState
{
    Idle,           // Event not started
    Initializing,   // Robot navigating to location
    Active,         // Player has control, obstacles active
    Completed       // Event finished, ready to reset
}
