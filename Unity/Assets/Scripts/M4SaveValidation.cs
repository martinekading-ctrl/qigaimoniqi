using System;
using System.Collections.Generic;
using System.Linq;
using Reborn1985.M2;
using Reborn1985.M3;

namespace Reborn1985.M4
{
    public static class M4SaveValidation
    {
        private static readonly Dictionary<string,ItemKind> Sources = new Dictionary<string,ItemKind>
        { ["B-02"]=ItemKind.Paper,["D-01"]=ItemKind.Paper,["D-02"]=ItemKind.Bottle,["D-03"]=ItemKind.Bottle,["F-01"]=ItemKind.Paper,["F-02"]=ItemKind.Bottle,["F-03"]=ItemKind.Metal };
        public static string Signature(M2Settings weights, M3TradeSettings prices, M4Settings life) =>
            "M4:"+weights.CapacityGrams+":"+weights.Weight(ItemKind.Paper)+":"+weights.Weight(ItemKind.Bottle)+":"+weights.Weight(ItemKind.Metal)+":"+
            prices.CentsPerKg(ItemKind.Paper)+":"+prices.CentsPerKg(ItemKind.Bottle)+":"+prices.CentsPerKg(ItemKind.Metal)+":"+life.BreakfastCents;
        public static bool Run(M2RunSnapshot data, M2Settings weights, M3TradeSettings prices, M4Settings settings)
        {
            try
            {
                if (data==null || data.schemaVersion!=3 || data.inventory==null || data.groundDrops==null || data.soldUnits==null || data.receipts==null || data.claimedSourceIds==null || data.life==null) return false;
                if (data.inventory.Length>8 || data.groundDrops.Length>7 || data.soldUnits.Length>7 || data.receipts.Length>7 || data.claimedSourceIds.Length>7 || data.inventoryRevision<0) return false;
                M4LifeData life=data.life;
                if (life.dayIndex<1 || life.dayIndex>100000 || life.purchasedMeals<0 || life.purchasedMeals>10000 || life.mealExpenseCents!=(long)life.purchasedMeals*settings.BreakfastCents || life.helpCompleted!=life.helpMealClaimed || (life.helpCompleted && !life.helpOffered)) return false;
                if (data.cashCents<0 || data.scrapIncomeCents<0 || data.cashCents+life.mealExpenseCents!=data.scrapIncomeCents) return false;
                if (data.inventory.Count(unit=>unit!=null && unit.kind==ItemKind.Letter && unit.unitId==M2RunState.LetterId && unit.originPointId=="opening")!=1) return false;
                var all=new List<ItemUnit>(data.inventory);
                foreach (GroundDrop drop in data.groundDrops)
                { if (drop==null || drop.unit==null || drop.unit.kind==ItemKind.Letter || drop.createdDay<1 || drop.createdDay>life.dayIndex) return false; all.Add(drop.unit); }
                all.AddRange(data.soldUnits);
                if (all.Any(unit=>unit==null) || all.Select(unit=>unit.unitId).Distinct().Count()!=all.Count) return false;
                foreach (ItemUnit unit in all.Where(unit=>unit.unitId!=M2RunState.LetterId))
                    if (!Sources.TryGetValue(unit.unitId,out ItemKind kind) || kind!=unit.kind || unit.originPointId!=unit.unitId) return false;
                var resourceIds=new HashSet<string>(all.Where(unit=>unit.kind!=ItemKind.Letter).Select(unit=>unit.unitId));
                if (data.claimedSourceIds.Distinct().Count()!=data.claimedSourceIds.Length || !resourceIds.SetEquals(data.claimedSourceIds)) return false;
                if (data.inventory.Sum(unit=>(long)weights.Weight(unit.kind))>weights.CapacityGrams || data.soldUnits.Any(unit=>unit.kind==ItemKind.Letter)) return false;
                var soldById=data.soldUnits.ToDictionary(unit=>unit.unitId);
                var receiptIds=new HashSet<string>(); long totalIncome=0,lastAfter=0;
                for (int i=0;i<data.receipts.Length;i++)
                {
                    SaleReceipt receipt=data.receipts[i];
                    if (receipt==null || receipt.sequence!=i+1 || receipt.unitIds==null || receipt.unitIds.Length==0 || receipt.quantities?.Length!=3 || receipt.grams?.Length!=3 || receipt.centsPerKg?.Length!=3 || receipt.lineCents?.Length!=3 || receipt.cashBefore<0 || receipt.cashBefore>lastAfter) return false;
                    var units=new List<ItemUnit>();
                    foreach (string id in receipt.unitIds) { if (!receiptIds.Add(id) || !soldById.TryGetValue(id,out ItemUnit unit)) return false; units.Add(unit); }
                    for (int k=0;k<3;k++)
                    {
                        ItemKind kind=(ItemKind)k; int count=units.Count(unit=>unit.kind==kind),grams=count*weights.Weight(kind),rate=prices.CentsPerKg(kind);
                        long numerator=(long)grams*rate;
                        if (rate<=0 || numerator%1000!=0 || receipt.quantities[k]!=count || receipt.grams[k]!=grams || receipt.centsPerKg[k]!=rate || receipt.lineCents[k]!=numerator/1000) return false;
                    }
                    if (receipt.totalGrams!=receipt.grams.Sum() || receipt.incomeCents!=receipt.lineCents.Sum() || receipt.cashAfter!=receipt.cashBefore+receipt.incomeCents) return false;
                    totalIncome+=receipt.incomeCents; lastAfter=receipt.cashAfter;
                }
                return receiptIds.SetEquals(soldById.Keys) && totalIncome==data.scrapIncomeCents && data.cashCents<=lastAfter;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is OverflowException || ex is NullReferenceException) { return false; }
        }
    }
}
