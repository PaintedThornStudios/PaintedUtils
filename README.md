# PaintedUtils

A utility mod for REPO that provides useful functionality for game objects and items.

## Features

### ColorChanger

The ColorChanger component allows you to:

- Apply color presets to game objects
- Modify emission intensity for glowing effects
- Easily apply colors to objects at runtime
- Revert objects to their original materials

#### Usage Examples

```csharp
// Get a reference to a game object
GameObject myObject = GameObject.Find("SomeObject");

// Method 1: Use the static helper to apply a color directly
ColorChanger.ColorObject(myObject, Color.red, 1.5f);  // Apply red with emission intensity 1.5

// Method 2: Add a ColorChanger component with presets
ColorChanger changer = ColorChanger.SetupOnObject(myObject);
changer.ApplyColorPreset("Blue");  // Apply a preset

// Method 3: Create your own presets
changer.AddColorPreset("MyCustomColor", new Color(0.5f, 0.2f, 0.8f), 2.0f);
changer.ApplyColorPreset("MyCustomColor");

// Reset to original appearance
changer.ResetToOriginalMaterials();
```

### ItemDropper

The ItemDropper component provides a way to make objects drop items when destroyed or interacted with:

- Define drop tables with guaranteed and random weighted drops
- Set drop quantity ranges for each item
- Apply physics forces to dropped items
- Support for item rarity effects (particles, sounds, etc.)

#### Usage Examples

```csharp
// Method 1: Add an ItemDropper component to a GameObject
GameObject enemy = GameObject.Find("Enemy");
ItemDropper dropper = ItemDropper.SetupDropperOnObject(enemy, "DropTables/CustomDropTable");

// Method 2: Trigger drops manually
dropper.DropItems();

// Method 3: Use static methods to drop items at a position
Vector3 position = new Vector3(10, 0, 10);
ItemDropper.DropItemsAtPosition("DropTables/CustomDropTable", position);

// Method 4: Drop items for an object being destroyed
ItemDropper.DropItemsForGameObject(gameObject, "DropTables/CustomDropTable");
```

### ItemLibrary

The ItemLibrary component provides a standard way to implement interactive items:

- Player look-at detection with customizable prompts
- Key-based interaction system
- Battery power system for electronic items
- Unity events for interaction callbacks

#### Usage Examples

```csharp
// Basic setup on a GameObject
GameObject item = GameObject.Find("InteractiveItem");
ItemLibrary library = item.AddComponent<ItemLibrary>();
library.promptText = "Press E to Use Flashlight";

// Add battery functionality
ItemBattery battery = item.AddComponent<ItemBattery>();
battery.maxBatteryLife = 60f; // 60 seconds
battery.drainRate = 1f;       // 1 unit per second
library.useBattery = true;
library.battery = battery;

// Add interaction events
library.onInteract.AddListener(() => {
    // Toggle flashlight on/off
    Light light = item.GetComponent<Light>();
    light.enabled = !light.enabled;
    battery.isDraining = light.enabled;
});

// Add battery empty event
library.onBatteryEmpty.AddListener(() => {
    // Turn off light when battery is empty
    Light light = item.GetComponent<Light>();
    light.enabled = false;
});
```

## Installation

1. Ensure you have BepInEx installed
2. Place the `PaintedUtils.dll` in your BepInEx/plugins folder
3. Start the game

## Dependencies

- REPOLib 2.0.1 or higher 