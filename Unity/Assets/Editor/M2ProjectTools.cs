using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Reborn1985.M1;
using Reborn1985.M2;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Reborn1985.Editor
{
    public static class M2ProjectTools
    {
        private const string SettingsPath = "Assets/Settings/M2Settings.asset";
        [MenuItem("Reborn 1985/M2/Attach M2 to saved M1 scene (keep map)")]
        public static void AttachToExistingScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(M1ProjectTools.ScenePath);
            M1World world = UnityEngine.Object.FindFirstObjectByType<M1World>();
            PlayerMover player = UnityEngine.Object.FindFirstObjectByType<PlayerMover>();
            if (world == null || player == null || world.Points.Length != 20) throw new InvalidOperationException("Existing M1 scene is required; never generate a replacement map.");
            var parameters = AssetDatabase.LoadAssetAtPath<M2Settings>(SettingsPath);
            if (parameters == null) { parameters = ScriptableObject.CreateInstance<M2Settings>(); AssetDatabase.CreateAsset(parameters, SettingsPath); }
            foreach (var item in new[] {
                ("B-02", ItemKind.Paper, false), ("D-01", ItemKind.Paper, false),
                ("D-02", ItemKind.Bottle, false), ("D-03", ItemKind.Bottle, false),
                ("F-01", ItemKind.Paper, true), ("F-02", ItemKind.Bottle, true), ("F-03", ItemKind.Metal, false) })
            {
                GreyboxPoint point = world.Points.Single(value => value.Id == item.Item1);
                if (!point.TryGetComponent(out M2Pickup pickup)) pickup = point.gameObject.AddComponent<M2Pickup>();
                pickup.Configure(item.Item1, item.Item2, item.Item3);
                pickup.ShowState(true);
            }
            if (!world.TryGetComponent(out M2Scavenging service)) service = world.gameObject.AddComponent<M2Scavenging>();
            service.Configure(world, player, parameters, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholders/Marker.png"));
            if (!world.TryGetComponent(out M2Hud hud)) hud = world.gameObject.AddComponent<M2Hud>();
            hud.Configure(service, Camera.main);
            world.GetComponent<GreyboxOverlay>().SetScreenOverlay(false);
            PlayerSettings.productName = "Reborn1985 M2";
            PlayerSettings.bundleVersion = "0.2.0";
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("M2_ATTACH_OK: seven existing points extended; M1 tiles/transforms/controller/camera unchanged.");
        }

        [MenuItem("Reborn 1985/M2/Verify inventory and M1 regression")]
        public static void Verify()
        {
            M1ProjectTools.Verify();
            EditorSceneManager.OpenScene(M1ProjectTools.ScenePath);
            M1World world = UnityEngine.Object.FindFirstObjectByType<M1World>();
            M2Scavenging game = UnityEngine.Object.FindFirstObjectByType<M2Scavenging>();
            M2Settings settings = AssetDatabase.LoadAssetAtPath<M2Settings>(SettingsPath);
            var checks = new List<string>();
            void Require(bool condition, string description)
            { if (!condition) throw new InvalidOperationException("M2_FAIL: " + description); checks.Add("PASS " + description); }
            Require(game != null && UnityEngine.Object.FindFirstObjectByType<M2Hud>() != null, "M2 service and HUD attached to existing scene");
            Require(settings.CapacityGrams == 5000 && settings.Weight(ItemKind.Paper) == 1000 && settings.Weight(ItemKind.Bottle) == 500 && settings.Weight(ItemKind.Metal) == 3000 && settings.Weight(ItemKind.Letter) == 0, "Exact test weights in integer grams");
            var positions = new[] { ("A-01",6,5), ("A-02",4,7), ("A-03",6,8), ("A-04",6,11), ("B-01",5,14), ("B-02",6,16),
                ("D-01",15,20), ("D-02",26,20), ("D-03",34,20), ("D-04",10,22), ("F-01",27,11), ("F-02",33,11), ("F-03",30,10),
                ("C-01",18,29), ("C-04",20,26), ("G-01",41,32), ("G-03",41,31), ("G-04",41,28), ("E-01",52,12), ("E-02",50,11) };
            foreach (var position in positions)
            {
                GreyboxPoint point = world.Points.Single(value => value.Id == position.Item1);
                Require(Vector2.Distance(point.transform.position, world.Center(new Vector2Int(position.Item2, position.Item3))) < .001f, "M1 position retained " + position.Item1);
            }
            M2Pickup[] pickups = world.GetComponentsInChildren<M2Pickup>();
            Require(pickups.Length == 7 && pickups.All(pickup => pickup.GetComponent<Collider2D>() == null), "Only seven approved sources; no pickup blocks movement");
            foreach (M2Pickup pickup in pickups)
            {
                ItemKind expected = pickup.Id == "F-03" ? ItemKind.Metal : pickup.Id == "D-02" || pickup.Id == "D-03" || pickup.Id == "F-02" ? ItemKind.Bottle : ItemKind.Paper;
                Require(pickup.Item == expected && pickup.IsSearch == (pickup.Id == "F-01" || pickup.Id == "F-02") && !pickup.IsDropped, "Source mapping " + pickup.Id);
            }
            Require(world.Points.Where(point => new[] { "D-04", "C-01", "G-03", "E-01", "E-02" }.Contains(point.Id)).All(point => point.GetComponent<M2Pickup>() == null), "Decorations, NPCs and old kettle remain non-collectible");
            var state = new M2RunState(settings);
            Require(state.Count(ItemKind.Letter) == 1 && state.TotalGrams == 0, "One protected zero-weight letter on new run");
            Require(!state.TryDropOne(ItemKind.Letter, Vector2.zero, out _) && !state.TryCollectSource("duplicate-letter", ItemKind.Letter), "Letter cannot drop or generate again");
            foreach (var source in new[] { ("B-02",ItemKind.Paper), ("D-01",ItemKind.Paper), ("D-02",ItemKind.Bottle), ("D-03",ItemKind.Bottle) })
            {
                Require(state.TryCollectSource(source.Item1, source.Item2), "Collect " + source.Item1);
                Require(!state.TryCollectSource(source.Item1, source.Item2), "Reject duplicate " + source.Item1);
            }
            Require(state.Count(ItemKind.Paper) == 2 && state.Count(ItemKind.Bottle) == 2 && state.TotalGrams == 3000, "TEST1: two papers and two bottles = 3000 grams");
            Require(!state.TryCollectSource("F-03",ItemKind.Metal) && !state.Claimed("F-03") && state.TotalGrams == 3000, "TEST2: 6000 grams rejected without claiming metal");
            Require(state.TryDropOne(ItemKind.Paper, Vector2.one, out GroundDrop paper) && state.TotalGrams == 2000, "Drop removes exactly one paper and 1000 grams");
            Require(state.TryCollectSource("F-03", ItemKind.Metal) && state.TotalGrams == 5000, "TEST3: legal choice permits metal at exactly 5000 grams");
            Require(!state.TryCollectDrop(paper.unit.unitId) && state.HasDrop(paper.unit.unitId), "Ground re-pick also rejects excess weight");
            Require(!state.TryCollectSource("F-01", ItemKind.Paper) && !state.Claimed("F-01"), "Full-bag search leaves source unclaimed");
            Require(state.TryDropOne(ItemKind.Metal, Vector2.right, out _), "Free capacity for finite searches");
            Require(state.TryCollectSource("F-01", ItemKind.Paper) && state.TryCollectSource("F-02", ItemKind.Bottle), "Search each approved pile once");
            for (int i = 0; i < 50; i++)
                if (state.TryCollectSource("F-01", ItemKind.Paper) || state.TryCollectSource("F-02", ItemKind.Bottle)) throw new InvalidOperationException("Search duplication");
            Require(state.Count(ItemKind.Paper) == 2 && state.Count(ItemKind.Bottle) == 3 && state.TotalGrams == 3500, "TEST4: 50 repeated searches produce nothing extra");
            for (int i = 0; i < 100; i++)
            {
                ItemKind kind = i % 2 == 0 ? ItemKind.Paper : ItemKind.Bottle;
                if (!state.TryDropOne(kind, Vector2.zero, out GroundDrop drop) || !state.TryCollectDrop(drop.unit.unitId) || state.TryCollectDrop(drop.unit.unitId))
                    throw new InvalidOperationException("Drop/re-pick ownership error at cycle " + i);
            }
            Require(state.TotalGrams == 3500 && state.GroundCount == 2, "TEST5: 100 drop/re-pick cycles conserve units and weight");
            M2RunSnapshot snapshot = state.Snapshot();
            Require(snapshot.inventory.Concat(snapshot.groundDrops.Select(drop => drop.unit)).Select(unit => unit.unitId).Distinct().Count() == 8, "All seven claimed resources and letter have unique exclusive ownership");
            snapshot.inventory[0].kind = ItemKind.Metal;
            Require(state.Count(ItemKind.Letter) == 1 && state.TotalGrams == 3500, "Snapshot is detached from live state");
            Require(state.Claimed("B-02") && state.Claimed("F-01") && state.Claimed("F-03"), "Drops do not reset source or search claims");
            Require(!state.TryDropOne(ItemKind.Metal,Vector2.zero,out _), "Cannot drop zero-count item or create negative inventory");
            var edge = new InteractionEdge();
            Require(edge.Press(true) && !edge.Press(true) && !edge.Press(true) && !edge.Press(false) && edge.Press(true), "Held interaction triggers once; release required");
            Directory.CreateDirectory("Logs"); File.WriteAllLines("Logs/M2EditModeValidation.txt",checks);
            Debug.Log("M2_VERIFY_OK: " + checks.Count + " checks\n" + string.Join("\n",checks));
        }

        [MenuItem("Reborn 1985/M2/Build Windows x64 development player")]
        public static void BuildWindows()
        {
            Verify();
            EditorSceneManager.OpenScene(M1ProjectTools.ScenePath);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Directory.CreateDirectory("Builds/M2-Windows");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { M1ProjectTools.ScenePath }, target = BuildTarget.StandaloneWindows64,
                locationPathName = "Builds/M2-Windows/Reborn1985.exe", options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("M2 Windows build failed: " + report.summary.result);
            File.WriteAllText("Builds/M2-Windows/试玩说明.txt",
                "《重生80年代》M2 Windows x64灰盒试玩\n\n" +
                "双击Reborn1985.exe；请保留解压后的完整目录。\n" +
                "WASD/方向键移动；E拾取/搜索（每次需松开再按）；Tab或I开关背包；Esc关闭背包。\n" +
                "背包中1/2/3/4选择废纸/瓶子/金属/来信，X丢弃一件；也可鼠标点击丢弃按钮。\n" +
                "黄色为废纸，绿色为瓶子，蓝灰色为金属；灰色搜索堆表示本次已搜索。\n" +
                "基础路线：B-02和D-01各一份废纸，D-02/D-03各一只瓶，共3kg。\n" +
                "F-03金属3kg，直接拿会超重；放下一份纸后可拿到5kg。F-01/F-02各搜索一次。\n" +
                "母亲来信受保护且不计重。背包打开仍可移动、拾取。丢弃物不阻路、可重新拾取。\n" +
                "本版没有出售/现金、早餐、睡觉、时间、跨日或存档；关闭并重开会开始新的一次运行。\n" +
                "关闭窗口退出。Unity版本6000.3.25f1。完整说明和实际验证见GitHub docs/m2/。\n");
            File.WriteAllText("Logs/M2BuildResult.json", JsonUtility.ToJson(new BuildResultRecord { unityVersion = Application.unityVersion, bytes = (long)report.summary.totalSize, result = report.summary.result.ToString() },true));
            Debug.Log("M2_BUILD_OK: Windows x64 Mono, " + report.summary.totalSize + " bytes");
        }
        [Serializable] private sealed class BuildResultRecord { public string unityVersion; public string result; public string target = "Windows x64 Mono Development"; public long bytes; }
        public static void PrepareAndBuild() { AttachToExistingScene(); BuildWindows(); }
    }
}
