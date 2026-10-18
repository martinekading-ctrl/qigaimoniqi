using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Reborn1985.M3;
using Reborn1985.M4;

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
        public int createdDay = 1;
        public GroundDrop Copy() => new GroundDrop { unit = unit.Copy(), position = position, createdDay = createdDay };
    }

    // Detached transfer data; M4 persists it and M5 will own refresh/cleanup.
    [Serializable]
    public sealed class M2RunSnapshot
    {
        public int schemaVersion = 3;
        public ItemUnit[] inventory;
        public string[] claimedSourceIds;
        public GroundDrop[] groundDrops;
        public long cashCents, scrapIncomeCents;
        public int inventoryRevision;
        public ItemUnit[] soldUnits;
        public SaleReceipt[] receipts;
        public M4LifeData life;
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
        private readonly string runToken = Guid.NewGuid().ToString("N");
        private M4LifeData life = new M4LifeData();
        public M4LifeData Life => life.Copy();
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
            dropped = new GroundDrop { unit = unit, position = position, createdDay = life.dayIndex };
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
            return new SaleQuote(revision,CashCents,lines.ToArray(),ids,status,runToken);
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
        public bool TryBuyBreakfast(int cents)
        {
            if (cents <= 0 || CashCents < cents || life.purchasedMeals >= 10000) return false;
            long expense = checked(life.mealExpenseCents + cents);
            CashCents -= cents; life.mealExpenseCents = expense; life.purchasedMeals++; revision++;
            return true;
        }
        public bool OfferHelp(int breakfastCents)
        {
            if (life.helpMealClaimed || breakfastCents<=0 || CashCents>=breakfastCents) return false;
            life.helpOffered = true; return true;
        }
        public bool CompleteHelp()
        {
            if (!life.helpOffered || life.helpMealClaimed) return false;
            life.helpCompleted = life.helpMealClaimed = true; return true;
        }
        public bool Rest()
        {
            if (life.dayIndex >= 100000) return false;
            life.dayIndex++; return true; // M5 owns resource refresh and old-drop cleanup.
        }
        public void MarkSeen(LifeView view)
        {
            switch (view)
            {
                case LifeView.Letter: life.letterRead=true; break;
                case LifeView.Year: life.yearRead=true; break;
                case LifeView.Notice: life.noticeRead=true; break;
                case LifeView.Puddle: life.puddleObserved=true; break;
                case LifeView.Kettle: life.kettleObserved=true; break;
                case LifeView.Resident: life.residentTalked=true; break;
            }
        }
        public void MarkOpeningSeen() => life.openingSeen = true;
        public static M2RunState RestoreValidated(M2RunSnapshot data, M2Settings weights, M3TradeSettings prices, M4Settings content)
        {
            if (!M4SaveValidation.Run(data,weights,prices,content)) throw new ArgumentException("Invalid run snapshot",nameof(data));
            var state = new M2RunState(weights);
            state.inventory.Clear(); state.inventory.AddRange(data.inventory.Select(unit=>unit.Copy()));
            foreach (string id in data.claimedSourceIds) state.claimed.Add(id);
            foreach (GroundDrop drop in data.groundDrops) state.ground.Add(drop.unit.unitId,drop.Copy());
            state.sold.AddRange(data.soldUnits.Select(unit=>unit.Copy()));
            state.receipts.AddRange(data.receipts.Select(receipt=>receipt.Copy()));
            state.revision=data.inventoryRevision; state.CashCents=data.cashCents; state.ScrapIncomeCents=data.scrapIncomeCents;
            state.life=data.life.Copy();
            return state;
        }
        public M2RunSnapshot Snapshot() => new M2RunSnapshot
        {
            inventory = inventory.Select(unit => unit.Copy()).ToArray(),
            claimedSourceIds = claimed.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            groundDrops = ground.Values.OrderBy(drop => drop.unit.unitId, StringComparer.Ordinal).Select(drop => drop.Copy()).ToArray(),
            cashCents = CashCents, scrapIncomeCents = ScrapIncomeCents, inventoryRevision = revision,
            soldUnits = sold.Select(unit => unit.Copy()).ToArray(), receipts = receipts.Select(receipt => receipt.Copy()).ToArray(), life=life.Copy()
        };
    }
}
