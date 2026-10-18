using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Reborn1985.M1;
using Reborn1985.M2;
using Reborn1985.M3;
using Reborn1985.M4;
using Reborn1985.M5;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Reborn1985.Editor
{
    public static class M5ProjectTools
    {
        [MenuItem("Reborn 1985/M5/Verify daily cycle save migration and P0 regression")]
        public static void Verify()
        {
            M4ProjectTools.Verify();
            EditorSceneManager.OpenScene(M1ProjectTools.ScenePath);
            var session=UnityEngine.Object.FindFirstObjectByType<M4Session>(); session.Initialize(); Physics2D.SyncTransforms();
            M2Settings weights=session.Game.Settings;
            M3TradeSettings prices=UnityEngine.Object.FindFirstObjectByType<M3Trade>().Settings;
            M4Settings content=session.Settings;
            var checks=new List<string>();
            void Require(bool ok,string name) {if(!ok)throw new InvalidOperationException("M5_FAIL: "+name);checks.Add("PASS "+name);}
            M4SaveData Data(M2RunState value)=>new M4SaveData {parameters=M4SaveValidation.Signature(weights,prices,content),position=session.Game.World.RegionEntry("A"),facing=Vector2.down,run=value.Snapshot()};
            Vector2 safe=session.Game.World.RegionEntry("D");
            Require(weights.SourceRefreshDays==1,"Provisional fixture refreshes original seven sources once per day");
            Require(session.Game.World.Points.Length==20,"Twenty original points retained without new IDs");
            Require(new SerializedObject(UnityEngine.Object.FindFirstObjectByType<M2Hud>()).FindProperty("stageTitle").stringValue.Contains("M5"),"Existing scene shows M5 title");
            var failure=new M2RunState(weights);
            Require(!failure.TryCollectSource("D-04",ItemKind.Paper)&&!failure.TryCollectSource("F-01",ItemKind.Metal),"Decoration and wrong item kind cannot become refresh resources");
            Require(failure.OfferHelp(26)&&failure.CompleteHelp(),"Failure fixture has used its only helper meal with zero cash");
            foreach(string source in M5DailyRules.SourceIds)
            {
                M5DailyRules.TryKind(source,out ItemKind kind);
                Require(failure.TryCollectSource(source,kind)&&failure.TryDropOne(kind,safe,out _),"Exhaust and drop first-day source "+source);
                for(int retry=0;retry<25;retry++)if(failure.TryCollectSource(source,kind))throw new InvalidOperationException("Same-day source duplication");
            }
            Require(failure.GroundCount==7&&failure.TotalGrams==0&&failure.CashCents==0&&session.Validate(Data(failure)),"All seven finite resources exhausted, zero cash, valid saved state");
            Require(failure.Rest()&&failure.Life.dayIndex==2&&failure.GroundCount==0&&failure.ClearedCount==7,"R-02 overnight removes all ground ownership and records seven cleared identities");
            Require(M5DailyRules.SourceIds.All(id=>!failure.Claimed(id)),"R-01 all seven next-day source slots become available");
            Require(failure.Life.helpMealClaimed&&!failure.CompleteHelp()&&!failure.OfferHelp(26),"L-02 rest cannot reset one-time helper reward");
            foreach(string source in new[]{"B-02","D-01","D-02","D-03"})
            {M5DailyRules.TryKind(source,out ItemKind kind);Require(failure.TryCollectSource(source,kind),"Recovery collect new physical unit "+source);}
            Require(failure.TotalGrams==3000&&failure.TrySell(failure.QuoteAllScrap(prices),prices,out _)==SaleResult.Success&&failure.CashCents==44,"R-01 zero-money failure recovers by next-day 3kg sale for44 cents");
            Require(failure.TryBuyBreakfast(26)&&failure.CashCents==18&&session.Validate(Data(failure)),"R-01 recovered route buys breakfast and conserves18 cents");
            Require(failure.Snapshot().soldUnits.All(unit=>unit.spawnedDay==2&&unit.unitId.EndsWith("@2",StringComparison.Ordinal)),"New-day sold units never reuse cleared first-day IDs");
            var held=new M2RunState(weights);held.TryCollectSource("B-02",ItemKind.Paper);held.TryCollectSource("D-02",ItemKind.Bottle);
            var previousQuote=held.QuoteAllScrap(prices);
            Require(held.Rest()&&held.TotalGrams==1500&&held.Count(ItemKind.Letter)==1,"R-02 backpack contents and protected letter survive rest");
            Require(held.TrySell(previousQuote,prices,out _)==SaleResult.StaleQuote,"Previous-day pending quote is invalid after rest");
            Require(held.TryCollectSource("B-02",ItemKind.Paper)&&held.Count(ItemKind.Paper)==2,"Old carried paper and fresh source paper coexist with distinct identities");
            Require(held.TryDropOne(ItemKind.Paper,safe,out GroundDrop oldPaper)&&oldPaper.unit.spawnedDay==1&&oldPaper.createdDay==2&&held.TryCollectDrop(oldPaper.unit.unitId),"Old inventory dropped today can still be re-picked today");
            Require(held.TrySell(held.QuoteAllScrap(prices),prices,out _)==SaleResult.Success&&held.CashCents==34&&session.Validate(Data(held)),"Mixed-day sale prices actual 2papers plus1bottle at34 cents");
            var capacity=new M2RunState(weights);
            for(int day=1;day<=10;day++){if(!capacity.TryCollectSource("D-02",ItemKind.Bottle))throw new InvalidOperationException("Carry fixture failed");if(day<10)capacity.Rest();}
            Require(capacity.Count(ItemKind.Bottle)==10&&capacity.TotalGrams==5000&&session.Validate(Data(capacity)),"Ten carried bottles plus letter remain a valid5kg multi-day save");
            Require(!capacity.TryCollectSource("D-03",ItemKind.Bottle)&&!capacity.Claimed("D-03"),"Next-day refreshed source still respects full backpack");
            Require(capacity.TryDropOne(ItemKind.Bottle,safe,out _)&&capacity.Rest()&&capacity.TotalGrams==4500&&capacity.ClearedCount==1,"Only ground bottle clears; nine carried bottles remain");
            var alternate=UnityEngine.Object.Instantiate(weights);
            try
            {
                var serialized=new SerializedObject(alternate);serialized.FindProperty("sourceRefreshDays").intValue=2;serialized.ApplyModifiedPropertiesWithoutUndo();
                var slow=new M2RunState(alternate);slow.TryCollectSource("F-01",ItemKind.Paper);slow.TryDropOne(ItemKind.Paper,safe,out _);slow.Rest();
                Require(slow.Claimed("F-01")&&slow.GroundCount==0,"Configurable2-day cycle still clears ground daily without early source refresh");
                Require(slow.Rest()&&!slow.Claimed("F-01")&&slow.TryCollectSource("F-01",ItemKind.Paper)&&slow.Snapshot().inventory.Any(unit=>unit.unitId=="F-01@3"),"Configured next cycle yields one new unit at day3");
                Require(M4SaveValidation.Run(slow.Snapshot(),alternate,prices,content),"Alternate cycle validates without changing production1-day fixture");
            }
            finally{UnityEngine.Object.DestroyImmediate(alternate);}
            var stress=new M2RunState(weights);stress.OfferHelp(26);stress.CompleteHelp();
            for(int day=1;day<=60;day++)
            {
                foreach(string source in M5DailyRules.SourceIds){M5DailyRules.TryKind(source,out ItemKind kind);if(!stress.TryCollectSource(source,kind)||stress.TrySell(stress.QuoteAllScrap(prices),prices,out _)!=SaleResult.Success)throw new InvalidOperationException("Stress sale failed");}
                if(!stress.TryBuyBreakfast(26)||!M4SaveValidation.Run(stress.Snapshot(),weights,prices,content))throw new InvalidOperationException("Stress conservation failed day"+day);
                if(day<60)stress.Rest();
            }
            Require(stress.CashCents==5280&&stress.ScrapIncomeCents==6840&&stress.SoldCount==420&&stress.Life.purchasedMeals==60,"60-day stress:420 unique sold units, income6840, meals1560, cash5280 cents");
            Require(stress.Snapshot().soldUnits.Select(unit=>unit.unitId).Distinct().Count()==420&&stress.Life.helpMealClaimed,"Stress retains420 identities and one lifetime helper reward");
            var stressCopy=M2RunState.RestoreValidated(JsonUtility.FromJson<M2RunSnapshot>(JsonUtility.ToJson(stress.Snapshot())),weights,prices,content);
            Require(stressCopy.CashCents==5280&&stressCopy.SoldCount==420&&!stressCopy.CompleteHelp(),"Multi-day JSON roundtrip retains assets and rejects helper replay");
            M4SaveData bad=Data(failure);bad.run.soldUnits[0].spawnedDay=3;Require(!session.Validate(bad),"Future source generation rejected");
            bad=Data(failure);bad.run.clearedUnits[0].unitId=bad.run.soldUnits[0].unitId;Require(!session.Validate(bad),"Cleared and sold physical identity duplication rejected");
            bad=Data(failure);bad.run.claimedSourceIds=bad.run.claimedSourceIds.Skip(1).ToArray();Require(!session.Validate(bad),"Missing historical claim rejected");
            string fixture=Path.GetFullPath("../docs/m5/fixtures/m4-save-schema3.json");
            var legacy=JsonUtility.FromJson<LegacyEnvelope>(File.ReadAllText(fixture));var original=JsonUtility.FromJson<M4SaveData>(legacy.payload);
            M4SaveData Upgrade(M4SaveData value)=>M5SaveUpgrade.Normalize(value,weights,prices,content);
            var upgraded=Upgrade(original);
            Require(upgraded!=null&&upgraded.run.schemaVersion==4&&original.run.schemaVersion==3&&original.run.groundDrops.Length==1,"Actual M4 fixture upgrades a detached copy, leaving original data unchanged");
            Require(session.Validate(upgraded)&&upgraded.run.cashCents==18&&upgraded.run.life.helpMealClaimed&&upgraded.run.life.dayIndex==2&&upgraded.run.groundDrops.Length==0&&upgraded.run.clearedUnits.Length==1,"M4 migration preserves18c/day2/helper and retires only stale previous-day ground");
            Require(Upgrade(upgraded)==upgraded,"Current schema normalization is idempotent");
            string directory=Path.GetFullPath(Path.Combine("Logs","M5SaveValidation",Guid.NewGuid().ToString("N")));Directory.CreateDirectory(directory);
            var store=new M4SaveStore(directory,Upgrade);File.Copy(fixture,store.Path);
            Require(store.Load(session.Validate,out M4SaveData loaded,out bool fallback,out _)&&!fallback&&loaded.run.schemaVersion==4,"Actual checksummed M4 file loads through migration before validation");
            store.Save(loaded,session.Validate);
            Require(File.ReadAllText(store.Path+".bak")==File.ReadAllText(fixture),"First M5 save retains the original valid M4 bytes as backup");
            File.WriteAllText(store.Path,"broken");
            Require(store.Load(session.Validate,out loaded,out fallback,out _)&&fallback&&loaded.run.clearedUnits.Length==1,"M4 backup fallback migrates once without resurrecting expired items");
            store.Save(Data(failure),session.Validate);
            Require(store.Load(session.Validate,out loaded,out fallback,out _)&&!fallback&&loaded.run.cashCents==18&&loaded.run.clearedUnits.Length==7,"Recovery state saves and reloads seven cleared identities without revival");
            string validBytes=File.ReadAllText(store.Path);var oversized=Data(failure);oversized.parameters=new string('x',1000001);bool sizeRejected=false;
            try{store.Save(oversized,_=>true);}catch(InvalidDataException){sizeRejected=true;}
            Require(sizeRejected&&File.ReadAllText(store.Path)==validBytes,"Oversized serialization cannot replace a readable save even with permissive caller validation");
            Require(weights.SourceRefreshDays==1&&session.Game.World.Points.Length==20,"Tests leave live scene and default refresh fixture unchanged");
            Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/M5EditModeValidation.txt",checks);Debug.Log("M5_VERIFY_OK: "+checks.Count+" checks\n"+string.Join("\n",checks));
        }

        [MenuItem("Reborn 1985/M5/Build Windows x64 development player")]
        public static void BuildWindows()
        {
            Verify();EditorSceneManager.OpenScene(M1ProjectTools.ScenePath);PlayerSettings.productName="Reborn1985";PlayerSettings.bundleVersion="0.5.0";AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Builds/M5-Windows");
            BuildReport report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{M1ProjectTools.ScenePath},target=BuildTarget.StandaloneWindows64,locationPathName="Builds/M5-Windows/Reborn1985.exe",options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("M5 build failed: "+report.summary.result);
            Directory.CreateDirectory("Builds/M5-Windows/Verification");File.Copy("../docs/m5/fixtures/m4-save-schema3.json","Builds/M5-Windows/Verification/M4Schema3.json",true);
            File.WriteAllText("Builds/M5-Windows/试玩说明.txt","《重生80年代》M5 跨日求生灰盒试玩\n\n完整解压，双击Reborn1985.exe。Unity6000.3.25f1，Windows x64。\n"+
                "WASD/方向键移动，E近距离交互，Enter确认，Esc返回；Tab/I背包，1—4选择，X丢弃；L家书，J线索；F5保存，F9读取确认。\n"+
                "七个原有来源每点每天一件，属于暂定测试规则。超重不领取。同日重访/读档/丢弃重拾不会额外产物。\n"+
                "陈伯称重：两纸两瓶3kg售0.44元；林婶基础餐0.26元，剩0.18元。无钱可一次帮工换餐，跨日不重置。\n"+
                "A-01原破旧铺盖免费休息到次日，清理上日地面丢弃物，保留背包/现金/家书/帮工/线索。七个来源进入新一轮，可从零现金继续谋生。\n"+
                "启动先进入新运行，F9主动读取。正常存档在AppData/LocalLow/QigaiPrototype/Reborn1985/Saves，保留.bak。支持M4旧档升级，旧档过期地面物按跨日规则清理。\n"+
                "Verification目录只供显式开发验证，不要当成自己的存档。家书正文、历史物价与最终美术仍待确认；旧壶只观察，未开发P1。\n关闭窗口退出。完整测试与限制见docs/m5。\n");
            File.WriteAllText("Logs/M5BuildResult.json",JsonUtility.ToJson(new BuildInfo{bytes=(long)report.summary.totalSize},true));Debug.Log("M5_BUILD_OK: Windows x64 Mono, "+report.summary.totalSize+" bytes");
        }
        [Serializable]private sealed class LegacyEnvelope{public string payload="";}
        [Serializable]private sealed class BuildInfo{public string unityVersion=Application.unityVersion;public string result="Succeeded",target="Windows x64 Mono Development";public long bytes;}
    }
}
