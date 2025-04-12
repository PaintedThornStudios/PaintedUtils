using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace PaintedUtils
{
    public class EyeStateManager : MonoBehaviour
    {
        private static EyeStateManager? instance;
        public static EyeStateManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("EyeStateManager");
                    instance = go.AddComponent<EyeStateManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private class EyeStateData
        {
            public float Timer;
            public bool IsSpeaking;
            public bool IsInitialized;
            public Color EyeColor;
            public Color PupilColor;
            public float LightIntensity;
            public bool EyeColorRGB;
            public bool PupilColorRGB;
            public float RGBCycleSpeed = 0.5f;
            private float eyeHue = 0f;
            private float pupilHue = 0f;

            public void UpdateRGBColors()
            {
                if (EyeColorRGB)
                {
                    eyeHue = (eyeHue + Time.deltaTime * RGBCycleSpeed) % 1f;
                    EyeColor = Color.HSVToRGB(eyeHue, 1f, 1f);
                }
                if (PupilColorRGB)
                {
                    pupilHue = (pupilHue + Time.deltaTime * RGBCycleSpeed) % 1f;
                    PupilColor = Color.HSVToRGB(pupilHue, 1f, 1f);
                }
            }
        }

        private Dictionary<int, EyeStateData> activeStates = new();
        private int nextStateId = 1;

        private void Update()
        {
            // Update timers for timed states
            foreach (var kvp in activeStates.ToList())
            {
                var data = kvp.Value;
                if (data.Timer > 0f)
                {
                    data.Timer -= Time.deltaTime;
                    if (data.Timer <= 0f)
                    {
                        if (data.IsInitialized)
                        {
                            ResetEyes();
                        }
                        activeStates.Remove(kvp.Key);
                    }
                }

                // Update RGB colors if enabled
                if (data.IsInitialized && (data.EyeColorRGB || data.PupilColorRGB))
                {
                    data.UpdateRGBColors();
                    ApplyEyeState(data);
                }
            }
        }

        public int SetEyeState(Color eyeColor, Color pupilColor, float lightIntensity, float duration = 0f, bool eyeColorRGB = false, bool pupilColorRGB = false)
        {
            int stateId = nextStateId++;
            var data = new EyeStateData
            {
                Timer = duration,
                IsSpeaking = true,
                IsInitialized = false,
                EyeColor = eyeColor,
                PupilColor = pupilColor,
                LightIntensity = lightIntensity,
                EyeColorRGB = eyeColorRGB,
                PupilColorRGB = pupilColorRGB
            };

            activeStates[stateId] = data;
            ApplyEyeState(data);
            StartCoroutine(InitializeSpeakingState(stateId));
            return stateId;
        }

        public void ClearEyeState(int stateId)
        {
            if (activeStates.TryGetValue(stateId, out var data))
            {
                if (data.IsInitialized)
                {
                    ResetEyes();
                }
                activeStates.Remove(stateId);
            }
        }

        private void ApplyEyeState(EyeStateData data)
        {
            if (ChatManager.instance?.playerAvatar?.playerHealth == null) return;

            ChatManager.instance.playerAvatar.playerHealth.overrideEyeMaterialColor = data.EyeColor;
            ChatManager.instance.playerAvatar.playerHealth.overridePupilMaterialColor = data.PupilColor;
            ChatManager.instance.playerAvatar.playerHealth.overrideEyeLightColor = data.EyeColor;
            ChatManager.instance.playerAvatar.playerHealth.overrideEyeLightIntensity = data.LightIntensity;
            ChatManager.instance.playerAvatar.playerHealth.EyeMaterialOverride((PlayerHealth.EyeOverrideState)100, 0.25f, 0);
        }

        private void ResetEyes()
        {
            if (ChatManager.instance?.playerAvatar?.playerHealth == null) return;
            ChatManager.instance.playerAvatar.playerHealth.EyeMaterialOverride(PlayerHealth.EyeOverrideState.None, 0.25f, 0);
        }

        private System.Collections.IEnumerator InitializeSpeakingState(int stateId)
        {
            yield return new WaitForSeconds(0.5f);
            if (activeStates.TryGetValue(stateId, out var data))
            {
                data.IsInitialized = true;
            }
        }

        [HarmonyPatch(typeof(ChatManager), nameof(ChatManager.StatePossessed))]
        private class ChatManagerStatePossessedPatch
        {
            public static void Postfix(ChatManager __instance)
            {
                foreach (var kvp in Instance.activeStates)
                {
                    var data = kvp.Value;
                    if (data.IsSpeaking)
                    {
                        Instance.ApplyEyeState(data);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ChatManager), nameof(ChatManager.PossessChatCustomLogic))]
        private class ChatManagerPossessChatCustomLogicPatch
        {
            public static bool Prefix(ChatManager __instance)
            {
                foreach (var kvp in Instance.activeStates)
                {
                    var data = kvp.Value;
                    if (data.IsSpeaking)
                    {
                        Instance.ApplyEyeState(data);
                    }
                }
                return true;
            }

            public static void Postfix(ChatManager __instance)
            {
                if (__instance.playerAvatar?.voiceChat?.ttsVoice?.isSpeaking == true)
                {
                    foreach (var kvp in Instance.activeStates)
                    {
                        var data = kvp.Value;
                        if (data.IsInitialized && !data.IsSpeaking)
                        {
                            data.IsSpeaking = true;
                        }
                        data.Timer = 0.2f;
                    }
                }
            }
        }
    }
} 