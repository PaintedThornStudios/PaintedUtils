using UnityEngine;

namespace PaintedUtils
{
    [CreateAssetMenu(menuName = "PaintedUtils/Rarity")]
    public class Rarity : ScriptableObject
    {
        public string rarityName;
        public ParticleSystem spawnEffect;
        public Sound spawnSound;
        public Color uiColor = Color.white;
        public Sprite icon;

        [System.Serializable]
        public class Sound {
            public AudioClip clip;
            public float volume = 1.0f;
            [Range(0f, 3f)]
            public float pitch = 1.0f;
            [Range(0f, 1f)]
            public float volumeRandom = 0f;
            [Range(0f, 1f)]
            public float pitchRandom = 0f;
            [Range(0f, 1f)]
            public float spatialBlend = 1f;
            [Range(-3f, 3f)]
            public float doppler = 1f;
            [Range(0f, 10f)]
            public float reverbMix = 1f;
            public float falloffMultiplier = 1f;
            public int channel = 0;
        }

        // Method to initialize AudioSource
        public void InitializeAudioSource(GameObject gameObject) {
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = spawnSound.clip;
            audioSource.volume = spawnSound.volume * Random.Range(1f - spawnSound.volumeRandom, 1f);
            audioSource.pitch = spawnSound.pitch * Random.Range(1f - spawnSound.pitchRandom, 1f);
            audioSource.spatialBlend = spawnSound.spatialBlend;
            audioSource.dopplerLevel = spawnSound.doppler;
            audioSource.reverbZoneMix = spawnSound.reverbMix;
            audioSource.rolloffMode = AudioRolloffMode.Custom;
            audioSource.maxDistance = 50f * spawnSound.falloffMultiplier;
            audioSource.Play();
        }
    }
} 