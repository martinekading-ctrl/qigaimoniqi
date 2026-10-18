using System;
using System.Linq;
using Reborn1985.M2;
using Reborn1985.M3;
using Reborn1985.M4;
using UnityEngine;

namespace Reborn1985.M5
{
    public static class M5SaveUpgrade
    {
        // Only M4 schema3 -> M5 schema4. Never mutate a live model or guess unknown versions.
        public static M4SaveData Normalize(M4SaveData input,M2Settings weights,M3TradeSettings prices,M4Settings content)
        {
            if(input==null || input.run==null) return null;
            if(input.run.schemaVersion==4) return input;
            if(input.version!=1 || input.run.schemaVersion!=3 || weights.SourceRefreshDays!=1 ||
               input.parameters!=M4LegacyValidation.Signature(weights,prices,content) ||
               !M4LegacyValidation.Run(input.run,weights,prices,content)) return null;
            var data=JsonUtility.FromJson<M4SaveData>(JsonUtility.ToJson(input));
            var run=data.run;
            foreach(ItemUnit unit in run.inventory.Concat(run.soldUnits).Concat(run.groundDrops.Select(drop=>drop.unit))) unit.spawnedDay=1;
            run.clearedUnits=run.groundDrops.Where(drop=>drop.createdDay<run.life.dayIndex).Select(drop=>drop.unit.Copy()).ToArray();
            run.groundDrops=run.groundDrops.Where(drop=>drop.createdDay==run.life.dayIndex).ToArray();
            run.schemaVersion=4; data.parameters=M4SaveValidation.Signature(weights,prices,content);
            return data;
        }
    }
}
