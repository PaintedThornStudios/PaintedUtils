using UnityEngine;
using REPOLib;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine.Events;
using System.Collections;
using PaintedUtils;
using HarmonyLib;
using System.Linq;

namespace PaintedUtils
{
    public class MusicalItemHandler : MonoBehaviourPunCallbacks
    {
        public enum PowerOffBehavior
        {
            None,
            OnDrop,
            OnSlamWhenNotHeld,
            OnSlamWhenHeld,
            OnSlam
        }

        public enum SkipBehavior
        {
            None,
            SkipWhenNotHeld,
            SkipWhenHeld,
            SkipAlways
        }

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private TrackData? trackData;
        [Range(0.1f, 10f)]
        [Tooltip("Multiplies the base volume of the music")]
        public float musicVolumeMultiplier = 1f;
        [Tooltip("Base volume level for the music (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 1f;
        [Tooltip("Duration of fade in/out effects in seconds")]
        [SerializeField] private float fadeDuration = 1f;
        public Rarity.Sound? powerOnSound;
        public Rarity.Sound? powerOffSound;
        public Rarity.Sound? skipSound;
        private int currentTrack = -1;
        private AudioSource? currentTrackSource;

        [Header("Effects")]
        public ParticleSystem? musicParticles;
        public ParticleSystem? impactParticles;
        public Light? speakerLight;
        public float impactParticleDuration = 2f;
        public float lightIntensity = 2f;

        [Header("Settings")]
        public float minMessageDelay = 8f;
        public float maxMessageDelay = 16f;
        public bool skipOnImpact = true;
        public bool turnOffOnHeavyImpact = true;
        [Tooltip("Delay in seconds before the first track starts playing after power on")]
        public float initialTrackDelay = 1f;
        private float coolDownUntilNextMessage;
        private float initialMessageDelay = 8f;
        private float musicStartDelay = 5f;
        public bool requireHeldForNextTrack = true;
        public PowerOffBehavior powerOffBehavior = PowerOffBehavior.OnSlamWhenNotHeld;
        public SkipBehavior skipBehavior = SkipBehavior.SkipWhenNotHeld;

        private PhysGrabObject? physGrabObject;
        private PhysGrabObjectImpactDetector? impactDetector;
        private bool isPlaying;
        private float nextMessageTime;
        private int currentEyeStateId = -1;

        private Coroutine fadeCoroutine;
        private int currentTrackIndex = 0;
        private Coroutine rgbCycleCoroutine;
        private float rgbCycleSpeed = 0.5f; // Speed of RGB cycling in seconds per cycle

        internal class EyeStateData
        {
            public float Timer;
            public bool IsSpeaking;
            public bool IsInitialized;
        }

        internal static class CustomEyeStates
        {
            public static Dictionary<int, PlayerHealth.EyeOverrideState> TrackEyeStates = new();
            public static Dictionary<PlayerHealth.EyeOverrideState, (Color eyeColor, Color pupilColor, Color lightColor, float lightIntensity)> CustomStates = new();
            public static Dictionary<int, EyeStateData> TrackData = new();
        }

        // Messages organized by track
        private string[][] trackMessages = new string[][]
        {
            // Track 0 messages (default)
            new string[]
            {
                "Let's get this party started!",
                "Time to turn up!",
                "Music makes the world go round!",
                "Feel the beat!",
                "Dance like nobody's watching!",
                "Let the music take control!"
            }
        };

        public void SetTrackMessages(string[][] newMessages)
        {
            trackMessages = newMessages;
        }

        private void Start()
        {
            physGrabObject = GetComponent<PhysGrabObject>();
            impactDetector = GetComponent<PhysGrabObjectImpactDetector>();
            
            if (impactDetector != null)
            {
                impactDetector.onImpactHeavy.AddListener(OnHeavyImpact);
            }

            // Initialize audio source if not already present
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.volume = 0f;
            audioSource.spatialBlend = 1f; // Make it 3D audio
            audioSource.rolloffMode = AudioRolloffMode.Custom;
            audioSource.maxDistance = 50f;

            // Initialize particles
            if (musicParticles != null)
            {
                var main = musicParticles.main;
                main.playOnAwake = false;
                main.loop = true;
                musicParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                Debug.Log("Music particles initialized");
            }
            if (impactParticles != null)
            {
                var main = impactParticles.main;
                main.playOnAwake = false;
                impactParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                Debug.Log("Impact particles initialized");
            }

            // Initialize light but keep it off
            if (speakerLight != null)
            {
                speakerLight.gameObject.SetActive(false);
            }

            isPlaying = false;
            RegisterCustomEyeStates();
        }

        private void RegisterCustomEyeStates()
        {
            if (trackData == null) return;

            for (int i = 0; i < trackData.tracks.Count; i++)
            {
                var track = trackData.GetTrackData(i);
                if (track == null) continue;

                var eyeState = (PlayerHealth.EyeOverrideState)(100 + i);
                CustomEyeStates.TrackEyeStates[i] = eyeState;
                CustomEyeStates.CustomStates[eyeState] = (
                    track.eyeColor,
                    track.pupilColor,
                    track.eyeColor,
                    track.lightIntensity
                );
            }
        }

        private void OnDestroy()
        {
            if (impactDetector != null)
            {
                impactDetector.onImpactHeavy.RemoveListener(OnHeavyImpact);
            }
            ClearCurrentEyeState();
        }

        private void ClearCurrentEyeState()
        {
            if (currentEyeStateId != -1)
            {
                if (CustomEyeStates.TrackData.TryGetValue(currentEyeStateId, out var data))
                {
                    if (data.IsInitialized)
                    {
                        ChatManager.instance.playerAvatar?.playerHealth?.EyeMaterialOverride(PlayerHealth.EyeOverrideState.None, 0.25f, 0);
                    }
                    CustomEyeStates.TrackData.Remove(currentEyeStateId);
                }
                currentEyeStateId = -1;
            }
        }

        private void Update()
        {
            if (!isPlaying || trackData == null) return;

            if (Time.time >= nextMessageTime)
            {
                SendTrackMessage();
            }

            // Handle pickup and state changes
            if (physGrabObject != null)
            {
                if (physGrabObject.grabbed && !isPlaying)
                {
                    StartMusic();
                }
                else if (physGrabObject.grabbed && isPlaying && Time.time >= nextMessageTime)
                {
                    SendTrackMessage();
                }
            }

            // Update light intensity when active
            if (isPlaying && speakerLight != null && speakerLight.gameObject.activeSelf)
            {
                var track = trackData.GetTrackData(currentTrack);
                if (track != null)
                {
                    speakerLight.intensity = Mathf.Lerp(speakerLight.intensity, track.lightIntensity, Time.deltaTime * 5f);
                }
            }

            // Handle drop behavior
            if (powerOffBehavior == PowerOffBehavior.OnDrop && 
                physGrabObject != null && !physGrabObject.grabbed && isPlaying)
            {
                StopMusic();
            }
        }

        private void SendTrackMessage()
        {
            if (trackData == null || ChatManager.instance == null || ChatManager.instance.StateIsPossessed()) return;
            
            // Only send messages if we're the one holding it
            if (physGrabObject == null || !physGrabObject.grabbed) return;

            var track = trackData.GetTrackData(currentTrack);
            if (track == null) return;

            string message = trackData.GetRandomMessage(currentTrack);
            if (string.IsNullOrEmpty(message)) return;

            // Calculate message duration based on message length
            float messageDuration = trackData.GetMessageDuration(currentTrack, message);

            // Clear previous eye state
            ClearCurrentEyeState();

            // Set up new eye state
            if (CustomEyeStates.TrackEyeStates.TryGetValue(currentTrack, out var eyeState))
            {
                CustomEyeStates.TrackData[currentTrack] = new EyeStateData
                {
                    Timer = Mathf.Max(messageDuration + 1.0f, 2f),
                    IsSpeaking = true,
                    IsInitialized = false
                };

                StartCoroutine(InitializeSpeakingState(currentTrack));
            }

            // Override pupil size
            ChatManager.instance.playerAvatar.OverridePupilSize(track.pupilSize, 4, 1f, 1f, 15f, 0.3f);

            // Find closest player for targeting
            string playerName = "friend";
            if (SemiFunc.IsMultiplayer())
            {
                List<PlayerAvatar> nearbyPlayers = SemiFunc.PlayerGetAllPlayerAvatarWithinRange(10f, transform.position);
                PlayerAvatar? closestPlayer = null;
                float closestDistance = float.MaxValue;

                foreach (PlayerAvatar player in nearbyPlayers)
                {
                    if (player != PlayerAvatar.instance)
                    {
                        float distance = Vector3.Distance(transform.position, player.transform.position);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestPlayer = player;
                        }
                    }
                }

                if (closestPlayer != null)
                {
                    playerName = closestPlayer.playerName;
                }
            }

            // Replace player name in message
            message = message.Replace("{playerName}", playerName);

            // Start TTS schedule and send message
            ChatManager.instance.PossessChatScheduleStart(10);
            ChatManager.instance.PossessChat((ChatManager.PossessChatID)1001, message, messageDuration, track.eyeColor, 0f, false, 0, null);
            ChatManager.instance.PossessChatScheduleEnd();

            // Update next message time
            nextMessageTime = Time.time + trackData.messageFrequency + Random.Range(-trackData.messageRandomness, trackData.messageRandomness);
        }

        private IEnumerator InitializeSpeakingState(int trackIndex)
        {
            yield return new WaitForSeconds(0.5f);
            if (CustomEyeStates.TrackData.TryGetValue(trackIndex, out var data))
            {
                data.IsInitialized = true;
            }
        }

        private void OnHeavyImpact()
        {
            if (!isPlaying) return;

            bool isHeld = physGrabObject != null && physGrabObject.grabbed;

            // Only host handles impact effects
            if (SemiFunc.IsMasterClientOrSingleplayer())
            {
                // Handle power off behavior
                switch (powerOffBehavior)
                {
                    case PowerOffBehavior.OnSlam:
                        StopMusic();
                        return; // Exit early if we're turning off
                    case PowerOffBehavior.OnSlamWhenHeld:
                        if (isHeld)
                        {
                            StopMusic();
                            return; // Exit early if we're turning off
                        }
                        break;
                    case PowerOffBehavior.OnSlamWhenNotHeld:
                        if (!isHeld)
                        {
                            StopMusic();
                            return; // Exit early if we're turning off
                        }
                        break;
                }

                // Handle skip behavior if we didn't turn off
                switch (skipBehavior)
                {
                    case SkipBehavior.SkipAlways:
                        NextTrack();
                        break;
                    case SkipBehavior.SkipWhenHeld:
                        if (isHeld)
                        {
                            NextTrack();
                        }
                        break;
                    case SkipBehavior.SkipWhenNotHeld:
                        if (!isHeld)
                        {
                            NextTrack();
                        }
                        break;
                }
            }
        }

        private void OnMediumImpact()
        {
            if (!isPlaying) return;
            
            bool isHeld = physGrabObject != null && physGrabObject.grabbed;
            
            // Only host handles impact effects
            if (SemiFunc.IsMasterClientOrSingleplayer())
            {
                switch (skipBehavior)
                {
                    case SkipBehavior.SkipAlways:
                        NextTrack();
                        break;
                    case SkipBehavior.SkipWhenHeld:
                        if (isHeld)
                        {
                            NextTrack();
                        }
                        break;
                    case SkipBehavior.SkipWhenNotHeld:
                        if (!isHeld)
                        {
                            NextTrack();
                        }
                        break;
                }
            }
        }

        private void OnLightImpact()
        {
            return;
        }

        private void StopImpactParticles()
        {
            if (impactParticles != null)
            {
                impactParticles.Stop();
                impactParticles.gameObject.SetActive(false);
            }
        }

        public void StartMusic()
        {
            if (isPlaying)
            {
                Debug.Log("Music is already playing");
                return;
            }
            if (trackData == null)
            {
                Debug.LogError("Cannot start music: trackData is null");
                return;
            }
            Debug.Log($"Starting music with track index {currentTrackIndex}");
            isPlaying = true;
            
            // Play power on sound if we have one
            if (powerOnSound?.clip != null)
            {
                AudioSource.PlayClipAtPoint(powerOnSound.clip, transform.position, powerOnSound.volume);
                Debug.Log("Power on sound played");
            }

            // Start first track with delay
            StartCoroutine(StartFirstTrackWithDelay());

            // Sync state with other players
            if (SemiFunc.IsMultiplayer())
            {
                photonView.RPC("StartMusicRPC", RpcTarget.Others);
            }
        }

        [PunRPC]
        private void StartMusicRPC()
        {
            if (!isPlaying)
            {
                isPlaying = true;
                StartCoroutine(StartFirstTrackWithDelay());
            }
        }

        private IEnumerator StartFirstTrackWithDelay()
        {
            yield return new WaitForSeconds(initialTrackDelay);
            PlayTrack(currentTrackIndex);
        }

        public void StopMusic()
        {
            if (!isPlaying) return;
            isPlaying = false;

            // Only host controls particles
            if (SemiFunc.IsMasterClientOrSingleplayer())
            {
                // Stop particles
                if (musicParticles != null)
                {
                    musicParticles.Stop();
                    musicParticles.gameObject.SetActive(false);
                }
                if (impactParticles != null)
                {
                    impactParticles.gameObject.SetActive(true);
                    impactParticles.Play();
                }

                // Stop RGB cycling
                if (rgbCycleCoroutine != null)
                {
                    StopCoroutine(rgbCycleCoroutine);
                    rgbCycleCoroutine = null;
                }
            }

            // Play power off sound if we have one
            if (powerOffSound?.clip != null)
            {
                AudioSource.PlayClipAtPoint(powerOffSound.clip, transform.position);
            }
            
            if (speakerLight != null)
            {
                speakerLight.gameObject.SetActive(false);
            }

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeOut());

            // Sync state with other players
            if (SemiFunc.IsMultiplayer())
            {
                photonView.RPC("StopMusicRPC", RpcTarget.Others);
            }
        }

