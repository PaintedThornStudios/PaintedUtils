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

        public class EyeStateData
        {
            public float Timer;
            public bool IsSpeaking;
            public bool IsInitialized;
            public Color EyeColor;
            public Color PupilColor;
            public float LightIntensity;
            public bool EyeColorRGB = false;
            public bool PupilColorRGB = false;
            public float RGBCycleSpeed = 0.5f;
            public float PupilSize = 3f;

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

        private Dictionary<string, EyeStateData> activeStates = new();

        private void Update()
        {
            foreach (var kvp in activeStates.ToList())
            {
                var playerName = kvp.Key;
                var data = kvp.Value;

                if (data.Timer > 0f)
                {
                    data.Timer -= Time.deltaTime;
                    if (data.Timer <= 0f)
                    {
                        ResetEyes(playerName);
                        activeStates.Remove(playerName);
                        continue;
                    }
                }

                if (data.IsInitialized && (data.EyeColorRGB || data.PupilColorRGB))
                {
                    data.UpdateRGBColors();
                    ApplyEyeState(playerName, data);
                }
            }
        }

        public void SetEyeState(string playerName, Color eyeColor, Color pupilColor, float lightIntensity, float duration = 0f, bool eyeColorRGB = false, bool pupilColorRGB = false, float pupilSize = 3f)
        {
            var data = new EyeStateData
            {
                Timer = duration,
                IsSpeaking = true,
                IsInitialized = false,
                EyeColor = eyeColor,
                PupilColor = pupilColor,
                LightIntensity = lightIntensity,
                EyeColorRGB = eyeColorRGB,
                PupilColorRGB = pupilColorRGB,
                PupilSize = pupilSize
            };

            activeStates[playerName] = data;
            ApplyEyeState(playerName, data);
            StartCoroutine(InitializeSpeakingState(playerName));
        }

        public void ClearEyeState(string playerName)
        {
            if (activeStates.TryGetValue(playerName, out var data))
            {
                ResetEyes(playerName);
                activeStates.Remove(playerName);
            }
        }

        private void ApplyEyeState(string playerName, EyeStateData data)
        {
            var target = SemiFunc.PlayerGetFromName(playerName);
            if (target?.playerHealth == null) return;

            // Debug.Log($"Applying Eye Override - Eye: {data.EyeColor} | Pupil: {data.PupilColor} | Intensity: {data.LightIntensity} | Player: {playerName} | Pupil Size: {data.PupilSize}");

            target.playerHealth.overrideEyeMaterialColor = data.EyeColor;
            target.playerHealth.overridePupilMaterialColor = data.PupilColor;
            target.playerHealth.overrideEyeLightColor = data.EyeColor;
            target.playerHealth.overrideEyeLightIntensity = data.LightIntensity;
            target.playerHealth.EyeMaterialOverride((PlayerHealth.EyeOverrideState)100, 0.25f, 40);
            ChatManager.instance.playerAvatar?.OverridePupilSize(data.PupilSize, 4, 15f, 0.3f, 15f, 0.3f);
        }

        private void ResetEyes(string playerName)
        {
            var target = SemiFunc.PlayerGetFromName(playerName);
            if (target?.playerHealth == null) return;

            target.playerHealth.EyeMaterialOverride(PlayerHealth.EyeOverrideState.None, 0.25f, 0);
        }

        private System.Collections.IEnumerator InitializeSpeakingState(string playerName)
        {
            yield return new WaitForSeconds(0.5f);
            if (activeStates.TryGetValue(playerName, out var data))
            {
                data.IsInitialized = true;
            }
        }

        public EyeStateData? GetActiveStateFor(string playerName)
        {
            if (activeStates.TryGetValue(playerName, out var data))
                return data;
            return null;
        }
    }

    [HarmonyPatch(typeof(PlayerHealth), "Update")]
    public class ForceCustomEyeStatePatch
    {
        static void Prefix(PlayerHealth __instance)
        {
            if (__instance == null || __instance.photonView == null || !__instance.photonView.IsMine)
                return;

            var playerName = __instance.playerAvatar?.playerName;
            if (string.IsNullOrEmpty(playerName)) return;

            try
            {
                var state = EyeStateManager.Instance.GetActiveStateFor(playerName);
                if (state == null) return;

                __instance.overrideEyeMaterialColor = state.EyeColor;
                __instance.overridePupilMaterialColor = state.PupilColor;
                __instance.overrideEyeLightColor = state.EyeColor;
                __instance.overrideEyeLightIntensity = state.LightIntensity;

                if (__instance.overrideEyePriority < 50)
                    __instance.EyeMaterialOverride((PlayerHealth.EyeOverrideState)100, 0.25f, 50);

                // ✅ Also apply pupil size override
                __instance.playerAvatar?.OverridePupilSize(state.PupilSize, 4, 1f, 1f, 15f, 0.3f);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EyeStateManager] Failed to apply eye state for {playerName}: {e}");
            }
        }
    }
}
