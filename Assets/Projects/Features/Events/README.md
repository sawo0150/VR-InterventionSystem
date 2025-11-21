# Event Management System

## Overview
This system manages all events in the VR Intervention System. It ensures only one event runs at a time and provides a consistent interface for all events.

## Architecture

```
SimulationSceneManager (singleton)
├── Event1Controller (implements IEvent)
├── Event2Controller (implements IEvent)
└── Event3Controller (implements IEvent)
```

## Event Lifecycle

1. **Initialize** - Robot navigates autonomously to event location
2. **Start** - Activate obstacles, give player control (triggered when robot arrives)
3. **Reset** - Clean up, return robot to idle state

## Setup Instructions

### 1. Create SimulationSceneManager

1. Create an empty GameObject in your scene
2. Name it "SimulationSceneManager"
3. Add the `SimulationSceneManager` component
4. Assign Event1Controller, Event2Controller, Event3Controller in Inspector

### 2. Set Up Event 1 (Example)

**Create Event1 GameObject:**
1. Create empty GameObject: "Event1"
2. Add `Event1Controller` component

**Configure Event1Controller:**
- **Robot**: Assign the robot GameObject for this event
- **Autonomous Navigation**: Assign autonomous nav component (if you have one)
- **Event Location**: Create empty GameObject at event location, assign here
- **Start Trigger**: Assign EventStartTrigger component
- **End Trigger**: Assign EventEndTrigger component

**Create Event Start Zone:**
1. Create empty GameObject: "Event1_StartZone"
2. Position it at event location
3. Add BoxCollider, check "Is Trigger"
4. Add `EventStartTrigger` component
5. Configure:
   - **Obstacles To Activate**: Drag all event obstacles here
   - **Robot Tag**: "Robot"

**Create Event End Zone:**
1. Create empty GameObject: "Event1_EndZone"
2. Position it at completion location
3. Add BoxCollider, check "Is Trigger"
4. Add `EventEndTrigger` component
5. Configure:
   - **Reset Button**: Assign UI reset button GameObject
   - **Robot Tag**: "Robot"

**Tag Your Robot:**
- Select robot GameObject
- Set Tag to "Robot"

### 3. Repeat for Event 2 and Event 3

Each teammate creates their own:
- Event2Controller / Event3Controller (implement IEvent)
- Event robots
- Start/End triggers
- Event-specific obstacles

## Triggering Events from MonitoringScene

### From UI Button:

```csharp
public class MonitoringSceneUI : MonoBehaviour
{
    public void OnEvent1ButtonClicked()
    {
        SimulationSceneManager.Instance.StartEvent(1);
    }

    public void OnEvent2ButtonClicked()
    {
        SimulationSceneManager.Instance.StartEvent(2);
    }

    public void OnEvent3ButtonClicked()
    {
        SimulationSceneManager.Instance.StartEvent(3);
    }
}
```

### In Unity Inspector:
1. Select Event Button in UI
2. Add OnClick event
3. Drag SimulationSceneManager GameObject
4. Select `StartEvent(int)`
5. Enter event number (1, 2, or 3)

## Creating Your Own Event

### Step 1: Implement IEvent Interface

```csharp
public class Event2Controller : MonoBehaviour, IEvent
{
    private EventState currentState = EventState.Idle;

    public void Initialize()
    {
        currentState = EventState.Initializing;
        // Start autonomous navigation to event location
    }

    public void Start()
    {
        currentState = EventState.Active;
        // Give player control
        // Activate obstacles
    }

    public void Reset()
    {
        currentState = EventState.Idle;
        // Disable player control
        // Deactivate obstacles
        // Return robot to spawn
    }

    public EventState GetState()
    {
        return currentState;
    }

    public string GetEventName()
    {
        return "Event 2";
    }
}
```

### Step 2: Assign to SimulationSceneManager

1. Select SimulationSceneManager GameObject
2. Assign your EventController to Event2Controller or Event3Controller slot

### Step 3: Create Triggers

Use EventStartTrigger and EventEndTrigger as templates

## Flow Diagram

```
MonitoringScene UI
    ↓ (Button Click)
SimulationSceneManager.StartEvent(eventNumber)
    ↓
Event.Initialize()
    ↓ (Robot navigates)
EventStartTrigger detects robot
    ↓
SimulationSceneManager.OnEventLocationReached()
    ↓
Event.Start()
    ↓ (Player completes event)
EventEndTrigger shows reset button
    ↓ (Player clicks reset)
SimulationSceneManager.ResetCurrentEvent()
    ↓
Event.Reset()
```

## Important Notes

- **Only one event can run at a time** - SimulationSceneManager enforces this
- **Tag your robots** - All robots must have "Robot" tag for triggers to work
- **Autonomous Navigation** - If you don't have autonomous nav yet, Event1Controller will teleport the robot (for testing)
- **Player Control** - RobotNavMeshController is enabled/disabled automatically
- **Obstacles** - Managed by EventStartTrigger (activated) and Event.Reset() (deactivated)

## Debug Tips

- Enable debug logs in SimulationSceneManager, EventControllers, and Triggers
- Check Console for event state changes
- Use Scene view to visualize trigger zones (green = start, yellow = end)
- Test flow: Initialize → Start → Reset before connecting UI

## Questions?

Ask your teammates or check existing Event1Controller implementation as reference!