        [PunRPC]
        private void StopMusicRPC()
        {
            if (isPlaying)
            {
                isPlaying = false;
                
                // Stop particles for all players
                if (musicParticles != null)
                {
                    musicParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    musicParticles.gameObject.SetActive(false);
                }
                if (impactParticles != null)
                {
                    impactParticles.gameObject.SetActive(true);
                    impactParticles.Play();
                }

                // Stop RGB cycling
                if (rgbCycleCoroutine != null)
                {
                    StopCoroutine(rgbCycleCoroutine);
                    rgbCycleCoroutine = null;
                }

                // Turn off light
                if (speakerLight != null)
                {
                    speakerLight.gameObject.SetActive(false);
                }

                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                }
                fadeCoroutine = StartCoroutine(FadeOut());
            }
        }

        public void ToggleMusic()
        {
            if (isPlaying)
            {
                StopMusic();
            }
            else
            {
                StartMusic();
            }
        }

        public void NextTrack()
        {
            if (!isPlaying || trackData == null) return;
            currentTrackIndex = (currentTrackIndex + 1) % trackData.tracks.Count;
            PlayTrack(currentTrackIndex);
            if (skipSound?.clip != null)
            {
                AudioSource.PlayClipAtPoint(skipSound.clip, transform.position);
            }

            // Sync track change with other players
            if (SemiFunc.IsMultiplayer())
            {
                photonView.RPC("NextTrackRPC", RpcTarget.Others, currentTrackIndex);
            }
        }

