using UnityEngine;
using REPOLib.Objects;

namespace PaintedUtils
{
    public class RarityGrabEffectHandler : MonoBehaviour
    {
        private RarityEffectTracker tracker;
        private PhysGrabObject grabObject;
        private bool wasGrabbed = false;

        private void Awake()
        {
            tracker = GetComponent<RarityEffectTracker>();
            grabObject = GetComponent<PhysGrabObject>();
        }

        private void Update()
        {
            if (tracker == null || grabObject == null) return;

            // Check if grab state changed
            if (grabObject.grabbedLocal != wasGrabbed)
            {
                wasGrabbed = grabObject.grabbedLocal;
                
                // If just grabbed, disable effects
                if (wasGrabbed)
                {
                    tracker.DisableRarityEffects();
                }
            }
        }
    }
} 