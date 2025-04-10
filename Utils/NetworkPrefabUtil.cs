using UnityEngine;
using BepInEx.Logging;
using REPOLib;
using REPOLib.Modules;
using System.Linq;

namespace PaintedThornStudios.PaintedUtils;

/// <summary>
/// Utility class for managing network prefabs in mods
/// </summary>
public class NetworkPrefabUtil
{
    /// <summary>
    /// Registers network prefabs from an asset bundle
    /// </summary>
    /// <param name="bundle">The asset bundle containing the prefabs</param>
    /// <param name="logger">Logger instance for error reporting</param>
    public static void RegisterNetworkPrefabs(AssetBundle bundle, ManualLogSource logger)
    {
        if (bundle == null) return;

        var networkPrefabs = bundle.GetAllAssetNames()
            .Where(name => name.Contains("/prefabs/") && name.EndsWith(".prefab"))
            .Select(name => bundle.LoadAsset<GameObject>(name))
            .ToList();

        foreach (var prefab in networkPrefabs)
        {
            NetworkPrefabs.RegisterNetworkPrefab(prefab);
        }

        if (networkPrefabs.Count > 0)
        {
            logger.LogInfo($"Successfully registered {networkPrefabs.Count} network prefabs through REPOLib");
        }
    }
} 