using UnityEngine;

namespace PaintedUtils
{
    public class RarityEffectTracker : MonoBehaviour
    {
        public RarityEffectHandle effectHandle;

        public void DisableRarityEffects()
        {
            effectHandle?.DisableEffects();
        }
    }
} 