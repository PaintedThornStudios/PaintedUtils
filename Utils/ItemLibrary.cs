using UnityEngine;
using UnityEngine.Events;

namespace PaintedUtils
{
    // Interface for the game's PhysGrabber system to avoid direct dependency
    public interface IPhysGrabber
    {
        IItemAttributes currentlyLookingAtItemAttributes { get; }
    }

    // Interface for ItemAttributes
    public interface IItemAttributes
    {
        GameObject gameObject { get; }
        string GetDisplayName();
    }

    // Static utility class to interface with game systems
    public static class GameInterfaces
    {
        public static IPhysGrabber PhysGrabber { get; set; }
        
        public static void UIItemInfoText(IItemAttributes itemAttributes, string promptText)
        {
            // This would ideally be implemented via a delegate or event system
            // Debug.Log($"[UIItemInfoText] {itemAttributes.GetDisplayName()}: {promptText}");
        }
    }

    [RequireComponent(typeof(Collider))]
    public class ItemLibrary : MonoBehaviour, IItemAttributes
    {
        [Header("Prompt Settings")]
        public bool enablePrompt = true;
        [Tooltip("Displayed when the player looks at the item.")]
        public string promptText = "Press E to Interact";
        [Tooltip("Display name for the item")]
        public string displayName;

        [Header("Battery Settings")]
        public bool useBattery = false;
        public ItemBattery battery;

        [Header("Interaction Settings")]
        public bool allowInteraction = true;
        public KeyCode interactionKey = KeyCode.E;

        [Header("Unity Events")]
        public UnityEvent onInteract;
        public UnityEvent onLookAt;
        public UnityEvent onBatteryEmpty;

        private bool isLookedAt = false;

        // Implementation of IItemAttributes
        public string GetDisplayName() => !string.IsNullOrEmpty(displayName) ? displayName : gameObject.name;

        private void Start()
        {
            if (useBattery && battery == null)
            {
                battery = GetComponent<ItemBattery>();
                if (battery == null)
                {
                    // Debug.LogWarning($"{name}: 'useBattery' is enabled but no ItemBattery found.");
                }
            }
        }

        private void Update()
        {
            HandleLookAt();

            if (isLookedAt && allowInteraction && Input.GetKeyDown(interactionKey))
            {
                TryUse();
            }

            if (useBattery && battery != null && battery.batteryLife <= 0)
            {
                onBatteryEmpty.Invoke();
            }
        }

        private void HandleLookAt()
        {
            if (!enablePrompt || GameInterfaces.PhysGrabber == null) return;

            if (GameInterfaces.PhysGrabber.currentlyLookingAtItemAttributes != null &&
                GameInterfaces.PhysGrabber.currentlyLookingAtItemAttributes.gameObject == this.gameObject)
            {
                if (!isLookedAt)
                {
                    isLookedAt = true;
                    onLookAt.Invoke();
                }

                // Call UI update function
                GameInterfaces.UIItemInfoText(this, promptText);
            }
            else
            {
                isLookedAt = false;
            }
        }

        private void TryUse()
        {
            if (!allowInteraction) return;

            if (useBattery && battery != null && battery.batteryLife <= 0f)
            {
                // Optional: play "empty" sound or show error prompt
                return;
            }

            onInteract.Invoke();
        }
    }

    // Battery component for items that need power
    public class ItemBattery : MonoBehaviour
    {
        [Header("Battery Settings")]
        public float batteryLife = 100f;
        public float maxBatteryLife = 100f;
        public float drainRate = 1f;
        public bool isDraining = false;

        [Header("Events")]
        public UnityEvent onBatteryEmpty;
        public UnityEvent onBatteryDrained;
        public UnityEvent onBatteryRecharged;

        private bool wasEmpty = false;

        private void Update()
        {
            if (isDraining)
            {
                DrainBattery(drainRate * Time.deltaTime);
            }

            // Check if battery just went empty
            if (batteryLife <= 0 && !wasEmpty)
            {
                wasEmpty = true;
                onBatteryEmpty?.Invoke();
            }
            else if (batteryLife > 0 && wasEmpty)
            {
                wasEmpty = false;
            }
        }

        public void DrainBattery(float amount)
        {
            if (batteryLife <= 0) return;

            batteryLife = Mathf.Max(0, batteryLife - amount);
            onBatteryDrained?.Invoke();
        }

        public void RechargeBattery(float amount)
        {
            batteryLife = Mathf.Min(maxBatteryLife, batteryLife + amount);
            onBatteryRecharged?.Invoke();
        }

        public void SetBatteryFull()
        {
            batteryLife = maxBatteryLife;
            onBatteryRecharged?.Invoke();
        }

        public float GetBatteryPercentage()
        {
            return batteryLife / maxBatteryLife;
        }
    }
} 