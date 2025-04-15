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

        private Coroutine fadeCoroutine;
        private int currentTrackIndex = 0;
        private Coroutine rgbCycleCoroutine;
        private float rgbCycleSpeed = 0.5f; // Speed of RGB cycling in seconds per cycle


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
                // Debug.Log("Music particles initialized");
            }
            if (impactParticles != null)
            {
                var main = impactParticles.main;
                main.playOnAwake = false;
                impactParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                // Debug.Log("Impact particles initialized");
            }

            // Initialize light but keep it off
            if (speakerLight != null)
            {
                speakerLight.gameObject.SetActive(false);
            }

            isPlaying = false;
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
            string localPlayerName = ChatManager.instance.playerAvatar.playerName;
            EyeStateManager.Instance.ClearEyeState(localPlayerName);
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
            if (trackData == null || ChatManager.instance == null || ChatManager.instance.StateIsPossessed() || physGrabObject == null || !physGrabObject.heldByLocalPlayer) return;
            
            // Only send messages if we're the one holding it
            if (physGrabObject == null || !physGrabObject.grabbed) return;

            var track = trackData.GetTrackData(currentTrack);
            if (track == null) return;

            string message = trackData.GetRandomMessage(currentTrack);
            if (string.IsNullOrEmpty(message)) return;

            // Calculate message duration based on message length
            float messageDuration = trackData.GetMessageDuration(currentTrack, message);

            Color eyeColor = new Color(track.eyeColor.r, track.eyeColor.g, track.eyeColor.b, 1f);
            Color pupilColor = new Color(track.pupilColor.r, track.pupilColor.g, track.pupilColor.b, 1f);
            eyeColor.a = 1f;
            pupilColor.a = 1f;
            // Debug.Log($"Eye Color: {eyeColor} | Pupil Color: {pupilColor} | Pupil Size: {track.pupilSize}");

            string localPlayerName = ChatManager.instance.playerAvatar.playerName;

            EyeStateManager.Instance.SetEyeState(
                localPlayerName,
                eyeColor,
                pupilColor,
                track.lightIntensity,
                duration: messageDuration + 1f,
                pupilSize: track.pupilSize
                // eyeColorRGB: track.eyeColorRGB,
                // pupilColorRGB: track.pupilColorRGB
            );

            if (SemiFunc.IsMultiplayer())
            {
            photonView.RPC("SyncSpeakerEyeEffect", RpcTarget.Others,
                localPlayerName,
                new float[] { eyeColor.r, eyeColor.g, eyeColor.b, eyeColor.a },
                new float[] { pupilColor.r, pupilColor.g, pupilColor.b, pupilColor.a },
                track.lightIntensity,
                track.pupilSize,
                messageDuration + 1f);
            }

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
            ChatManager.instance.PossessChat((ChatManager.PossessChatID)1001, message, messageDuration, eyeColor, 0f, false, 0, null);
            ChatManager.instance.PossessChatScheduleEnd();

            // Update next message time
            nextMessageTime = Time.time + trackData.messageFrequency + Random.Range(-trackData.messageRandomness, trackData.messageRandomness);
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
                // Debug.Log("Music is already playing");
                return;
            }
            if (trackData == null)
            {
                Debug.LogError("Cannot start music: trackData is null");
                return;
            }
            // Debug.Log($"Starting music with track index {currentTrackIndex}");
            isPlaying = true;
            
            // Play power on sound if we have one
            if (powerOnSound?.clip != null)
            {
                AudioSource.PlayClipAtPoint(powerOnSound.clip, transform.position, powerOnSound.volume);
                if (SemiFunc.IsMultiplayer())
                {
                    photonView.RPC("PlayPowerOnSoundRPC", RpcTarget.Others);
                }
                // Debug.Log("Power on sound played");
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

            // Debug.Log($"Setting up audio for track {index}: clip={track.sound.clip.name}, volume={volume * musicVolumeMultiplier}");
            
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
                // Debug.Log("Track-specific particles enabled and playing");
            }
            else if (musicParticles != null)
            {
                // Use default music particles
                musicParticles.gameObject.SetActive(true);
                musicParticles.Play();
                // Debug.Log("Default music particles enabled and playing");
            }

            // Enable and set up light
            if (speakerLight != null)
            {
                speakerLight.gameObject.SetActive(true);
                speakerLight.color = track.lightColor;
                speakerLight.intensity = 0f; // Start at 0, will lerp to track's light intensity
                // Debug.Log($"Speaker light enabled with color {track.lightColor} and target intensity {track.lightIntensity}");

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

        [PunRPC]
        private void PlayPowerOnSoundRPC()
        {
            if (powerOnSound?.clip != null)
            {
                AudioSource.PlayClipAtPoint(powerOnSound.clip, transform.position, powerOnSound.volume);
            }
        }

        [PunRPC]
        private void SyncSpeakerEyeEffect(
            string playerName,
            float[] eyeRGBA,
            float[] pupilRGBA,
            float intensity,
            float pupilSize,
            float duration)
        {
            Color eye = new Color(eyeRGBA[0], eyeRGBA[1], eyeRGBA[2], eyeRGBA[3]);
            Color pupil = new Color(pupilRGBA[0], pupilRGBA[1], pupilRGBA[2], pupilRGBA[3]);

            EyeStateManager.Instance.SetEyeState(playerName, eye, pupil, intensity, duration, pupilSize: pupilSize);
        }

    }
} 