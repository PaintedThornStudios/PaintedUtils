using System.Collections.Generic;
using UnityEngine;

namespace PaintedUtils
{
    [CreateAssetMenu(menuName = "PaintedUtils/Drop Table")]
    public class DropTable : ScriptableObject
    {
        public List<ItemDropper.ItemDrop> drops = new List<ItemDropper.ItemDrop>();
        public List<DropTable> nestedTables;
        [Range(0, 100)]
        public int nestedTableChance = 50; // Percentage chance to roll a nested table
    }
} 