        [PunRPC]
        private void NextTrackRPC(int trackIndex)
        {
            if (isPlaying && trackData != null)
            {
                currentTrackIndex = trackIndex;
                PlayTrack(currentTrackIndex);
            }
        }

        private void PlayTrack(int index)
        {
            if (trackData == null)
            {
                Debug.LogError("PlayTrack failed: trackData is null");
                return;
            }
            
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            // Stop any existing RGB cycling
            if (rgbCycleCoroutine != null)
            {
                StopCoroutine(rgbCycleCoroutine);
            }

            // Get the track data and set up audio
            var track = trackData.GetTrackData(index);
            if (track == null)
            {
                Debug.LogError($"PlayTrack failed: No track data found for index {index}");
                return;
            }

            if (track.sound == null)
            {
                Debug.LogError($"PlayTrack failed: Track {index} has no sound data");
                return;
            }

            if (track.sound.clip == null)
            {
                Debug.LogError($"PlayTrack failed: Track {index} sound has no audio clip");
                return;
            }

            Debug.Log($"Setting up audio for track {index}: clip={track.sound.clip.name}, volume={volume * musicVolumeMultiplier}");
            
            // Configure audio source
            audioSource.clip = track.sound.clip;
            audioSource.loop = true;
            audioSource.spatialBlend = track.sound.spatialBlend;
            audioSource.dopplerLevel = track.sound.doppler;
            audioSource.reverbZoneMix = track.sound.reverbMix;
            audioSource.Play();

            // Start fade in
            fadeCoroutine = StartCoroutine(FadeIn());

            // Handle particles
            if (track.trackParticles != null)
            {
                // Use track-specific particles
                if (musicParticles != null)
                {
                    musicParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    musicParticles.gameObject.SetActive(false);
                }
                
                track.trackParticles.gameObject.SetActive(true);
                track.trackParticles.Play();
                Debug.Log("Track-specific particles enabled and playing");
            }
            else if (musicParticles != null)
            {
                // Use default music particles
                musicParticles.gameObject.SetActive(true);
                musicParticles.Play();
                Debug.Log("Default music particles enabled and playing");
            }

            // Enable and set up light
            if (speakerLight != null)
            {
                speakerLight.gameObject.SetActive(true);
                speakerLight.color = track.lightColor;
                speakerLight.intensity = 0f; // Start at 0, will lerp to track's light intensity
                Debug.Log($"Speaker light enabled with color {track.lightColor} and target intensity {track.lightIntensity}");

                // Start RGB cycling if enabled
                if (track.lightColorRGB)
                {
                    rgbCycleCoroutine = StartCoroutine(RGBCycleLight());
                }
            }

            // Set up message timing
            nextMessageTime = Time.time + trackData.messageFrequency + Random.Range(-trackData.messageRandomness, trackData.messageRandomness);
            currentTrack = index;
        }

