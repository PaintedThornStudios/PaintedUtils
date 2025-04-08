using UnityEngine;

namespace PaintedUtils
{
    public static class RarityEffectManager
    {
        public static RarityEffectHandle TryPlayEffects(Rarity rarity, Vector3 position, Transform parent = null)
        {
            var handle = new RarityEffectHandle();

            if (rarity.spawnEffect != null)
            {
                var go = GameObject.Instantiate(rarity.spawnEffect, position, Quaternion.identity, parent);
                go.transform.up = Vector3.up;
                go.Play();
                handle.particleSystem = go;
                handle.particleGameObject = go.gameObject;

                // Apply color override if enabled
                if (rarity.overrideEffectColor)
                {
                    ApplyColorOverrideToEffect(go.gameObject, rarity);
                }

                // If the effect should be disabled on grab, add the necessary components
                if (rarity.disableEffectOnGrab && parent != null)
                {
                    var tracker = parent.gameObject.AddComponent<RarityEffectTracker>();
                    tracker.effectHandle = handle;
                    
                    // Add the grab effect handler if the object can be grabbed
                    if (parent.gameObject.GetComponent<PhysGrabObject>() != null)
                    {
                        parent.gameObject.AddComponent<RarityGrabEffectHandler>();
                    }
                }
            }

            if (rarity.spawnSound != null && rarity.spawnSound.clip != null)
            {
                rarity.InitializeAudioSource(handle.particleGameObject);
            }

            return handle;
        }

        private static void ApplyColorOverrideToEffect(GameObject targetEffectInstance, Rarity rarity)
        {
            if (targetEffectInstance == null || rarity == null) return;

            // Find all ParticleSystems in the object and children
            ParticleSystem[] systems = targetEffectInstance.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                var main = ps.main;
                if (rarity.overrideEffectColor)
                {
                    main.startColor = rarity.overrideColor;
                }
            }
        }
    }
} 