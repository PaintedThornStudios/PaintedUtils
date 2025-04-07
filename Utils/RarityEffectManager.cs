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
            }

            if (rarity.spawnSound != null)
            {
                // Note: This cannot be stopped manually — PlayClipAtPoint uses a one-shot AudioSource.
                AudioSource.PlayClipAtPoint(rarity.spawnSound.clip, position);
            }

            return handle;
        }
    }
} 