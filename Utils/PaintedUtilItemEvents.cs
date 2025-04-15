using UnityEngine;
using UnityEngine.Events;
using REPOLib.Objects;
using System.Collections;
using System.Collections.Generic;
using REPOLib.Modules;

namespace PaintedUtils
{
    [System.Serializable]
    public class PlayerEvent : UnityEvent<PhysGrabber> { }

    [System.Serializable]
    public class TimerEvent
    {
        public string name;
        public float interval = 1f;
        public bool onlyWhenGrabbed = false;
        public PlayerEvent onTimer;
    }

    [System.Serializable]
    public class UseEvent
    {
        public string name;
        public bool useCustomKey = false;
        public KeyCode customKey = KeyCode.E;
        public bool canUseWhenNotGrabbed = false;
        public PlayerEvent onUse;
    }

    public class PaintedUtilItemEvents : MonoBehaviour
    {
        [Header("Grab Events")]
        public PlayerEvent onGrabbed;
        public PlayerEvent onReleased;

        [Header("Use Events")]
        public List<UseEvent> useEvents = new List<UseEvent>();

        [Header("Timer Events")]
        public List<TimerEvent> timerEvents = new List<TimerEvent>();
        private List<Coroutine> timerCoroutines = new List<Coroutine>();

        private PhysGrabObject grabObject;
        private bool wasGrabbed = false;

        private void Awake()
        {
            grabObject = GetComponent<PhysGrabObject>();
            if (grabObject == null)
            {
                Debug.LogWarning($"PaintedUtilItemEvents on {gameObject.name} requires a PhysGrabObject component!");
                enabled = false;
                return;
            }

            StartAllTimers();
        }

        private void Update()
        {
            // Handle grab state changes
            if (grabObject.grabbedLocal != wasGrabbed)
            {
                wasGrabbed = grabObject.grabbedLocal;
                
                if (wasGrabbed)
                {
                    onGrabbed.Invoke(FindObjectOfType<PhysGrabber>());
                }
                else
                {
                    onReleased.Invoke(FindObjectOfType<PhysGrabber>());
                }
            }

            // Handle use events
            foreach (var useEvent in useEvents)
            {
                bool keyPressed = useEvent.useCustomKey ? 
                    Input.GetKeyDown(useEvent.customKey) : 
                    SemiFunc.InputDown(InputKey.Interact);

                if (keyPressed)
                {
                    // If grabbed, always allow use
                    if (wasGrabbed)
                    {
                        useEvent.onUse.Invoke(FindObjectOfType<PhysGrabber>());
                    }
                    // If not grabbed but canUseWhenNotGrabbed is true, check if player is looking at and in range
                    else if (useEvent.canUseWhenNotGrabbed && IsPlayerLookingAtAndInRange())
                    {
                        useEvent.onUse.Invoke(FindObjectOfType<PhysGrabber>());
                    }
                }
            }
        }

        private bool IsPlayerLookingAtAndInRange()
        {
            // Get the PhysGrabber component from the local player
            var grabber = FindObjectOfType<PhysGrabber>();
            if (grabber == null) return false;

            // Check if this object is the one being looked at
            if (grabber.currentlyLookingAtPhysGrabObject != grabObject) return false;

            // Check if within grab range
            float distance = Vector3.Distance(grabber.transform.position, transform.position);
            return distance <= grabber.grabRange;
        }

        private void StartAllTimers()
        {
            foreach (var timerEvent in timerEvents)
            {
                if (timerEvent.interval > 0)
                {
                    var coroutine = StartCoroutine(TimerRoutine(timerEvent));
                    timerCoroutines.Add(coroutine);
                }
            }
        }

        private IEnumerator TimerRoutine(TimerEvent timerEvent)
        {
            while (true)
            {
                yield return new WaitForSeconds(timerEvent.interval);
                
                if (!timerEvent.onlyWhenGrabbed || wasGrabbed)
                {
                    timerEvent.onTimer.Invoke(FindObjectOfType<PhysGrabber>());
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var coroutine in timerCoroutines)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            timerCoroutines.Clear();
        }
    }
} 