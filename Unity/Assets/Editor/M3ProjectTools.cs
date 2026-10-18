using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Reborn1985.M1;
using Reborn1985.M2;
using Reborn1985.M3;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Reborn1985.Editor
{
    public static class M3ProjectTools
    {
        private const string SettingsPath = "Assets/Settings/M3TradeSettings.asset";
        [MenuItem("Reborn 1985/M3/Attach trade to existing scene (keep map)")]
        public static void Attach()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(M1ProjectTools.ScenePath);
            M2Scavenging game = UnityEngine.Object.FindFirstObjectByType<M2Scavenging>();
            M2Hud hud = UnityEngine.Object.FindFirstObjectByType<M2Hud>();
            if (game == null || hud == null || game.World.Points.Length != 20) throw new InvalidOperationException("Accepted M2 scene is required.");
            var settings = AssetDatabase.LoadAssetAtPath<M3TradeSettings>(SettingsPath);
            if (settings == null) { settings = ScriptableObject.CreateInstance<M3TradeSettings>(); AssetDatabase.CreateAsset(settings,SettingsPath); }
            if (!game.TryGetComponent(out M3Trade trade)) trade = game.gameObject.AddComponent<M3Trade>();
            if (!game.TryGetComponent(out M3Hud display)) display = game.gameObject.AddComponent<M3Hud>();
            trade.Configure(game,settings); display.Configure(trade,game,hud);
            hud.SetStageTitle("重生80年代 · M3 废品收购");
            PlayerSettings.productName = "Reborn1985 M3"; PlayerSettings.bundleVersion = "0.3.0";
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("M3_ATTACH_OK: existing G-03 merchant and G-04 approach retained; no map generation.");
        }
        [MenuItem("Reborn 1985/M3/Verify trade and M1 M2 regression")]
        public static void Verify()
        {
            M2ProjectTools.Verify();
            EditorSceneManager.OpenScene(M1ProjectTools.ScenePath);
            var game = UnityEngine.Object.FindFirstObjectByType<M2Scavenging>();
            var trade = UnityEngine.Object.FindFirstObjectByType<M3Trade>();
            var prices = AssetDatabase.LoadAssetAtPath<M3TradeSettings>(SettingsPath);
            var weights = AssetDatabase.LoadAssetAtPath<M2Settings>("Assets/Settings/M2Settings.asset");
            var checks = new List<string>();
            void Require(bool value,string name) { if (!value) throw new InvalidOperationException("M3_FAIL: "+name); checks.Add("PASS "+name); }
            M2RunState BaseBag()
            {
                var state = new M2RunState(weights);
                foreach (var source in new[] { ("B-02",ItemKind.Paper),("D-01",ItemKind.Paper),("D-02",ItemKind.Bottle),("D-03",ItemKind.Bottle) })
                    if (!state.TryCollectSource(source.Item1,source.Item2)) throw new InvalidOperationException("Fixture collect failed");
                return state;
            }
            Require(trade != null && UnityEngine.Object.FindFirstObjectByType<M3Hud>() != null,"Trade and UI serialized in existing M2 scene");
            Require(prices.CentsPerKg(ItemKind.Paper)==12 && prices.CentsPerKg(ItemKind.Bottle)==20 && prices.CentsPerKg(ItemKind.Metal)==16 && prices.CentsPerKg(ItemKind.Letter)==0,"Approved test rates 12/20/16 cents per kilogram; letter excluded");
            Require(game.World.CanStand(new Vector2Int(41,28)),"Original G-04 counter approach clear");
            Require(M3TradeSettings.Yuan(44)=="0.44 元" && M3TradeSettings.Yuan(48)=="0.48 元" && M3TradeSettings.Yuan(0)=="0.00 元","Exact decimal money display without float");
            var empty = new M2RunState(weights);
            Require(empty.CashCents==0 && empty.ScrapIncomeCents==0 && empty.Count(ItemKind.Letter)==1,"New run starts at zero cents with protected letter");
            Require(empty.QuoteAllScrap(prices).Status==SaleResult.Empty && empty.TrySell(empty.QuoteAllScrap(prices),prices,out _)==SaleResult.Empty,"E-03 letter-only inventory cannot make a sale");
            Require(empty.Snapshot().receipts.Length==0 && empty.TotalGrams==0 && empty.CashCents==0,"Empty attempt changes neither units, receipts nor cash");
            var state = BaseBag();
            SaleQuote quote = state.QuoteAllScrap(prices);
            Require(quote.Status==SaleResult.Success && quote.TotalGrams==3000 && quote.TotalCents==44,"E-01 two papers and two bottles quote exactly 3000g / 44 cents");
            Require(quote.Lines[0].Cents==24 && quote.Lines[1].Cents==20 && quote.Lines[1].CentsPerKg==20,"Paper24 + bottle20 cents; bottle rate is per kg");
            Require(state.Count(ItemKind.Paper)==2 && state.Count(ItemKind.Bottle)==2 && state.CashCents==0 && state.SoldCount==0,"Opening and reading quote makes no asset mutation");
            Require(state.TryDropOne(ItemKind.Paper,Vector2.zero,out GroundDrop drop),"Inventory can change independently of stale quote");
            Require(state.TrySell(quote,prices,out _)==SaleResult.StaleQuote && state.CashCents==0 && state.TotalGrams==2000,"E-03 missing quantity rejects whole sale atomically");
            Require(state.TryCollectDrop(drop.unit.unitId),"M2 ground paper can be re-picked before re-quoting");
            Require(state.TrySell(quote,prices,out _)==SaleResult.StaleQuote,"Drop/re-pick does not resurrect old quote even if counts match");
            SaleQuote fresh = state.QuoteAllScrap(prices);
            Require(state.TrySell(fresh,prices,out SaleReceipt receipt)==SaleResult.Success,"Explicit fresh confirmation succeeds");
            Require(state.CashCents==44 && state.ScrapIncomeCents==44 && state.TotalGrams==0 && state.SoldCount==4,"E-01 inventory deduction, weight and cash commit together");
            Require(receipt.cashBefore==0 && receipt.cashAfter==44 && receipt.incomeCents==44 && receipt.unitIds.Length==4 && receipt.totalGrams==3000,"Receipt records actual identities, grams and cents");
            Require(state.Count(ItemKind.Letter)==1 && !receipt.unitIds.Contains(M2RunState.LetterId),"Mother letter is absent from sale and retained");
            for (int i=0;i<100;i++) if (state.TrySell(fresh,prices,out _) != SaleResult.StaleQuote) throw new InvalidOperationException("Repeated sale accepted");
            Require(state.CashCents==44 && state.Snapshot().receipts.Length==1 && state.SoldCount==4,"E-03 100 repeated confirmations do not pay twice");
            foreach (string id in new[]{"B-02","D-01","D-02","D-03"})
                Require(!state.TryCollectSource(id,id=="B-02"||id=="D-01"?ItemKind.Paper:ItemKind.Bottle),"Sold source remains claimed "+id);
            Require(state.TryCollectSource("F-03",ItemKind.Metal),"M2 capacity is freed after sale");
            SaleQuote metal = state.QuoteAllScrap(prices);
            Require(metal.TotalGrams==3000 && metal.TotalCents==48,"E-02 3kg metal at 16 cents/kg quotes 48 cents");
            Require(state.TrySell(metal,prices,out _) == SaleResult.Success && state.CashCents==92,"Metal sale accumulates exact cash 44+48=92");
            Require(state.TryCollectSource("F-01",ItemKind.Paper) && state.TryCollectSource("F-02",ItemKind.Bottle),"Finite unclaimed search resources remain usable");
            Require(state.TryDropOne(ItemKind.Bottle,Vector2.one,out GroundDrop bottle),"One bottle transfers to ground");
            SaleQuote paperOnly = state.QuoteAllScrap(prices);
            Require(paperOnly.TotalCents==12 && paperOnly.UnitIds.Count==1,"Ground bottle is not sold from inventory");
            Require(state.TrySell(paperOnly,prices,out _) == SaleResult.Success && state.HasDrop(bottle.unit.unitId) && state.CashCents==104,"Sale leaves ground ownership intact");
            Require(state.TryCollectDrop(bottle.unit.unitId) && state.TrySell(state.QuoteAllScrap(prices),prices,out _) == SaleResult.Success,"Re-picked unsold ground bottle can be sold once");
            M2RunSnapshot snapshot = state.Snapshot();
            Require(state.CashCents==114 && state.ScrapIncomeCents==114 && snapshot.receipts.Length==4 && snapshot.soldUnits.Length==7,"All finite resources total exactly 114 cents in four receipts");
            Require(snapshot.soldUnits.Select(unit=>unit.unitId).Distinct().Count()==7 && snapshot.inventory.Length==1 && snapshot.groundDrops.Length==0,"Ownership conserved across inventory, ground and sold sink");
            Require(snapshot.claimedSourceIds.Length==7 && state.TotalGrams==0 && state.Count(ItemKind.Letter)==1,"All source claims and protected letter survive sales");
            snapshot.receipts[0].incomeCents=999; snapshot.soldUnits[0].kind=ItemKind.Letter; snapshot.cashCents=999;
            Require(state.CashCents==114 && state.Snapshot().receipts[0].incomeCents==44 && state.Snapshot().soldUnits.All(unit=>unit.kind!=ItemKind.Letter),"M4 transfer snapshot is detached from authoritative state");
            M3TradeSettings altered = UnityEngine.Object.Instantiate(prices);
            try
            {
                var pendingState = BaseBag(); SaleQuote pending = pendingState.QuoteAllScrap(altered);
                var edit = new SerializedObject(altered); edit.FindProperty("paperCentsPerKg").intValue=13; edit.ApplyModifiedPropertiesWithoutUndo();
                Require(pendingState.TrySell(pending,altered,out _) == SaleResult.StaleQuote && pendingState.CashCents==0,"Price change invalidates already displayed quote without payment");
                var oneBottle = new M2RunState(weights); oneBottle.TryCollectSource("D-02",ItemKind.Bottle);
                edit.Update(); edit.FindProperty("bottleCentsPerKg").intValue=21; edit.ApplyModifiedPropertiesWithoutUndo();
                SaleQuote fractional = oneBottle.QuoteAllScrap(altered);
                Require(fractional.Status==SaleResult.FractionalCent && oneBottle.TrySell(fractional,altered,out _) == SaleResult.FractionalCent,"Unapproved half-cent parameters refuse sale; no invented rounding");
                Require(oneBottle.Count(ItemKind.Bottle)==1 && oneBottle.CashCents==0,"Rejected fractional amount retains goods and cash");
                edit.Update(); edit.FindProperty("bottleCentsPerKg").intValue=0; edit.ApplyModifiedPropertiesWithoutUndo();
                SaleQuote invalid = oneBottle.QuoteAllScrap(altered);
                Require(invalid.Status==SaleResult.InvalidPrice && oneBottle.TrySell(invalid,altered,out _) == SaleResult.InvalidPrice,"Invalid price cannot consume goods for zero payment");
            }
            finally { UnityEngine.Object.DestroyImmediate(altered); }
            Directory.CreateDirectory("Logs"); File.WriteAllLines("Logs/M3EditModeValidation.txt",checks);
            Debug.Log("M3_VERIFY_OK: "+checks.Count+" checks\n"+string.Join("\n",checks));
        }
        [MenuItem("Reborn 1985/M3/Build Windows x64 development player")]
        public static void BuildWindows()
        {
            Verify(); EditorSceneManager.OpenScene(M1ProjectTools.ScenePath);
            Directory.CreateDirectory("Builds/M3-Windows");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{M1ProjectTools.ScenePath},target=BuildTarget.StandaloneWindows64,
                locationPathName="Builds/M3-Windows/Reborn1985.exe",options=BuildOptions.Development });
            if (report.summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("M3 build failed: "+report.summary.result);
            File.WriteAllText("Builds/M3-Windows/试玩说明.txt","《重生80年代》M3 Windows x64试玩\n\n保留整个目录，双击Reborn1985.exe。\n"+
                "WASD/方向键移动，E拾取/搜索；Tab/I背包，1—4选择、X丢弃或鼠标按钮，Esc关包。\n"+
                "到G区陈伯柜台前G-04站位按E：查看称重和报价；Enter或点击出售全部废品成交；Esc或按钮取消。\n"+
                "E只打开，不会成交。交易打开时可移动，离开站位自动关闭；先Esc退出交易再开背包。\n"+
                "基础B-02/D-01废纸各1份、D-02/D-03玻璃瓶各1只，共3kg，出售收入0.44元。F-03金属3kg，单独出售0.48元。\n"+
                "收购测试价：纸0.12元/kg、瓶0.20元/kg、金属0.16元/kg；不是历史考证价格。\n"+
                "母亲来信不计重、不出售、不丢弃。原有拾荒、有限搜索、超重保护和丢弃重拾继续可用。\n"+
                "本版没有早餐/帮工、休息、游戏时间、存档或跨日；关闭重开即新运行、现金归零。\n"+
                "完整验证见仓库docs/m3/。Unity 6000.3.25f1。关闭窗口退出。\n");
            File.WriteAllText("Logs/M3BuildResult.json",JsonUtility.ToJson(new BuildInfo {bytes=(long)report.summary.totalSize},true));
            Debug.Log("M3_BUILD_OK: Windows x64 Mono, "+report.summary.totalSize+" bytes");
        }
        [Serializable] private sealed class BuildInfo { public string unityVersion=Application.unityVersion; public string result="Succeeded"; public string target="Windows x64 Mono Development"; public long bytes; }
    }
}
