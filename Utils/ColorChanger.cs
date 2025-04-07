using UnityEngine;
using System.Collections.Generic;
using REPOLib.Modules;

namespace PaintedUtils
{
    public class ColorChanger : MonoBehaviour
    {
        [System.Serializable]
        public class ColorPreset
        {
            public string presetName;
            public Color color = Color.white;
            public float emissionIntensity = 0f;
            public bool affectChildren = true;
        }

        [SerializeField] private List<ColorPreset> colorPresets = new List<ColorPreset>();
        [SerializeField] private string currentPresetName;
        [SerializeField] private bool applyOnAwake = false;
        
        private Dictionary<string, ColorPreset> presetLookup = new Dictionary<string, ColorPreset>();
        private List<Renderer> affectedRenderers = new List<Renderer>();
        private List<Material> originalMaterials = new List<Material>();

        // Public accessor properties
        public List<ColorPreset> ColorPresets => colorPresets;
        public string CurrentPresetName => currentPresetName;

        private void Awake()
        {
            // Build lookup dictionary for faster access
            presetLookup.Clear();
            foreach (var preset in colorPresets)
            {
                if (!string.IsNullOrEmpty(preset.presetName) && !presetLookup.ContainsKey(preset.presetName))
                {
                    presetLookup.Add(preset.presetName, preset);
                }
            }

            // Cache renderers
            CacheRenderers();

            if (applyOnAwake && !string.IsNullOrEmpty(currentPresetName))
            {
                ApplyColorPreset(currentPresetName);
            }
        }

        private void CacheRenderers()
        {
            affectedRenderers.Clear();
            originalMaterials.Clear();

            // Get all renderers on this object and children
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                affectedRenderers.Add(renderer);
                
                // Store original materials
                Material[] materials = renderer.materials;
                foreach (var mat in materials)
                {
                    // We're storing a clone to avoid modifying the original
                    originalMaterials.Add(new Material(mat));
                }
            }
        }

        public void ApplyColorPreset(string presetName)
        {
            if (string.IsNullOrEmpty(presetName) || !presetLookup.ContainsKey(presetName))
            {
                Debug.LogWarning($"Color preset '{presetName}' not found on {gameObject.name}");
                return;
            }

            currentPresetName = presetName;
            ColorPreset preset = presetLookup[presetName];
            
            int rendererIndex = 0;
            foreach (var renderer in affectedRenderers)
            {
                if (!preset.affectChildren && renderer.transform != transform)
                {
                    rendererIndex++;
                    continue;
                }
                
                Material[] materials = renderer.materials;
                
                for (int i = 0; i < materials.Length; i++)
                {
                    // Apply color
                    materials[i].color = preset.color;
                    
                    // Apply emission if supported
                    if (materials[i].HasProperty("_EmissionColor") && preset.emissionIntensity > 0)
                    {
                        materials[i].EnableKeyword("_EMISSION");
                        materials[i].SetColor("_EmissionColor", preset.color * preset.emissionIntensity);
                    }
                    else if (materials[i].HasProperty("_EmissionColor"))
                    {
                        materials[i].DisableKeyword("_EMISSION");
                    }
                }
                
                renderer.materials = materials;
                rendererIndex++;
            }
            
            Debug.Log($"Applied color preset '{presetName}' to {gameObject.name}");
        }

        public void ResetToOriginalMaterials()
        {
            int rendererIndex = 0;
            int materialIndex = 0;
            
            foreach (var renderer in affectedRenderers)
            {
                Material[] materials = renderer.materials;
                
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materialIndex < originalMaterials.Count)
                    {
                        materials[i] = new Material(originalMaterials[materialIndex]);
                        materialIndex++;
                    }
                }
                
                renderer.materials = materials;
                rendererIndex++;
            }
            
            Debug.Log($"Reset materials on {gameObject.name} to original state");
        }

        public void AddColorPreset(string name, Color color, float emissionIntensity = 0f, bool affectChildren = true)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("Cannot add color preset: name is empty");
                return;
            }
            
            // Remove existing preset with the same name if it exists
            colorPresets.RemoveAll(p => p.presetName == name);
            
            // Create and add new preset
            ColorPreset newPreset = new ColorPreset
            {
                presetName = name,
                color = color,
                emissionIntensity = emissionIntensity,
                affectChildren = affectChildren
            };
            
            colorPresets.Add(newPreset);
            
            // Update lookup dictionary
            if (presetLookup.ContainsKey(name))
            {
                presetLookup[name] = newPreset;
            }
            else
            {
                presetLookup.Add(name, newPreset);
            }
            
            Debug.Log($"Added color preset '{name}' to {gameObject.name}");
        }

        // Static method to apply a color to any game object
        public static void ColorObject(GameObject targetObject, Color color, float emissionIntensity = 0f)
        {
            Renderer[] renderers = targetObject.GetComponentsInChildren<Renderer>(true);
            
            foreach (var renderer in renderers)
            {
                Material[] materials = renderer.materials;
                
                for (int i = 0; i < materials.Length; i++)
                {
                    // Apply color
                    materials[i].color = color;
                    
                    // Apply emission if supported
                    if (materials[i].HasProperty("_EmissionColor") && emissionIntensity > 0)
                    {
                        materials[i].EnableKeyword("_EMISSION");
                        materials[i].SetColor("_EmissionColor", color * emissionIntensity);
                    }
                    else if (materials[i].HasProperty("_EmissionColor"))
                    {
                        materials[i].DisableKeyword("_EMISSION");
                    }
                }
                
                renderer.materials = materials;
            }
            
            Debug.Log($"Applied color to {targetObject.name}");
        }

        // Static method to create and setup a ColorChanger on any GameObject
        public static ColorChanger SetupOnObject(GameObject targetObject)
        {
            // Get existing or add new ColorChanger component
            ColorChanger changer = targetObject.GetComponent<ColorChanger>();
            if (changer == null)
            {
                changer = targetObject.AddComponent<ColorChanger>();
                Debug.Log($"Added ColorChanger component to {targetObject.name}");
            }
            
            // Initialize with some default presets
            if (changer.colorPresets.Count == 0)
            {
                changer.AddColorPreset("Default", Color.white);
                changer.AddColorPreset("Red", Color.red, 1.5f);
                changer.AddColorPreset("Green", Color.green, 1.5f);
                changer.AddColorPreset("Blue", Color.blue, 1.5f);
                changer.AddColorPreset("Yellow", Color.yellow, 1.5f);
                changer.AddColorPreset("Purple", new Color(0.5f, 0f, 0.5f), 1.5f);
                changer.AddColorPreset("Glowing", Color.white, 2.0f);
            }
            
            return changer;
        }
    }
} 