using System;
using System.Collections.Generic;
using System.IO;
using Reborn1985.M1;
using Reborn1985.M2;
using Reborn1985.M4;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Reborn1985.Editor
{
    public static class P0UsabilityProjectTools
    {
        [MenuItem("Reborn 1985/P0 usability/Verify retained M5 and presentation contract")]
        public static void Verify()
        {
            M5ProjectTools.Verify();EditorSceneManager.OpenScene(M1ProjectTools.ScenePath);
            var session=UnityEngine.Object.FindFirstObjectByType<M4Session>();session.Initialize();Physics2D.SyncTransforms();
            var checks=new List<string>();
            void Require(bool ok,string name){if(!ok)throw new InvalidOperationException("P0_USABILITY_FAIL "+name);checks.Add("PASS "+name);}
            Require(session.Game.State.Snapshot().schemaVersion==4,"UI polish retains exact M5 save schema4");
            Require(session.HasUnsavedChanges&&session.SaveStatusText.Contains("尚未手动保存"),"Fresh run does not claim to have been saved");
            Require(!new SerializedObject(UnityEngine.Object.FindFirstObjectByType<GreyboxOverlay>()).FindProperty("showScreenOverlay").boolValue,"Full P0 HUD uses no redundant M1 world-label overlay");
            session.OpenLetter();Require(!session.ContextPrompt().Contains("Enter")&&session.ContextPrompt().Contains("Esc"),"Read-only letter prompt does not advertise a nonexistent confirm action");
            Require(session.Game.State.Count(ItemKind.Letter)==1&&session.Game.State.TotalGrams==0,"Reading remains optional and retains the protected zero-weight letter");session.Close();
            Require(session.OpenPoint("A-01")&&session.ContextPrompt().Contains("Enter"),"Rest still advertises its actual confirmation action");session.Close();
            Require(session.Game.State.Life.dayIndex==1&&session.Game.State.CashCents==0,"Opening and cancelling UI does not issue money or advance a day");
            Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/P0UsabilityEditModeValidation.txt",checks);Debug.Log("P0_USABILITY_VERIFY_OK: "+checks.Count+" checks\n"+string.Join("\n",checks));
        }
        [MenuItem("Reborn 1985/P0 usability/Build Windows x64 development player")]
        public static void BuildWindows()
        {
            Verify();EditorSceneManager.OpenScene(M1ProjectTools.ScenePath);PlayerSettings.productName="Reborn1985";PlayerSettings.bundleVersion="0.5.1";AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Builds/M5.1-Windows");
            BuildReport report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{M1ProjectTools.ScenePath},target=BuildTarget.StandaloneWindows64,locationPathName="Builds/M5.1-Windows/Reborn1985.exe",options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("P0 usability build failed: "+report.summary.result);
            Directory.CreateDirectory("Builds/M5.1-Windows/Verification");
            File.Copy("../docs/m5/fixtures/m4-save-schema3.json","Builds/M5.1-Windows/Verification/M4Schema3.json",true);
            File.Copy("../docs/p0-usability/fixtures/m5-save-schema4.json","Builds/M5.1-Windows/Verification/M5Schema4.json",true);
            File.WriteAllText("Builds/M5.1-Windows/试玩说明.txt","《重生80年代》M5.1 P0体验完善版\n\n完整解压，双击Reborn1985.exe。Unity6000.3.25f1，Windows x64。\n"+
                "WASD/方向键移动，E近距离交互，Enter确认可执行操作，Esc返回；Tab/I背包，1—4选择、X丢弃普通物品。L家书，J线索，F1可选操作说明。\n"+
                "F5保存，F9读取确认。启动先进入新运行，继续上次进度需F9。左上显示已保存/未保存变动/读取失败/备份恢复状态。\n"+
                "近距离目标显示单个提示，隔墙/远距不提示可取；背包与对话开启时不叠加浮动标签。操作说明可随时关闭，不锁定行走或强制顺序。\n"+
                "原有拾荒、5kg承重、陈伯44分基础收入、26分餐、一次性帮工、免费休息/有限刷新/清理均保留。\n"+
                "A小巷与原铺盖，B巷口，D主街，C林婶，G陈伯，F废弃物搜索，E旧壶只观察。\n"+
                "正常存档仍在AppData/LocalLow/QigaiPrototype/Reborn1985/Saves；兼容M5 schema4与M4已知升级，保留.bak。\n"+
                "Verification只供显式开发测试，不是玩家存档。仍为灰盒；不进入P1，不制作最终美术或擅写家书终稿。关闭窗口退出，操作/验收见docs/p0-usability。\n");
            File.WriteAllText("Logs/P0UsabilityBuildResult.json",JsonUtility.ToJson(new BuildInfo{bytes=(long)report.summary.totalSize},true));Debug.Log("P0_USABILITY_BUILD_OK: Windows x64, "+report.summary.totalSize+" bytes");
        }
        [Serializable]private sealed class BuildInfo{public string unityVersion=Application.unityVersion;public string result="Succeeded",target="Windows x64 Mono Development",version="0.5.1";public long bytes;}
    }
}