        private IEnumerator FadeIn()
        {
            float startVolume = audioSource.volume;
            float targetVolume = volume * musicVolumeMultiplier;
            float elapsedTime = 0f;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsedTime / fadeDuration);
                yield return null;
            }

            audioSource.volume = targetVolume;
        }

        private IEnumerator FadeOut()
        {
            float startVolume = audioSource.volume;
            float elapsedTime = 0f;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsedTime / fadeDuration);
                yield return null;
            }

            audioSource.volume = 0f;
            audioSource.Stop();
        }

        private IEnumerator RGBCycleLight()
        {
            float hue = 0f;
            while (true)
            {
                hue = (hue + Time.deltaTime * rgbCycleSpeed) % 1f;
                if (speakerLight != null)
                {
                    speakerLight.color = Color.HSVToRGB(hue, 1f, 1f);
                }
                yield return null;
            }
        }

        [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.EyeMaterialSetup))]
        private class EyeMaterialSetupPatch
        {
            private static void Postfix(PlayerHealth __instance)
            {
                if (CustomEyeStates.CustomStates.TryGetValue(__instance.overrideEyeState, out var customState))
                {
                    __instance.overrideEyeMaterialColor = customState.eyeColor;
                    __instance.overridePupilMaterialColor = customState.pupilColor;
                    __instance.overrideEyeLightColor = customState.lightColor;
                    __instance.overrideEyeLightIntensity = customState.lightIntensity;
                }
            }
        }

        [HarmonyPatch(typeof(ChatManager), nameof(ChatManager.StatePossessed))]
        private class ChatManagerStatePossessedPatch
        {
            public static void Postfix(ChatManager __instance)
            {
                if (__instance.playerAvatar == null) return;

                // Only apply eye state if the player is holding a powered-on speaker
                bool isHoldingSpeaker = false;
                if (PhysGrabber.instance != null && PhysGrabber.instance.grabbed)
                {
                    var handler = PhysGrabber.instance.grabbedPhysGrabObject?.GetComponent<MusicalItemHandler>();
                    isHoldingSpeaker = handler != null && handler.isPlaying;
                }

                if (isHoldingSpeaker)
                {
                    foreach (var kvp in CustomEyeStates.TrackData)
                    {
                        int index = kvp.Key;
                        var data = kvp.Value;
                        if (data.IsSpeaking && CustomEyeStates.TrackEyeStates.TryGetValue(index, out var eyeState))
                        {
                            __instance.playerAvatar.playerHealth.EyeMaterialOverride(eyeState, 0.25f, 0);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ChatManager), nameof(ChatManager.PossessChatCustomLogic))]
        private class ChatManagerPossessChatCustomLogicPatch
        {
            public static bool Prefix(ChatManager __instance)
            {
                if (__instance.playerAvatar == null) return true;

                // Only apply eye state if the player is holding a powered-on speaker
                bool isHoldingSpeaker = false;
                if (PhysGrabber.instance != null && PhysGrabber.instance.grabbed)
                {
                    var handler = PhysGrabber.instance.grabbedPhysGrabObject?.GetComponent<MusicalItemHandler>();
                    isHoldingSpeaker = handler != null && handler.isPlaying;
                }

                if (isHoldingSpeaker)
                {
                    foreach (var kvp in CustomEyeStates.TrackData)
                    {
                        int index = kvp.Key;
                        var data = kvp.Value;
                        if (data.IsSpeaking && CustomEyeStates.TrackEyeStates.TryGetValue(index, out var eyeState))
                        {
                            __instance.playerAvatar.playerHealth.EyeMaterialOverride(eyeState, 0.25f, 0);
                        }
                    }
                }
                return true;
            }

            public static void Postfix(ChatManager __instance)
            {
                if (__instance.playerAvatar?.voiceChat?.ttsVoice?.isSpeaking == true)
                {
                    foreach (var kvp in CustomEyeStates.TrackData)
                    {
                        int index = kvp.Key;
                        var data = kvp.Value;

                        if (data.IsInitialized && !data.IsSpeaking)
                        {
                            data.IsSpeaking = true;
                        }

                        data.Timer = 0.2f;
                    }
                }
                else
                {
                    // Reset eye state when TTS is done
                    foreach (var kvp in CustomEyeStates.TrackData)
                    {
                        int index = kvp.Key;
                        var data = kvp.Value;
                        
                        if (data.IsSpeaking)
                        {
                            data.IsSpeaking = false;
                            data.Timer = 0f;
                            __instance.playerAvatar?.playerHealth?.EyeMaterialOverride(PlayerHealth.EyeOverrideState.None, 0.25f, 0);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ChatManager), nameof(ChatManager.Update))]
        private class ChatManagerUpdatePatch
        {
            private static void Postfix(ChatManager __instance)
            {
                if (__instance.playerAvatar == null) return;

                // Only apply eye state if the player is holding a powered-on speaker
                bool isHoldingSpeaker = false;
                if (PhysGrabber.instance != null && PhysGrabber.instance.grabbed)
                {
                    var handler = PhysGrabber.instance.grabbedPhysGrabObject?.GetComponent<MusicalItemHandler>();
                    isHoldingSpeaker = handler != null && handler.isPlaying;
                }

                foreach (var kvp in CustomEyeStates.TrackData.ToList())
                {
                    int index = kvp.Key;
                    var data = kvp.Value;

                    if (data.Timer > 0f)
                        data.Timer -= Time.deltaTime;

                    if (data.Timer <= 0f)
                    {
                        if (data.IsInitialized && isHoldingSpeaker)
                        {
                            __instance.playerAvatar.playerHealth.EyeMaterialOverride(PlayerHealth.EyeOverrideState.None, 0.25f, 0);
                        }

                        CustomEyeStates.TrackData.Remove(index);
                    }
                }
            }
        }
    }
} 