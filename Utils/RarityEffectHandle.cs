using UnityEngine;

namespace PaintedUtils
{
    public class RarityEffectHandle
    {
        public ParticleSystem particleSystem;
        public GameObject particleGameObject;
        public AudioSource audioSource;

        public void DisableEffects()
        {
            if (particleSystem != null)
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (particleGameObject != null)
                GameObject.Destroy(particleGameObject);
            // No need to destroy AudioSource.PlayClipAtPoint result; it's auto-cleaned up
        }
    }
} 