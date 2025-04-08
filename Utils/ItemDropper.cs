using UnityEngine;
using System.Collections.Generic;
using REPOLib.Modules;
namespace PaintedUtils
{
    public class ItemDropper : MonoBehaviour
    {
        public enum PrefabType { NetworkPrefab, Valuable, Item }

        [System.Serializable]
        public class ItemDrop
        {
            public GameObject itemPrefab;
            public int minQuantity;
            public int maxQuantity;
            public int weight;
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
                // Try to load a default drop table
                dropTable = Resources.Load<DropTable>("DropTables/DefaultDropTable");
                
                if (dropTable == null)
                {
                    Debug.LogError("Failed to load fallback drop table. No items will be dropped.");
                    return;
                }
                else
                {
                    Debug.Log($"Using fallback drop table: {dropTable.name}");
                }
            }

            // Debug.Log($"Static DropItemsAtPosition executing with drop table: {dropTable.name}");
            
            // Guaranteed Drops
            if (dropTable.guaranteedDrops != null)
            {
                foreach (var drop in dropTable.guaranteedDrops)
                {
                    int quantity = Random.Range(drop.minQuantity, drop.maxQuantity + 1);
                    for (int j = 0; j < quantity; j++)
                    {
                        Vector3 pos = position + Random.insideUnitSphere * dropRadius;
                        SpawnPrefabStatic(drop, pos, force);
                    }
                }
            }

            // Check for nested tables
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

            // Weighted Drops
            if (dropTable.drops != null && dropTable.drops.Count > 0)
            {
                List<ItemDrop> validDrops = new List<ItemDrop>(dropTable.drops);
                int typesToDrop = Mathf.Clamp(Random.Range(1, 3 + 1), 1, validDrops.Count);

                for (int i = 0; i < typesToDrop; i++)
                {
                    if (validDrops.Count == 0) break;

                    int totalWeight = 0;
                    foreach (var drop in validDrops)
                        totalWeight += drop.weight;

                    int randomWeight = Random.Range(0, totalWeight);
                    int current = 0;
                    int index = 0;

                    for (int k = 0; k < validDrops.Count; k++)
                    {
                        current += validDrops[k].weight;
                        if (randomWeight < current)
                        {
                            index = k;
                            break;
                        }
                    }

                    ItemDrop selectedDrop = validDrops[index];
                    validDrops.RemoveAt(index);

                    int quantity = Random.Range(selectedDrop.minQuantity, selectedDrop.maxQuantity + 1);
                    for (int j = 0; j < quantity; j++)
                    {
                        Vector3 pos = position + Random.insideUnitSphere * dropRadius;
                        SpawnPrefabStatic(selectedDrop, pos, force);
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
            // Debug.Log($"DropItems called on {gameObject.name}. Drop table is {(dropTable == null ? "NULL" : "assigned")}");
            
            // Use the static method if this object might be destroyed
            if (gameObject.activeInHierarchy)
            {
                DropItemsMethod();
            }
            else
            {
                // If the object is already inactive, use the static method
                Vector3 position = dropTarget ? dropTarget.position : transform.position;
                DropItemsAtPosition(dropTable, position, dropSpreadRadius, forceStrength);
            }
        }
        public System.Collections.IEnumerator DropItemsCoroutine()
        {
            yield return null; // Wait one frame
            DropItemsMethod();
        }

        private void Awake()
        {
            // Debug.Log($"Awake on {gameObject.name}. Drop table is {(dropTable == null ? "NULL" : dropTable.name)}");
            
            // Try to auto-initialize the drop table if none is set
            if (dropTable == null)
            {
                InitializeDropTableFromName();
            }
        }

        // Method to handle drops when the object is despawned
        public void OnDespawn()
        {
            // Debug.Log($"OnDespawn called for {gameObject.name}");
            DropItems();
        }

        public void DropItemsMethod()
        {
            // Check if dropTable is null to prevent NullReferenceException
            if (dropTable == null)
            {
                Debug.LogError($"No drop table assigned to ItemDropper on {gameObject.name}. Please assign a drop table in the inspector.");
                return;
            }

            // Debug.Log($"DropItemsMethod executing with drop table: {dropTable.name}");
            // Debug.Log($"Guaranteed drops: {(dropTable.guaranteedDrops == null ? "NULL" : dropTable.guaranteedDrops.Count.ToString())}");
            // Debug.Log($"Weighted drops: {(dropTable.drops == null ? "NULL" : dropTable.drops.Count.ToString())}");

            Vector3 basePosition = dropTarget ? dropTarget.position : transform.position;

            // Guaranteed Drops
            foreach (var drop in dropTable.guaranteedDrops)
            {
                int quantity = Random.Range(drop.minQuantity, drop.maxQuantity + 1);
                for (int j = 0; j < quantity; j++)
                {
                    Vector3 pos = GetSpreadPosition(basePosition);
                    SpawnPrefab(drop, pos);
                }
            }

            // Weighted Drops
            List<ItemDrop> validDrops = new List<ItemDrop>(dropTable.drops);
            int typesToDrop = Mathf.Clamp(Random.Range(minItemTypes, maxItemTypes + 1), 1, validDrops.Count);

            for (int i = 0; i < typesToDrop; i++)
            {
                if (validDrops.Count == 0) break;

                int index = PickWeightedIndex(validDrops);
                ItemDrop selectedDrop = validDrops[index];
                validDrops.RemoveAt(index);

                int quantity = Random.Range(selectedDrop.minQuantity, selectedDrop.maxQuantity + 1);
                for (int j = 0; j < quantity; j++)
                {
                    Vector3 pos = GetSpreadPosition(basePosition);
                    SpawnPrefab(selectedDrop, pos);
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