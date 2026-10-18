using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Reborn1985.M2
{
    public enum ItemKind { Paper, Bottle, Metal, Letter }

    [Serializable]
    public sealed class ItemUnit
    {
        public string unitId;
        public string originPointId;
        public ItemKind kind;
        public ItemUnit Copy() => new ItemUnit { unitId = unitId, originPointId = originPointId, kind = kind };
    }

    [Serializable]
    public sealed class GroundDrop
    {
        public ItemUnit unit;
        public Vector2 position;
        public GroundDrop Copy() => new GroundDrop { unit = unit.Copy(), position = position };
    }

    // Detached transfer data for later M4/M5 integration; no disk save or day clock in M2.
    [Serializable]
    public sealed class M2RunSnapshot
    {
        public int schemaVersion = 1;
        public ItemUnit[] inventory;
        public string[] claimedSourceIds;
        public GroundDrop[] groundDrops;
    }

    // All ownership mutations complete synchronously before the UI is notified.
    // A physical unit retains its ID through every drop and re-pick.
    public sealed class M2RunState
    {
        private readonly List<ItemUnit> inventory = new List<ItemUnit>();
        private readonly Dictionary<string, GroundDrop> ground = new Dictionary<string, GroundDrop>();
        private readonly HashSet<string> claimed = new HashSet<string>();
        private readonly M2Settings settings;
        public const string LetterId = "story-mother-letter";
        public M2RunState(M2Settings parameters)
        {
            settings = parameters;
            inventory.Add(new ItemUnit { unitId = LetterId, originPointId = "opening", kind = ItemKind.Letter });
        }
        public int Count(ItemKind kind) => inventory.Count(unit => unit.kind == kind);
        public int TotalGrams => inventory.Sum(unit => settings.Weight(unit.kind));
        public bool Claimed(string id) => claimed.Contains(id);
        public bool HasDrop(string id) => ground.ContainsKey(id);
        public int GroundCount => ground.Count;
        public bool CanCarry(ItemKind kind) => kind != ItemKind.Letter && TotalGrams <= settings.CapacityGrams - settings.Weight(kind);

        public bool TryCollectSource(string source, ItemKind kind)
        {
            if (string.IsNullOrEmpty(source) || source == LetterId || claimed.Contains(source) || !CanCarry(kind)) return false;
            if (inventory.Any(unit => unit.unitId == source) || ground.ContainsKey(source)) return false;
            claimed.Add(source);
            inventory.Add(new ItemUnit { unitId = source, originPointId = source, kind = kind });
            return true;
        }
        public bool TryCollectDrop(string id)
        {
            if (!ground.TryGetValue(id, out GroundDrop drop) || !CanCarry(drop.unit.kind)) return false;
            ground.Remove(id);
            inventory.Add(drop.unit);
            return true;
        }
        public bool TryDropOne(ItemKind kind, Vector2 position, out GroundDrop dropped)
        {
            dropped = null;
            if (kind == ItemKind.Letter) return false;
            ItemUnit unit = inventory.FirstOrDefault(item => item.kind == kind);
            if (unit == null || ground.ContainsKey(unit.unitId)) return false;
            inventory.Remove(unit);
            dropped = new GroundDrop { unit = unit, position = position };
            ground.Add(unit.unitId, dropped);
            // Never expose the mutable object owned by this state.
            dropped = dropped.Copy();
            return true;
        }
        public M2RunSnapshot Snapshot() => new M2RunSnapshot
        {
            inventory = inventory.Select(unit => unit.Copy()).ToArray(),
            claimedSourceIds = claimed.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            groundDrops = ground.Values.OrderBy(drop => drop.unit.unitId, StringComparer.Ordinal).Select(drop => drop.Copy()).ToArray()
        };
    }
}
