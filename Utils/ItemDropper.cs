using UnityEngine;
using System.Collections.Generic;
using REPOLib.Modules;
namespace PaintedUtils
{
    public class ItemDropper : MonoBehaviour
    {
        public enum PrefabType { NetworkPrefab, Valuable, Item }
        public enum DropChanceType { Weight, Percentage }

        [System.Serializable]
        public class ItemDrop
        {
            public GameObject itemPrefab;
            public int minQuantity = 1;
            public int maxQuantity = 1;
            public DropChanceType dropChanceType = DropChanceType.Weight;
            [Tooltip("Used when Drop Chance Type is set to Weight")]
            public int weight = 100;
            [Range(0, 100)]
            [Tooltip("Percentage chance this item will drop. Used when Drop Chance Type is set to Percentage")]
            public float chance = 100f;
            public bool guaranteed = false;
            public PrefabType prefabType;
            public Rarity rarity;
        }

        [SerializeField] private DropTable dropTable;
        [SerializeField] private Transform dropTarget;
        [SerializeField] private float dropSpreadRadius = 0.5f;
        [SerializeField] private bool applyForce = true;
        [SerializeField] private float forceStrength = 3f;
        [SerializeField] private int minItemTypes = 1;
        [SerializeField] private int maxItemTypes = 3;

        // Public accessor properties
        public DropTable DropTable => dropTable;
        public float DropSpreadRadius => dropSpreadRadius;
        public float ForceStrength => forceStrength;

        // Method to initialize drop table from resources
        public void InitializeDropTable(string dropTableResourcePath)
        {
            if (dropTable == null)
            {
                // Debug.Log($"Attempting to load drop table from: {dropTableResourcePath}");
                dropTable = Resources.Load<DropTable>(dropTableResourcePath);
                
                if (dropTable != null)
                {
                    // Debug.Log($"Successfully loaded drop table: {dropTable.name}");
                }
                else
                {
                    // Debug.LogWarning($"Failed to load drop table from path: {dropTableResourcePath}");
                }
            }
        }

        // New method to automatically initialize drop table based on GameObject name
        public void InitializeDropTableFromName()
        {
            if (dropTable != null) return;

            // Get the base name of the GameObject
            string baseName = gameObject.name;
            
            // Remove common suffixes that might be in the name
            string[] suffixesToRemove = { "Controller", "Prefab", "(Clone)" };
            foreach (string suffix in suffixesToRemove)
            {
                if (baseName.EndsWith(suffix))
                {
                    baseName = baseName.Substring(0, baseName.Length - suffix.Length).Trim();
                }
            }

            // Try different possible paths
            string[] possiblePaths = new string[]
            {
                $"DropTables/{baseName}DropTable",
                $"DropTables/{baseName}",
                "DropTables/DefaultDropTable"
            };

            foreach (string path in possiblePaths)
            {
                InitializeDropTable(path);
                if (dropTable != null)
                {
                    // Debug.Log($"Successfully auto-loaded drop table from: {path}");
                    return;
                }
            }

            Debug.LogWarning($"Could not auto-load drop table for {gameObject.name}. Tried paths: {string.Join(", ", possiblePaths)}");
        }

