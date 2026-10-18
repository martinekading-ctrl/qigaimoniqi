using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Reborn1985.M3;

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
        public int schemaVersion = 2;
        public ItemUnit[] inventory;
        public string[] claimedSourceIds;
        public GroundDrop[] groundDrops;
        public long cashCents, scrapIncomeCents;
        public int inventoryRevision;
        public ItemUnit[] soldUnits;
        public SaleReceipt[] receipts;
    }

    // All ownership mutations complete synchronously before the UI is notified.
    // A physical unit retains its ID through every drop and re-pick.
    public sealed class M2RunState
    {
        private readonly List<ItemUnit> inventory = new List<ItemUnit>();
        private readonly Dictionary<string, GroundDrop> ground = new Dictionary<string, GroundDrop>();
        private readonly HashSet<string> claimed = new HashSet<string>();
        private readonly List<ItemUnit> sold = new List<ItemUnit>();
        private readonly List<SaleReceipt> receipts = new List<SaleReceipt>();
        private int revision;
        public long CashCents { get; private set; }
        public long ScrapIncomeCents { get; private set; }
        public int SoldCount => sold.Count;
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
            revision++;
            return true;
        }
        public bool TryCollectDrop(string id)
        {
            if (!ground.TryGetValue(id, out GroundDrop drop) || !CanCarry(drop.unit.kind)) return false;
            ground.Remove(id);
            inventory.Add(drop.unit);
            revision++;
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
            revision++;
            // Never expose the mutable object owned by this state.
            dropped = dropped.Copy();
            return true;
        }
        public SaleQuote QuoteAllScrap(M3TradeSettings prices)
        {
            var lines = new List<SaleLine>();
            SaleResult status = SaleResult.Success;
            foreach (ItemKind kind in new[] { ItemKind.Paper, ItemKind.Bottle, ItemKind.Metal })
            {
                int count = Count(kind), grams = checked(count * settings.Weight(kind));
                int rate = prices.CentsPerKg(kind);
                long numerator = (long)grams * rate;
                if (rate <= 0) status = SaleResult.InvalidPrice;
                // The approved fixture is exact. No unapproved rounding rule is invented.
                else if (numerator % 1000 != 0 && status == SaleResult.Success) status = SaleResult.FractionalCent;
                lines.Add(new SaleLine(kind,count,grams,rate,numerator / 1000));
            }
            string[] ids = inventory.Where(unit => unit.kind != ItemKind.Letter).Select(unit => unit.unitId).OrderBy(id => id,StringComparer.Ordinal).ToArray();
            if (ids.Length == 0) status = SaleResult.Empty;
            return new SaleQuote(revision,CashCents,lines.ToArray(),ids,status);
        }
        public SaleResult TrySell(SaleQuote quote, M3TradeSettings prices, out SaleReceipt receipt)
        {
            receipt = null;
            if (quote == null) return SaleResult.NoPending;
            SaleQuote current = QuoteAllScrap(prices);
            if (!quote.Matches(current)) return SaleResult.StaleQuote;
            if (current.Status != SaleResult.Success) return current.Status;
            long nextCash = checked(CashCents + current.TotalCents);
            long nextIncome = checked(ScrapIncomeCents + current.TotalCents);
            ItemUnit[] units = inventory.Where(unit => current.UnitIds.Contains(unit.unitId)).ToArray();
            receipt = new SaleReceipt
            {
                sequence = receipts.Count + 1, cashBefore = CashCents, cashAfter = nextCash, incomeCents = current.TotalCents, totalGrams = current.TotalGrams,
                unitIds = current.UnitIds.ToArray(), quantities = current.Lines.Select(line => line.Quantity).ToArray(),
                grams = current.Lines.Select(line => line.Grams).ToArray(), centsPerKg = current.Lines.Select(line => line.CentsPerKg).ToArray(),
                lineCents = current.Lines.Select(line => line.Cents).ToArray()
            };
            // Every validation and calculation above precedes the single ownership/cash commit.
            inventory.RemoveAll(unit => current.UnitIds.Contains(unit.unitId));
            sold.AddRange(units);
            CashCents = nextCash; ScrapIncomeCents = nextIncome; revision++;
            receipts.Add(receipt.Copy());
            return SaleResult.Success;
        }
        public M2RunSnapshot Snapshot() => new M2RunSnapshot
        {
            inventory = inventory.Select(unit => unit.Copy()).ToArray(),
            claimedSourceIds = claimed.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            groundDrops = ground.Values.OrderBy(drop => drop.unit.unitId, StringComparer.Ordinal).Select(drop => drop.Copy()).ToArray(),
            cashCents = CashCents, scrapIncomeCents = ScrapIncomeCents, inventoryRevision = revision,
            soldUnits = sold.Select(unit => unit.Copy()).ToArray(), receipts = receipts.Select(receipt => receipt.Copy()).ToArray()
        };
    }
}
