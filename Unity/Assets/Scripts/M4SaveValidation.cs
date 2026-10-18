using System;
using System.Collections.Generic;
using System.Linq;
using Reborn1985.M2;
using Reborn1985.M3;
using Reborn1985.M5;

namespace Reborn1985.M4
{
    public static class M4SaveValidation
    {
        public static string Signature(M2Settings weights, M3TradeSettings prices, M4Settings life) =>
            "M5:"+weights.CapacityGrams+":"+weights.Weight(ItemKind.Paper)+":"+weights.Weight(ItemKind.Bottle)+":"+weights.Weight(ItemKind.Metal)+":"+
            prices.CentsPerKg(ItemKind.Paper)+":"+prices.CentsPerKg(ItemKind.Bottle)+":"+prices.CentsPerKg(ItemKind.Metal)+":"+life.BreakfastCents+":"+weights.SourceRefreshDays;
        public static bool Run(M2RunSnapshot data, M2Settings weights, M3TradeSettings prices, M4Settings settings)
        {
            try
            {
                if (data==null || data.schemaVersion!=4 || data.inventory==null || data.groundDrops==null || data.soldUnits==null || data.clearedUnits==null || data.receipts==null || data.claimedSourceIds==null || data.life==null) return false;
                M4LifeData life=data.life;
                if (life.dayIndex<1 || life.dayIndex>100000 || life.purchasedMeals<0 || life.purchasedMeals>10000 || life.mealExpenseCents!=(long)life.purchasedMeals*settings.BreakfastCents || life.helpCompleted!=life.helpMealClaimed || (life.helpCompleted && !life.helpOffered)) return false;
                int minWeight=new[]{weights.Weight(ItemKind.Paper),weights.Weight(ItemKind.Bottle),weights.Weight(ItemKind.Metal)}.Min();
                if(minWeight<=0 || weights.CapacityGrams<1) return false;
                int maxUnits=(1+(life.dayIndex-1)/weights.SourceRefreshDays)*M5DailyRules.SourceIds.Count;
                if(data.inventory.Length>1+weights.CapacityGrams/minWeight || data.groundDrops.Length>maxUnits || data.soldUnits.Length>maxUnits || data.clearedUnits.Length>maxUnits || data.receipts.Length>maxUnits || data.claimedSourceIds.Length>maxUnits || data.inventoryRevision<0) return false;
                if (data.cashCents<0 || data.scrapIncomeCents<0 || checked(data.cashCents+life.mealExpenseCents)!=data.scrapIncomeCents) return false;
                if (data.inventory.Count(unit=>unit!=null && unit.kind==ItemKind.Letter && unit.unitId==M2RunState.LetterId && unit.originPointId=="opening" && unit.spawnedDay==1)!=1) return false;
                var all=new List<ItemUnit>(data.inventory);
                foreach (GroundDrop drop in data.groundDrops)
                { if (drop==null || drop.unit==null || drop.unit.kind==ItemKind.Letter || drop.createdDay!=life.dayIndex || drop.unit.spawnedDay>drop.createdDay) return false; all.Add(drop.unit); }
                all.AddRange(data.soldUnits);
                all.AddRange(data.clearedUnits);
                if (all.Count>maxUnits+1 || all.Any(unit=>unit==null) || all.Select(unit=>unit.unitId).Distinct().Count()!=all.Count) return false;
                foreach (ItemUnit unit in all.Where(unit=>unit.unitId!=M2RunState.LetterId))
                    if (!M5DailyRules.TryKind(unit.originPointId,out ItemKind kind) || kind!=unit.kind || unit.spawnedDay<1 || unit.spawnedDay>life.dayIndex || M5DailyRules.CycleStart(unit.spawnedDay,weights.SourceRefreshDays)!=unit.spawnedDay || unit.unitId!=M5DailyRules.UnitId(unit.originPointId,unit.spawnedDay)) return false;
                var resourceIds=new HashSet<string>(all.Where(unit=>unit.kind!=ItemKind.Letter).Select(unit=>unit.unitId));
                if (data.claimedSourceIds.Distinct().Count()!=data.claimedSourceIds.Length || !resourceIds.SetEquals(data.claimedSourceIds)) return false;
                if (data.inventory.Sum(unit=>(long)weights.Weight(unit.kind))>weights.CapacityGrams || data.soldUnits.Any(unit=>unit.kind==ItemKind.Letter) || data.clearedUnits.Any(unit=>unit.kind==ItemKind.Letter || unit.spawnedDay>=life.dayIndex)) return false;
                var soldById=data.soldUnits.ToDictionary(unit=>unit.unitId);
                var receiptIds=new HashSet<string>(); long totalIncome=0,lastAfter=0;
                for (int i=0;i<data.receipts.Length;i++)
                {
                    SaleReceipt receipt=data.receipts[i];
                    if (receipt==null || receipt.sequence!=i+1 || receipt.unitIds==null || receipt.unitIds.Length==0 || receipt.unitIds.Length>weights.CapacityGrams/minWeight || receipt.quantities?.Length!=3 || receipt.grams?.Length!=3 || receipt.centsPerKg?.Length!=3 || receipt.lineCents?.Length!=3 || receipt.cashBefore<0 || receipt.cashBefore>lastAfter) return false;
                    var units=new List<ItemUnit>();
                    foreach (string id in receipt.unitIds) { if (!receiptIds.Add(id) || !soldById.TryGetValue(id,out ItemUnit unit)) return false; units.Add(unit); }
                    for (int k=0;k<3;k++)
                    {
                        ItemKind kind=(ItemKind)k; int count=units.Count(unit=>unit.kind==kind),grams=count*weights.Weight(kind),rate=prices.CentsPerKg(kind);
                        long numerator=(long)grams*rate;
                        if (rate<=0 || numerator%1000!=0 || receipt.quantities[k]!=count || receipt.grams[k]!=grams || receipt.centsPerKg[k]!=rate || receipt.lineCents[k]!=numerator/1000) return false;
                    }
                    if (receipt.totalGrams>weights.CapacityGrams || receipt.totalGrams!=receipt.grams.Sum() || receipt.incomeCents!=receipt.lineCents.Sum() || receipt.cashAfter!=checked(receipt.cashBefore+receipt.incomeCents)) return false;
                    totalIncome=checked(totalIncome+receipt.incomeCents); lastAfter=receipt.cashAfter;
                }
                return receiptIds.SetEquals(soldById.Keys) && totalIncome==data.scrapIncomeCents && data.cashCents<=lastAfter;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is OverflowException || ex is NullReferenceException) { return false; }
        }
    }
}
