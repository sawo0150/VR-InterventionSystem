# Sound System Setup Guide

## Overview
The sound system has been integrated into your VR Intervention System. It provides:
- **Engine sounds** that vary with robot speed
- **Collision sounds** for deer and rolling stone obstacles
- **UI alert sounds** for warnings, errors, status messages, etc.
- **Delivery complete sound** when missions finish

## Quick Setup (5 Steps)

### Step 1: Create Audio Config Asset
1. In Unity Project window, navigate to `Assets/Projects/Features/Sounds/`
2. Right-click → Create → VR Intervention → Audio Config
3. Name it `MainAudioConfig`
4. Assign your audio clips to the fields in the Inspector

### Step 2: Create SoundManager GameObject
1. Open your **LoaderScene** (0_LoaderScene)
2. Create a new Empty GameObject, name it `SoundManager`
3. Add the `SoundManager` component to it
4. Assign the `MainAudioConfig` asset to the Audio Config field

### Step 3: Add Audio Sources to SoundManager
The SoundManager needs three AudioSource components:

1. **Engine Audio Source** (for robot engine sounds)
   - Add Component → Audio Source
   - Set Spatial Blend to 1.0 (full 3D)
   - Loop: Will be controlled by script
   - Play On Awake: OFF

2. **UI Audio Source** (for UI notification sounds)
   - Add Component → Audio Source
   - Set Spatial Blend to 0.0 (2D sound)
   - Loop: OFF
   - Play On Awake: OFF

3. **SFX Audio Source** (for collision/impact sounds)
   - Add Component → Audio Source
   - Set Spatial Blend to 1.0 (full 3D)
   - Loop: OFF
   - Play On Awake: OFF

4. Assign these AudioSources in the SoundManager component Inspector

### Step 4: Attach Engine Audio Source to Robot
The engine sound should come from the robot's position (3D spatial audio).

**Option A (Recommended):** Attach to robot at runtime
- The SoundManager has a method `AttachEngineSourceToRobot(Transform robotTransform)`
- Call this when your robot is instantiated/initialized
- Add this to your robot initialization code:
```csharp
if (SoundManager.Instance != null)
{
    SoundManager.Instance.AttachEngineSourceToRobot(robotTransform);
}
```

**Option B:** Parent manually in Editor
- In the Hierarchy, drag the Engine AudioSource to be a child of your robot GameObject
- Reset its local position to (0, 0, 0)

### Step 5: Assign Audio Clips in AudioConfig
Open the `MainAudioConfig` asset and assign your audio clips:

**Engine Sounds:**
- Engine Loop: Looping engine idle sound
- Engine Base Volume: 0.5 (adjust to taste)
- Engine Min Pitch: 0.8 (pitch when stopped/slow)
- Engine Max Pitch: 1.5 (pitch at max speed)

**Collision Sounds:**
- Deer Collision Sound: Impact sound for deer
- Stone Collision Sound: Impact sound for rolling stones
- Collision Volume: 0.7

**UI Alert Sounds:**
- Alert Sound: Event activation alerts
- Warning Sound: Boundary violations
- Error Sound: Critical errors
- Hint Sound: Help messages
- Status Sound: Informational messages
- UI Sound Volume: 0.6

**UI Interaction Sounds:**
- Button Click Sound: Sound when regular button is pressed/clicked
- Button Hover Sound: Sound when hovering over buttons
- Event Trigger Button Sound: Special sound for Event 1/2/3 trigger buttons
- Button Sound Volume: 0.5
- Event Button Volume: 0.7

**Completion Sounds:**
- Delivery Complete Sound: Success/completion sound
- Completion Volume: 0.8

## How It Works

### Engine Sounds
- Triggered automatically by `RobotNavMeshController.cs:147`
- Pitch changes based on robot speed (0 = idle pitch, max speed = high pitch)
- Smoothly transitions using `enginePitchSmoothTime` setting
- Only plays when robot is moving

### Collision Sounds
- Triggered when robot collides with obstacles
- Different sounds for deer vs. rolling stones
- Plays at collision location (3D spatial audio)
- Triggered in `ObstacleCollisionHandler.cs:90-93`

### UI Alert Sounds
- Triggered whenever UI panels are shown
- Different sounds for different message types:
  - Alert: Event activation
  - Warning: Boundary violations
  - Error: Deer/stone collisions, critical errors
  - Hint: Player guidance
  - Status: Informational messages
  - Delivery: Mission complete
- Plays as 2D audio (non-spatial, always audible)
- Triggered in `PlayerUIManager.cs:301-305`

### Delivery Complete Sound
- Triggered when robot completes delivery
- Plays before showing completion UI
- Triggered in `SimulationSceneManager.cs:171-175`

### Button Interaction Sounds
- **Easy Setup**: Add `UIButtonSound` component to any UI Button
- Automatically plays click sound when button is pressed
- Automatically plays hover sound when mouse/pointer enters button
- Works with both regular Unity UI and XR interactable buttons
- Respects button's `interactable` state (no sound if disabled)

**Manual Setup (if needed):**
```csharp
// Play click sound manually
SoundManager.Instance.PlayButtonClickSound();

// Play hover sound manually
SoundManager.Instance.PlayButtonHoverSound();
```

