using System;
using System.Collections.Generic;
using System.Linq;
using Reborn1985.M2;

namespace Reborn1985.M3
{
    public enum SaleResult { Success, Empty, StaleQuote, InvalidPrice, FractionalCent, NoPending, OutOfReach }

    public sealed class SaleLine
    {
        public ItemKind Kind { get; }
        public int Quantity { get; }
        public int Grams { get; }
        public int CentsPerKg { get; }
        public long Cents { get; }
        internal SaleLine(ItemKind kind, int quantity, int grams, int rate, long cents)
        { Kind = kind; Quantity = quantity; Grams = grams; CentsPerKg = rate; Cents = cents; }
    }

    // Immutable quote. Confirming still compares it with fresh authoritative state.
    public sealed class SaleQuote
    {
        public int Revision { get; }
        public long CashBefore { get; }
        public IReadOnlyList<SaleLine> Lines { get; }
        public IReadOnlyList<string> UnitIds { get; }
        public SaleResult Status { get; }
        public int TotalGrams => Lines.Sum(line => line.Grams);
        public long TotalCents => Lines.Sum(line => line.Cents);
        internal SaleQuote(int revision, long cash, SaleLine[] lines, string[] ids, SaleResult status)
        { Revision = revision; CashBefore = cash; Lines = Array.AsReadOnly(lines); UnitIds = Array.AsReadOnly(ids); Status = status; }
        internal bool Matches(SaleQuote other) => Revision == other.Revision && CashBefore == other.CashBefore && Status == other.Status &&
            UnitIds.SequenceEqual(other.UnitIds) && Lines.Count == other.Lines.Count && Lines.Zip(other.Lines, (a,b) =>
                a.Kind == b.Kind && a.Quantity == b.Quantity && a.Grams == b.Grams && a.CentsPerKg == b.CentsPerKg && a.Cents == b.Cents).All(equal => equal);
    }

    [Serializable]
    public sealed class SaleReceipt
    {
        public int sequence;
        public long cashBefore, cashAfter, incomeCents;
        public int totalGrams;
        public string[] unitIds;
        public int[] quantities, grams, centsPerKg;
        public long[] lineCents;
        public SaleReceipt Copy() => new SaleReceipt
        {
            sequence = sequence, cashBefore = cashBefore, cashAfter = cashAfter, incomeCents = incomeCents, totalGrams = totalGrams,
            unitIds = (string[])unitIds.Clone(), quantities = (int[])quantities.Clone(), grams = (int[])grams.Clone(),
            centsPerKg = (int[])centsPerKg.Clone(), lineCents = (long[])lineCents.Clone()
        };
    }
}
