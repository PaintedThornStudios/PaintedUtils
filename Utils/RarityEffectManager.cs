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
                    ApplyColorOverrideToEffect(go.gameObject);
                }
            }

            if (rarity.spawnSound != null)
            {
                // Note: This cannot be stopped manually — PlayClipAtPoint uses a one-shot AudioSource.
                AudioSource.PlayClipAtPoint(rarity.spawnSound.clip, position);
            }

            return handle;
        }

        public static void ApplyColorOverrideToEffect(GameObject targetEffectInstance)
        {
            if (targetEffectInstance == null) return;

            // Find all ParticleSystems in the object and children
            ParticleSystem[] systems = targetEffectInstance.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                var main = ps.main;
                // Get the rarity component from the root object or parent hierarchy
                var rarity = targetEffectInstance.GetComponent<Rarity>();
                if (rarity == null && targetEffectInstance.transform.parent != null)
                {
                    rarity = targetEffectInstance.transform.parent.GetComponent<Rarity>();
                }

                if (rarity != null && rarity.overrideEffectColor)
                {
                    main.startColor = rarity.overrideColor;
                }
            }
        }
    }
} 