## File Reference

### Created Files
- `AudioConfig.cs` - ScriptableObject for organizing audio settings
- `SoundManager.cs` - Singleton managing all sound playback
- `UIButtonSound.cs` - Component for automatic button sound playback
- `SOUND_SETUP_GUIDE.md` - This guide

### Modified Files
- `RobotNavMeshController.cs` - Added engine sound updates
- `ObstacleCollisionHandler.cs` - Added collision sounds
- `PlayerUIManager.cs` - Added UI alert sounds
- `SimulationSceneManager.cs` - Added delivery complete sound
- `GameManager.cs` - Added using statement for Audio namespace

## Testing

### Test Engine Sounds
1. Start the game and board a robot
2. Move the robot using VR controllers or WASD keys
3. Listen for engine sound pitch changing with speed

### Test Collision Sounds
1. Start Event 1
2. Intentionally collide with a deer or rolling stone
3. Listen for impact sound

### Test UI Sounds
1. Trigger various UI messages (warnings, alerts, etc.)
2. Each message type should have its own sound

### Test Delivery Complete Sound
1. Complete a full delivery mission
2. Listen for success sound when reaching the end trigger

### Test Button Sounds
1. Add UIButtonSound component to any button
2. Click the button - should hear click sound
3. Hover over button - should hear hover sound

## Adding Button Sounds to Your UI

### For Regular Buttons (Method 1 - Automatic)

1. **Select any UI Button** in your scene
2. **Add Component** → Search for "UIButtonSound"
3. **Configure** (optional):
   - ✓ Play Click Sound (checkbox)
   - ✓ Play Hover Sound (checkbox)
4. **Done!** The button will now play sounds automatically

### For Event Trigger Buttons (Special Sound)

**For Event 1, 2, 3 buttons in the minimap/monitoring scene:**

1. **Select the Event button** (Event 1, Event 2, or Event 3 button)
2. **Add Component** → Search for "UIEventTriggerButton"
3. **Configure** (optional):
   - ✓ Play Event Trigger Sound (checkbox) - Uses special event sound
   - ✓ Play Hover Sound (checkbox)
4. **Done!** The button will play a special event trigger sound when clicked

**Difference:**
- `UIButtonSound` → Plays regular button click sound
- `UIEventTriggerButton` → Plays special event trigger sound (more impactful for starting events)

### Method 2: Manual (For Custom Buttons)

If you have custom button scripts, add these calls:

```csharp
using VRInterventionSystem.Audio;

// Regular button click
SoundManager.Instance?.PlayButtonClickSound();

// Event trigger button click (special sound)
SoundManager.Instance?.PlayEventTriggerButtonSound();

// Button hover
SoundManager.Instance?.PlayButtonHoverSound();
```

### Method 3: Batch Add to Multiple Buttons

To add sounds to all buttons at once:

1. In Hierarchy, search for "Button" (type: Button)
2. Select all buttons (Ctrl+A / Cmd+A)
3. Add Component → UIButtonSound
4. All buttons now have sounds!

## Troubleshooting

### No sounds playing at all
- Check that SoundManager exists in the scene
- Verify AudioConfig is assigned
- Ensure Audio Listener exists on Main Camera (XR Rig)
- Check Unity's Audio settings (Edit → Project Settings → Audio)

### Engine sound not playing
- Verify Engine AudioSource is assigned in SoundManager
- Check that audio clip is assigned in AudioConfig
- Ensure the robot is actually moving (check speed in console)
- Verify the engine AudioSource is attached to the robot (or parented to it)

### UI sounds not playing
- Check UI AudioSource is assigned in SoundManager
- Verify audio clips are assigned in AudioConfig for each message type
- Test by triggering different UI panels

### Sounds too loud/quiet
- Adjust volume settings in AudioConfig
- Adjust individual AudioSource volumes
- Check Unity's Audio Mixer settings

### Engine sound doesn't change pitch
- Verify Engine Min/Max Pitch settings in AudioConfig
- Check Pitch Smooth Time (lower = faster response)
- Ensure robot speed is actually changing

## Advanced Customization

### Adding New Sound Types
1. Add AudioClip field to `AudioConfig.cs`
2. Add playback method to `SoundManager.cs`
3. Call the method from appropriate location in your code

### Adjusting Spatial Audio
- Modify `engineSpatialBlend` in AudioConfig (0 = 2D, 1 = 3D)
- Adjust `engineMaxDistance` for 3D sound falloff range
- Modify AudioSource Rolloff Mode (Linear/Logarithmic/Custom)

### Multiple Robot Support
If you have multiple robots:
- Create separate AudioSources for each robot's engine
- Call `AttachEngineSourceToRobot()` for each robot
- Or create multiple SoundManager instances (not recommended)

## Architecture Notes

The sound system uses a singleton pattern (`SoundManager.Instance`) for easy access from anywhere in the codebase. The AudioConfig ScriptableObject allows you to organize all audio settings in one place and swap configurations easily (e.g., different sound packs for different environments).

All sound triggers use null checks (`if (SoundManager.Instance != null)`) so the system gracefully handles missing SoundManager without throwing errors.
