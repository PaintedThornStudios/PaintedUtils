using System.Collections.Generic;
using UnityEngine;

namespace PaintedUtils
{
    [CreateAssetMenu(menuName = "PaintedUtils/Drop Table")]
    public class DropTable : ScriptableObject
    {
        public List<ItemDropper.ItemDrop> guaranteedDrops;
        public List<ItemDropper.ItemDrop> drops;
    }
} 