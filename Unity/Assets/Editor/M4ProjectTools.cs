using System;
using System.Collections.Generic;
using System.IO;
using Reborn1985.M1;
using Reborn1985.M2;
using Reborn1985.M3;
using Reborn1985.M4;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Reborn1985.Editor
{
    public static class M4ProjectTools
    {
        private const string SettingsPath="Assets/Settings/M4Settings.asset";
        [MenuItem("Reborn 1985/M4/Attach life and save to existing scene")]
        public static void Attach()
        {
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var scene=EditorSceneManager.OpenScene(M1ProjectTools.ScenePath);
            var game=UnityEngine.Object.FindFirstObjectByType<M2Scavenging>();var trade=UnityEngine.Object.FindFirstObjectByType<M3Trade>();var hud=UnityEngine.Object.FindFirstObjectByType<M2Hud>();
            if(game==null||trade==null||hud==null)throw new InvalidOperationException("Existing M3 scene is required");
            var settings=AssetDatabase.LoadAssetAtPath<M4Settings>(SettingsPath);
            if(settings==null){settings=ScriptableObject.CreateInstance<M4Settings>();AssetDatabase.CreateAsset(settings,SettingsPath);}
            if(!game.TryGetComponent(out M4Session session))session=game.gameObject.AddComponent<M4Session>();
            if(!game.TryGetComponent(out M4Hud display))display=game.gameObject.AddComponent<M4Hud>();
            session.Configure(game,trade,settings);display.Configure(session,hud);hud.SetStageTitle("重生80年代 · M4 生活与开局");
            // Stable from the first save-capable version onward, so later milestones find saves.
            PlayerSettings.productName="Reborn1985";PlayerSettings.bundleVersion="0.4.0";
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("M4_ATTACH_OK: existing 20 points/map retained; life and save components attached.");
        }
        [MenuItem("Reborn 1985/M4/Verify life save and M1 M2 M3 regression")]
        public static void Verify()
        {
            M3ProjectTools.Verify();EditorSceneManager.OpenScene(M1ProjectTools.ScenePath);
            var session=UnityEngine.Object.FindFirstObjectByType<M4Session>();
            var weights=AssetDatabase.LoadAssetAtPath<M2Settings>("Assets/Settings/M2Settings.asset");
            var prices=AssetDatabase.LoadAssetAtPath<M3TradeSettings>("Assets/Settings/M3TradeSettings.asset");
            var settings=AssetDatabase.LoadAssetAtPath<M4Settings>(SettingsPath);
            var checks=new List<string>();
            void Require(bool value,string name){if(!value)throw new InvalidOperationException("M4_FAIL: "+name);checks.Add("PASS "+name);}
            Require(session!=null&&UnityEngine.Object.FindFirstObjectByType<M4Hud>()!=null,"M4 life/UI components serialized in existing scene");
            Require(settings.BreakfastCents==26,"Approved provisional breakfast cost 26 cents");
            session.Initialize();Physics2D.SyncTransforms();
            var state=new M2RunState(weights);
            Require(!state.TryBuyBreakfast(26)&&state.CashCents==0&&state.Life.purchasedMeals==0,"Insufficient cash does not buy or debit");
            Require(state.OfferHelp(26)&&!state.Life.helpCompleted,"No-money helper accepts one small task");
            M2RunSnapshot beforeHelp=state.Snapshot();
            Require(state.CompleteHelp()&&state.Life.helpMealClaimed&&state.CashCents==0,"Help gives exactly one meal and no cash");
            for(int i=0;i<50;i++)if(state.CompleteHelp()||state.OfferHelp(26))throw new InvalidOperationException("Help duplication");
            Require(state.Life.helpMealClaimed&&state.Life.purchasedMeals==0,"50 repeat help actions cannot issue extra meals");
            foreach(var item in new[]{("B-02",ItemKind.Paper),("D-01",ItemKind.Paper),("D-02",ItemKind.Bottle),("D-03",ItemKind.Bottle)})Require(state.TryCollectSource(item.Item1,item.Item2),"M2 collect "+item.Item1);
            Require(state.TrySell(state.QuoteAllScrap(prices),prices,out _)==SaleResult.Success&&state.CashCents==44,"M3 first income remains 44 cents");
            Require(!state.OfferHelp(26),"Completed help cannot be offered again with cash");
            Require(state.TryBuyBreakfast(26)&&state.CashCents==18&&state.Life.purchasedMeals==1&&state.Life.mealExpenseCents==26,"L-01 purchase consumes26 cents:44 to18, one paid meal");
            Require(!state.TryBuyBreakfast(26)&&state.CashCents==18,"Further unaffordable purchase is refused without partial debit");
            foreach(LifeView view in new[]{LifeView.Letter,LifeView.Year,LifeView.Notice,LifeView.Puddle,LifeView.Kettle,LifeView.Resident})state.MarkSeen(view);
            state.MarkOpeningSeen();
            Require(state.Life.letterRead&&state.Life.yearRead&&state.Life.kettleObserved&&state.Life.residentTalked,"Optional story/clue flags recorded without item rewards");
            Require(state.TryCollectSource("F-01",ItemKind.Paper)&&state.TryDropOne(ItemKind.Paper,session.Game.World.RegionEntry("F"),out _),"Ground unit retains ownership and creation day");
            Require(state.Rest()&&state.Life.dayIndex==2&&state.CashCents==18&&state.GroundCount==0&&!state.Claimed("F-01")&&state.ClearedCount==1&&state.Life.helpMealClaimed,"M5 free rest cleans yesterday ground, refreshes sources and retains helper flag");
            Require(state.TryCollectSource("F-01",ItemKind.Paper)&&state.TryDropOne(ItemKind.Paper,session.Game.World.RegionEntry("F"),out _),"Fresh next-day unit can be dropped independently of the cleared first-day unit");
            M4SaveData Data(M2RunSnapshot snapshot)=>new M4SaveData{parameters=M4SaveValidation.Signature(weights,prices,settings),position=session.Game.World.RegionEntry("A"),facing=Vector2.down,run=snapshot};
            var data=Data(state.Snapshot());
            Require(session.Validate(data),"Complete food/cash/story/day/ground snapshot validates against real world");
            var restored=M2RunState.RestoreValidated(data.run,weights,prices,settings);
            Require(restored.CashCents==18&&restored.GroundCount==1&&restored.Count(ItemKind.Letter)==1&&restored.Life.dayIndex==2&&restored.Life.helpMealClaimed,"Restored state retains exact cash, ground, letter and once-only reward");
            Require(!restored.CompleteHelp()&&!restored.TryCollectSource("F-01",ItemKind.Paper),"Reload cannot duplicate help or original search yield");
            var pending=new M2RunState(weights);pending.TryCollectSource("B-02",ItemKind.Paper);var oldQuote=pending.QuoteAllScrap(prices);
            Require(M2RunState.RestoreValidated(pending.Snapshot(),weights,prices,settings).TrySell(oldQuote,prices,out _)==SaleResult.StaleQuote,"Quotes from an old live state never survive reload");
            var pre=M2RunState.RestoreValidated(beforeHelp,weights,prices,settings);
            Require(!pre.Life.helpMealClaimed&&pre.Life.helpOffered&&pre.CompleteHelp()&&!pre.CompleteHelp(),"L-03 before-help snapshot remains unclaimed and can pay exactly once");
            var bad=Data(state.Snapshot());bad.run.inventory[0].kind=ItemKind.Metal;Require(!session.Validate(bad),"Corrupt letter identity rejected");
            bad=Data(state.Snapshot());bad.run.groundDrops[0].unit.unitId="D-01";Require(!session.Validate(bad),"Duplicate physical unit across sold and ground rejected");
            bad=Data(state.Snapshot());bad.run.cashCents++;Require(!session.Validate(bad),"Cash/income/expense mismatch rejected");
            bad=Data(state.Snapshot());bad.run.life.helpCompleted=false;Require(!session.Validate(bad),"Mixed help completed/claimed flags rejected");
            bad=Data(state.Snapshot());bad.position=new Vector2(float.NaN,1);Require(!session.Validate(bad),"Non-finite player position rejected");
            bad=Data(state.Snapshot());bad.position=session.Game.World.Center(new Vector2Int(0,0));Require(!session.Validate(bad),"Player inside map wall cannot be restored");
            bad=Data(state.Snapshot());bad.version=99;Require(!session.Validate(bad),"Future save schema rejected without silent migration");
            bad=Data(state.Snapshot());bad.parameters="different";Require(!session.Validate(bad),"Different price/weight parameters rejected explicitly");
            string directory=Path.GetFullPath(Path.Combine("Logs","M4SaveValidation",Guid.NewGuid().ToString("N")));
            var store=new M4SaveStore(directory);store.Save(data,session.Validate);
            Require(store.Load(session.Validate,out M4SaveData disk,out bool fallback,out _)&&!fallback&&disk.run.cashCents==18,"Actual disk JSON/sha256 first save and load succeed");
            store.Save(data,session.Validate);Require(File.Exists(store.Path+".bak"),"Atomic second save retains last valid backup");
            File.WriteAllText(store.Path+".tmp","incomplete write");Require(store.Load(session.Validate,out _,out fallback,out _)&&!fallback,"Interrupted temp file is ignored, good primary survives");
            File.WriteAllText(store.Path,"{truncated");Require(store.Load(session.Validate,out disk,out fallback,out _)&&fallback&&disk.run.life.helpMealClaimed,"Corrupted primary falls back to valid once-only reward state");
            store.Save(data,session.Validate);Require(store.Load(session.Validate,out _,out fallback,out _)&&!fallback,"Valid new save replaces damaged primary without losing backup");
            File.WriteAllText(store.Path,"broken");File.WriteAllText(store.Path+".bak","broken");
            Require(!store.Load(session.Validate,out _,out _,out _),"Both invalid files refuse load, no guessed partial recovery");
            store.Save(data,session.Validate);string good=File.ReadAllText(store.Path);bool rejected=false;
            File.WriteAllText(store.Path,good.Replace("\"version\": 1","\"version\": 99"));
            Require(!store.Load(session.Validate,out _,out _,out _),"Future envelope is refused cleanly, without throwing or applying state");
            File.WriteAllText(store.Path,good.Replace("\"sha256\": \"","\"sha256\": \"BROKEN"));
            Require(!store.Load(session.Validate,out _,out _,out _),"Checksum mismatch is refused cleanly without partial recovery");
            File.WriteAllText(store.Path,good);
            try{store.Save(bad,session.Validate);}catch(InvalidDataException){rejected=true;}
            Require(rejected&&File.ReadAllText(store.Path)==good,"Rejected save cannot overwrite previous valid bytes");
            Require(state.CashCents==18&&state.Life.helpMealClaimed&&state.GroundCount==1,"All disk fault tests leave live model unchanged");
            Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/M4EditModeValidation.txt",checks);Debug.Log("M4_VERIFY_OK: "+checks.Count+" checks\n"+string.Join("\n",checks));
        }
        [MenuItem("Reborn 1985/M4/Build Windows x64 development player")]
        public static void BuildWindows()
        {
            Verify();EditorSceneManager.OpenScene(M1ProjectTools.ScenePath);Directory.CreateDirectory("Builds/M4-Windows");
            BuildReport report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{M1ProjectTools.ScenePath},target=BuildTarget.StandaloneWindows64,locationPathName="Builds/M4-Windows/Reborn1985.exe",options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("M4 build failed: "+report.summary.result);
            File.WriteAllText("Builds/M4-Windows/试玩说明.txt","《重生80年代》M4 灰盒试玩\n\n保留完整目录，双击Reborn1985.exe。Unity6000.3.25f1，Windows x64。\n"+
                "WASD/方向键移动，E近距离交互，Enter确认，Esc关闭。Tab/I背包，1—4选择，X丢弃；家书行按钮阅读。\n"+
                "L家书，J线索，F5保存，F9读取确认；左上也有保存/读取按钮。\n"+
                "C区林婶：基础餐0.26元，基础四物出售0.44元后买餐剩0.18元。缺钱时接受一次帮工，到C-04整理并领餐，不发现金。\n"+
                "A-02旧报纸、A-03积水、B-01公告；E-01旧壶/E-02居民只提供线索，不买卖。\n"+
                "A-01原铺盖免费休息到次日，当前M4不刷新资源、不清理旧地面物，完整跨日规则留M5。\n"+
                "启动默认为新运行；F9读取上次手动存档。存档位于Unity persistentDataPath/Saves/save.json，保留.bak备份。\n"+
                "家书正文、具体月日与帮工美术为原型占位待审；没有饥饿条、住房、旧货经营或P1功能。关闭窗口退出。完整验证见docs/m4。\n");
            File.WriteAllText("Logs/M4BuildResult.json",JsonUtility.ToJson(new BuildInfo{bytes=(long)report.summary.totalSize},true));Debug.Log("M4_BUILD_OK: Windows x64 Mono, "+report.summary.totalSize+" bytes");
        }
        [Serializable]private sealed class BuildInfo{public string unityVersion=Application.unityVersion;public string result="Succeeded",target="Windows x64 Mono Development";public long bytes;}
    }
}
