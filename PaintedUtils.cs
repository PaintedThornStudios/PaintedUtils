using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.IO;
using System.Linq;
using REPOLib;
using REPOLib.Modules;
using REPOLib.Commands;
using System;

namespace PaintedUtils
{
    /// <summary>
    /// PaintedUtils - A collection of utility components for REPO games
    /// 
    /// Included utilities:
    /// - ColorChanger: Modify colors and emission of objects at runtime
    /// - ItemDropper: Create drop tables for items and handle item spawning
    /// - ItemLibrary: Standard implementation for interactive items
    /// - ItemBattery: Battery power system for electronic items
    /// </summary>
    [BepInPlugin("PaintedThornStudios.PaintedUtils", "PaintedUtils", "1.0")]
    [BepInDependency(REPOLib.MyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
    public class PaintedUtils : BaseUnityPlugin
    {
        private static string GetModName()
        {
            var attribute = typeof(PaintedUtils).GetCustomAttributes(typeof(BepInPlugin), false)[0] as BepInPlugin;
            return attribute?.Name ?? "PaintedUtils";
        }

        private static readonly string BundleName = GetModName();
        internal static PaintedUtils Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger => Instance._logger;
        private ManualLogSource _logger => base.Logger;
        internal Harmony? Harmony { get; set; }
        private AssetBundle? _assetBundle;
        private bool _hasFixedAudioMixerGroups = false;

        private void LoadAssetBundle()
        {
            if (_assetBundle != null) return;

            string modPath = Path.GetDirectoryName(Info.Location);
            string bundlePath = Path.Combine(modPath, $"{BundleName}.bundle");
            
            _assetBundle = AssetBundle.LoadFromFile(bundlePath);
            if (_assetBundle == null)
            {
                Logger.LogError($"Failed to load bundle from {bundlePath}");
                return;
            }
        }


        private void FixAllPrefabAudioMixerGroups()
        {
            if (_assetBundle == null) return;

            var allPrefabs = _assetBundle.GetAllAssetNames()
                .Where(name => name.EndsWith(".prefab"))
                .Select(name => _assetBundle.LoadAsset<GameObject>(name))
                .ToList();

            foreach (var prefab in allPrefabs)
            {
                REPOLib.Modules.Utilities.FixAudioMixerGroups(prefab);
            }
        }

        private void Awake()
        {
            Instance = this;
            
            this.gameObject.transform.parent = null;
            this.gameObject.hideFlags = HideFlags.HideAndDontSave;

            Patch();

            LoadAssetBundle();

            Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
            Logger.LogInfo($"White Roses have been painted Red! @CarsonJF");
        }

        internal void Patch()
        {
            Harmony ??= new Harmony(Info.Metadata.GUID);
            Harmony.PatchAll();
        }

        internal void Unpatch()
        {
            Harmony?.UnpatchSelf();
        }

        private void Update()
        {
            if (!_hasFixedAudioMixerGroups)
            {
                FixAllPrefabAudioMixerGroups();
                _hasFixedAudioMixerGroups = true;
            }
        }
    }
} 