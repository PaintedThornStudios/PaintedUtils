using UnityEngine;
using System.Collections.Generic;

namespace PaintedUtils
{
    [System.Serializable]
    public class TrackMessageData
    {
        public List<string> messages = new List<string>();
        public Rarity.Sound? sound;
        [Header("Eye Effects")]
        [Tooltip("Color of the eyes during TTS")]
        public Color eyeColor = Color.white;
        [Tooltip("Color of the pupils during TTS")]
        public Color pupilColor = Color.black;
        [Tooltip("If true, eyes will cycle through rainbow colors")]
        public bool eyeColorRGB = false;
        [Tooltip("If true, pupils will cycle through rainbow colors")]
        public bool pupilColorRGB = false;
        [Range(0.1f, 5f)]
        [Tooltip("Size of the pupils during TTS")]
        public float pupilSize = 3f;
        [Header("Speaker Light")]
        [Tooltip("Color of the speaker light")]
        public Color lightColor = Color.white;
        [Tooltip("If true, light will cycle through rainbow colors")]
        public bool lightColorRGB = false;
        [Range(0f, 10f)]
        [Tooltip("Intensity of the eye and speaker light")]
        public float lightIntensity = 5f;
        [Header("Particles")]
        [Tooltip("Optional track-specific particle system. If not set, will use the default music particles")]
        public ParticleSystem? trackParticles;

        public float CalculateMessageDuration(string message)
        {
            if (string.IsNullOrEmpty(message)) return 2f;
            
            // Calculate duration based on message length (0.05 seconds per character)
            return Mathf.Max(message.Length * 0.05f, 2f);
        }
    }

    [CreateAssetMenu(fileName = "New Track Data", menuName = "Painted Utils/Track Data")]
    public class TrackData : ScriptableObject
    {
        public List<TrackMessageData> tracks = new List<TrackMessageData>();
        [Tooltip("Default messages to use if no track-specific messages are provided")]
        public List<string> defaultMessages = new List<string>();
        public float messageFrequency = 5f;
        public float messageRandomness = 1f;

        public TrackMessageData? GetTrackData(int index)
        {
            if (index < 0 || index >= tracks.Count) return null;
            return tracks[index];
        }

        public string GetRandomMessage(int trackIndex)
        {
            var trackData = GetTrackData(trackIndex);
            if (trackData == null) return GetRandomDefaultMessage();
            
            if (trackData.messages.Count > 0)
            {
                return trackData.messages[Random.Range(0, trackData.messages.Count)];
            }
            
            return GetRandomDefaultMessage();
        }

        private string GetRandomDefaultMessage()
        {
            if (defaultMessages.Count == 0) return string.Empty;
            return defaultMessages[Random.Range(0, defaultMessages.Count)];
        }

        public float GetMessageDuration(int trackIndex, string message)
        {
            var trackData = GetTrackData(trackIndex);
            if (trackData == null) return 2f; // Default fallback duration
            return trackData.CalculateMessageDuration(message);
        }
    }
} 