using UnityEngine;
using System.IO;
using BepInEx;
using BepInEx.Logging;

namespace PaintedThornStudios.PaintedUtils;

/// <summary>
/// Utility class for loading and managing asset bundles in mods
/// </summary>
public class AssetBundleUtil
{
    /// <summary>
    /// Loads an asset bundle from the mod's directory
    /// </summary>
    /// <param name="modInstance">The mod's BaseUnityPlugin instance</param>
    /// <param name="bundleName">Name of the bundle file (without .bundle extension)</param>
    /// <param name="logger">Logger instance for error reporting</param>
    /// <returns>The loaded AssetBundle or null if loading failed</returns>
    public static AssetBundle LoadAssetBundle(BaseUnityPlugin modInstance, string bundleName, ManualLogSource logger)
    {
        string modPath = Path.GetDirectoryName(modInstance.Info.Location);
        string bundlePath = Path.Combine(modPath, $"{bundleName}.bundle");
        
        AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
        if (bundle == null)
        {
            logger.LogError($"Failed to load bundle from {bundlePath}");
            return null;
        }

        return bundle;
    }

    /// <summary>
    /// Fixes audio mixer groups for all prefabs in an asset bundle
    /// </summary>
    /// <param name="bundle">The asset bundle containing the prefabs</param>
    /// <param name="logger">Logger instance for error reporting</param>
    public static void FixAudioMixerGroups(AssetBundle bundle, ManualLogSource logger)
    {
        if (bundle == null) return;

        foreach (string assetName in bundle.GetAllAssetNames())
        {
            GameObject prefab = bundle.LoadAsset<GameObject>(assetName);
            if (prefab == null) continue;

            AudioSource[] audioSources = prefab.GetComponentsInChildren<AudioSource>(true);
            foreach (AudioSource audioSource in audioSources)
            {
                if (audioSource.outputAudioMixerGroup == null)
                {
                    logger.LogWarning($"AudioSource in {assetName} has no mixer group assigned");
                }
            }
        }
    }
} 