        // Static method to create and initialize an ItemDropper on any GameObject
        public static ItemDropper SetupDropperOnObject(GameObject targetObject, string dropTablePath, float spreadRadius = 0.5f, float force = 3f)
        {
            // Get existing or add new ItemDropper component
            ItemDropper dropper = targetObject.GetComponent<ItemDropper>();
            if (dropper == null)
            {
                dropper = targetObject.AddComponent<ItemDropper>();
                // Debug.Log($"Added ItemDropper component to {targetObject.name}");
            }

            // Initialize the drop table
            dropper.InitializeDropTable(dropTablePath);
            
            // Configure the dropper if needed
            if (dropper.DropTable != null)
            {
                // Use reflection to set private fields since we can't modify them directly
                typeof(ItemDropper).GetField("dropSpreadRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(dropper, spreadRadius);
                typeof(ItemDropper).GetField("forceStrength", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(dropper, force);
                
                // Debug.Log($"Configured ItemDropper on {targetObject.name} with spread radius {spreadRadius} and force {force}");
            }
            
            return dropper;
        }

        // Static method to drop items for objects that are being destroyed
        public static void DropItemsAtPosition(DropTable dropTable, Vector3 position, float dropRadius = 0.5f, float force = 3f)
        {
            if (dropTable == null)
            {
                Debug.LogWarning("Cannot drop items: No drop table provided to static method");
                dropTable = Resources.Load<DropTable>("DropTables/DefaultDropTable");
                
                if (dropTable == null)
                {
                    Debug.LogError("Failed to load fallback drop table. No items will be dropped.");
                    return;
                }
            }

            // Handle all drops based on their configuration
            foreach (var drop in dropTable.drops)
            {
                bool shouldDrop = false;

                if (drop.guaranteed)
                {
                    shouldDrop = true;
                }
                else if (drop.dropChanceType == DropChanceType.Percentage)
                {
                    shouldDrop = Random.Range(0f, 100f) < drop.chance;
                }

                if (shouldDrop)
                {
                    int quantity = Random.Range(drop.minQuantity, drop.maxQuantity + 1);
                    for (int j = 0; j < quantity; j++)
                    {
                        Vector3 pos = position + Random.insideUnitSphere * dropRadius;
                        SpawnPrefabStatic(drop, pos, force);
                    }
                }
            }

            // Handle weighted drops separately
            List<ItemDrop> weightedDrops = dropTable.drops.FindAll(d => !d.guaranteed && d.dropChanceType == DropChanceType.Weight && d.weight > 0);
            if (weightedDrops.Count > 0)
            {
                int typesToDrop = Mathf.Clamp(Random.Range(1, 3 + 1), 1, weightedDrops.Count);

                for (int i = 0; i < typesToDrop; i++)
                {
                    if (weightedDrops.Count == 0) break;

                    int totalWeight = 0;
                    foreach (var drop in weightedDrops)
                        totalWeight += drop.weight;

                    int randomWeight = Random.Range(0, totalWeight);
                    int current = 0;
                    int index = 0;

                    for (int k = 0; k < weightedDrops.Count; k++)
                    {
                        current += weightedDrops[k].weight;
                        if (randomWeight < current)
                        {
                            index = k;
                            break;
                        }
                    }

                    ItemDrop selectedDrop = weightedDrops[index];
                    weightedDrops.RemoveAt(index);

                    int quantity = Random.Range(selectedDrop.minQuantity, selectedDrop.maxQuantity + 1);
                    for (int j = 0; j < quantity; j++)
                    {
                        Vector3 pos = position + Random.insideUnitSphere * dropRadius;
                        SpawnPrefabStatic(selectedDrop, pos, force);
                    }
                }
            }

            // Handle nested tables
            if (dropTable.nestedTables != null && dropTable.nestedTables.Count > 0)
            {
                if (Random.Range(0, 100) < dropTable.nestedTableChance)
                {
                    int nestedIndex = Random.Range(0, dropTable.nestedTables.Count);
                    DropTable nestedTable = dropTable.nestedTables[nestedIndex];
                    if (nestedTable != null)
                    {
                        DropItemsAtPosition(nestedTable, position, dropRadius, force);
                    }
                }
            }
        }

        // Another static method to drop items at a position, looking up a table by name
        public static void DropItemsAtPosition(string dropTableResourcePath, Vector3 position, float dropRadius = 0.5f, float force = 3f)
        {
            // Debug.Log($"Attempting to drop items with table from path: {dropTableResourcePath}");
            DropTable table = Resources.Load<DropTable>(dropTableResourcePath);
            DropItemsAtPosition(table, position, dropRadius, force);
        }

        // Static method to drop items for a GameObject that's being destroyed
        public static void DropItemsForGameObject(GameObject sourceObject, string dropTableResourcePath = null)
        {
            if (sourceObject == null)
            {
                Debug.LogError("Cannot drop items: Source GameObject is null");
                return;
            }

            // Check if the object has an ItemDropper component
            ItemDropper dropper = sourceObject.GetComponent<ItemDropper>();
            if (dropper != null && dropper.DropTable != null)
            {
                // Use the component's drop table and settings
                DropItemsAtPosition(dropper.DropTable, sourceObject.transform.position, dropper.DropSpreadRadius, dropper.ForceStrength);
                // Debug.Log($"Dropped items for {sourceObject.name} using its ItemDropper component");
                return;
            }
            
            // If no dropper component with valid table, try the resource path
            if (!string.IsNullOrEmpty(dropTableResourcePath))
            {
                DropItemsAtPosition(dropTableResourcePath, sourceObject.transform.position);
                // Debug.Log($"Dropped items for {sourceObject.name} using resource path: {dropTableResourcePath}");
                return;
            }
            
            // Last resort - try a default drop table
            DropItemsAtPosition((DropTable)null, sourceObject.transform.position);
        }

        private static void SpawnPrefabStatic(ItemDrop drop, Vector3 position, float forceStrength)
        {
            GameObject spawned = null;

            switch (drop.prefabType)
            {
                case PrefabType.NetworkPrefab:
                    spawned = NetworkPrefabs.SpawnNetworkPrefab(drop.itemPrefab.name, position, Quaternion.identity);
                    break;
                case PrefabType.Valuable:
                    spawned = Valuables.SpawnValuable(Valuables.GetValuableByName(drop.itemPrefab.name), position, Quaternion.identity);
                    if (spawned == null)
                        Debug.LogWarning($"Could not spawn Valuable: {drop.itemPrefab.name}");
                    break;
                case PrefabType.Item:
                    string itemName = drop.itemPrefab.name;
                    if (!itemName.StartsWith("Item ")) itemName = "Item " + itemName;
                    spawned = NetworkPrefabs.SpawnNetworkPrefab(itemName, position, Quaternion.identity);
                    if (spawned == null)
                        Debug.LogWarning($"Could not spawn Item: {itemName}");
                    break;
            }

            if (spawned != null && forceStrength > 0)
            {
                Rigidbody rb = spawned.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(Random.onUnitSphere * forceStrength, ForceMode.Impulse);
                }
            }

            if (drop.rarity != null && spawned != null)
            {
                var effectHandle = RarityEffectManager.TryPlayEffects(drop.rarity, position, spawned.transform);
                
                // Optional: Attach the effect handle to the item if you want to access it later
                var tracker = spawned.AddComponent<RarityEffectTracker>();
                tracker.effectHandle = effectHandle;
            }
        }

        public void DropItems()
        {
            if (dropTable == null)
            {
                InitializeDropTableFromName();
                if (dropTable == null)
                {
                    Debug.LogWarning($"No drop table found for {gameObject.name}");
                    return;
                }
            }

            // Handle all drops based on their configuration
            foreach (var drop in dropTable.drops)
            {
                bool shouldDrop = false;

                if (drop.guaranteed)
                {
                    shouldDrop = true;
                }
                else if (drop.dropChanceType == DropChanceType.Percentage)
                {
                    shouldDrop = Random.Range(0f, 100f) < drop.chance;
                }

                if (shouldDrop)
                {
                    int quantity = Random.Range(drop.minQuantity, drop.maxQuantity + 1);
                    for (int j = 0; j < quantity; j++)
                    {
                        Vector3 pos = GetSpreadPosition(transform.position);
                        SpawnPrefab(drop, pos);
                    }
                }
            }

            // Handle weighted drops separately
            List<ItemDrop> weightedDrops = dropTable.drops.FindAll(d => !d.guaranteed && d.dropChanceType == DropChanceType.Weight && d.weight > 0);
            if (weightedDrops.Count > 0)
            {
                int typesToDrop = Mathf.Clamp(Random.Range(minItemTypes, maxItemTypes + 1), 1, weightedDrops.Count);

                for (int i = 0; i < typesToDrop; i++)
                {
                    if (weightedDrops.Count == 0) break;

                    int index = PickWeightedIndex(weightedDrops);
                    ItemDrop selectedDrop = weightedDrops[index];
                    weightedDrops.RemoveAt(index);

                    int quantity = Random.Range(selectedDrop.minQuantity, selectedDrop.maxQuantity + 1);
                    for (int j = 0; j < quantity; j++)
                    {
                        Vector3 pos = GetSpreadPosition(transform.position);
                        SpawnPrefab(selectedDrop, pos);
                    }
                }
            }

            // Handle nested tables
            if (dropTable.nestedTables != null && dropTable.nestedTables.Count > 0)
            {
                if (Random.Range(0, 100) < dropTable.nestedTableChance)
                {
                    int nestedIndex = Random.Range(0, dropTable.nestedTables.Count);
                    DropTable nestedTable = dropTable.nestedTables[nestedIndex];
                    if (nestedTable != null)
                    {
                        var tempDropTable = dropTable;
                        dropTable = nestedTable;
                        DropItems();
                        dropTable = tempDropTable;
                    }
                }
            }
        }

        private Vector3 GetSpreadPosition(Vector3 center)
        {
            return center + Random.insideUnitSphere * dropSpreadRadius;
        }

        private void SpawnPrefab(ItemDrop drop, Vector3 position)
        {
            GameObject spawned = null;

            switch (drop.prefabType)
            {
                case PrefabType.NetworkPrefab:
                    spawned = NetworkPrefabs.SpawnNetworkPrefab(drop.itemPrefab.name, position, Quaternion.identity);
                    break;
                case PrefabType.Valuable:
                    spawned = Valuables.SpawnValuable(Valuables.GetValuableByName(drop.itemPrefab.name), position, Quaternion.identity);
                    if (spawned == null)
                        Debug.LogWarning($"Could not spawn Valuable: {drop.itemPrefab.name}");
                    break;
                case PrefabType.Item:
                    string itemName = drop.itemPrefab.name;
                    if (!itemName.StartsWith("Item ")) itemName = "Item " + itemName;
                    spawned = NetworkPrefabs.SpawnNetworkPrefab(itemName, position, Quaternion.identity);
                    if (spawned == null)
                        Debug.LogWarning($"Could not spawn Item: {itemName}");
                    break;
            }

            if (spawned != null && applyForce)
            {
                Rigidbody rb = spawned.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(Random.onUnitSphere * forceStrength, ForceMode.Impulse);
                }
            }

            if (drop.rarity != null && spawned != null)
            {
                var effectHandle = RarityEffectManager.TryPlayEffects(drop.rarity, position, spawned.transform);
                
                // Optional: Attach the effect handle to the item if you want to access it later
                var tracker = spawned.AddComponent<RarityEffectTracker>();
                tracker.effectHandle = effectHandle;
            }
        }

        private int PickWeightedIndex(List<ItemDrop> drops)
        {
            int totalWeight = 0;
            foreach (var drop in drops)
                totalWeight += drop.weight;

            int randomWeight = Random.Range(0, totalWeight);
            int current = 0;

            for (int i = 0; i < drops.Count; i++)
            {
                current += drops[i].weight;
                if (randomWeight < current)
                    return i;
            }

            return 0;
        }
    }
}