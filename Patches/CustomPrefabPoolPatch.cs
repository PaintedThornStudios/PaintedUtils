using HarmonyLib;
using REPOLib.Objects;
using UnityEngine;
using BepInEx.Logging;

namespace PaintedUtils.Patches
{
    public static class CustomPrefabPoolPatch
    {
        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("CustomPrefabPoolPatch");
        private static bool isFromItemDropper = false;

        [HarmonyPatch(typeof(ItemDropper))]
        [HarmonyPatch("DropItems")]
        public static class ItemDropperPatch
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                isFromItemDropper = true;
            }

            [HarmonyPostfix]
            public static void Postfix()
            {
                isFromItemDropper = false;
            }
        }

        [HarmonyPatch(typeof(CustomPrefabPool))]
        [HarmonyPatch("Instantiate")]
        public static class CustomPrefabPoolInstantiatePatch
        {
            [HarmonyPrefix]
            public static bool Prefix(CustomPrefabPool __instance, string prefabId, Vector3 position, Quaternion rotation, ref GameObject __result)
            {
                if (!isFromItemDropper)
                {
                    return true; // Let original method handle non-ItemDropper calls
                }

                if (string.IsNullOrWhiteSpace(prefabId))
                {
                    return true; // Let original method handle the error
                }

                if (position == null)
                {
                    position = Vector3.zero;
                }

                if (rotation == null)
                {
                    rotation = Quaternion.identity;
                }

                // Get the prefab from the pool
                GameObject prefab = __instance.GetPrefab(prefabId);
                if (prefab == null)
                {
                    return true; // Let original method handle the fallback
                }

                // Spawn the prefab without logging
                bool activeSelf = prefab.activeSelf;
                if (activeSelf)
                {
                    prefab.SetActive(false);
                }

                __result = Object.Instantiate(prefab, position, rotation);

                if (activeSelf)
                {
                    prefab.SetActive(true);
                }

                return false; // Skip original method
            }
        }
    }
} 