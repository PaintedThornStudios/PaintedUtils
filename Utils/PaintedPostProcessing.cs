using UnityEngine;
using System.Collections.Generic;

namespace PaintedUtils
{
    public abstract class PaintedPostProcessingEffect
    {
        public float duration;
        public float intensity;
        public bool isActive;

        public abstract void ApplyEffect(PlayerAvatar player);
        public abstract void RemoveEffect(PlayerAvatar player);
        public virtual void UpdateEffect(PlayerAvatar player) { }
    }

    public class PaintedPostProcessing : MonoBehaviour
    {
        private static PaintedPostProcessing instance;
        private Dictionary<PlayerAvatar, List<PaintedPostProcessingEffect>> activeEffects = new Dictionary<PlayerAvatar, List<PaintedPostProcessingEffect>>();

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public static void ApplyEffect(PlayerAvatar player, PaintedPostProcessingEffect effect)
        {
            if (instance == null)
            {
                GameObject go = new GameObject("PaintedPostProcessing");
                instance = go.AddComponent<PaintedPostProcessing>();
            }

            if (!instance.activeEffects.ContainsKey(player))
            {
                instance.activeEffects[player] = new List<PaintedPostProcessingEffect>();
            }

            // Remove any existing effects of the same type
            var existingEffect = instance.activeEffects[player].Find(e => e.GetType() == effect.GetType());
            if (existingEffect != null)
            {
                existingEffect.RemoveEffect(player);
                instance.activeEffects[player].Remove(existingEffect);
            }

            instance.activeEffects[player].Add(effect);
            effect.ApplyEffect(player);
        }

        public static void RemoveEffect(PlayerAvatar player, PaintedPostProcessingEffect effect)
        {
            if (instance == null || !instance.activeEffects.ContainsKey(player)) return;

            effect.RemoveEffect(player);
            instance.activeEffects[player].Remove(effect);
        }

        private void Update()
        {
            foreach (var playerEffects in activeEffects)
            {
                foreach (var effect in playerEffects.Value.ToArray())
                {
                    if (effect.duration > 0)
                    {
                        effect.duration -= Time.deltaTime;
                        effect.UpdateEffect(playerEffects.Key);
                        
                        if (effect.duration <= 0)
                        {
                            effect.RemoveEffect(playerEffects.Key);
                            playerEffects.Value.Remove(effect);
                        }
                    }
                }
            }
        }
    }
} 