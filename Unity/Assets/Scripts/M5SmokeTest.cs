using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Reborn1985.M1;
using Reborn1985.M2;
using Reborn1985.M3;
using Reborn1985.M4;
using UnityEngine;

namespace Reborn1985.M5
{
    public sealed class M5SmokeTest : MonoBehaviour
    {
        [Serializable]private sealed class Check
        {
            public string name; public bool passed,helpClaimed; public int day,grams,ground,cleared,sold,letters,claimed; public long cashCents;
        }
        [Serializable]private sealed class Result
        {
            public string phase,unityVersion,platform,graphicsDevice,normalSavePath; public bool passed;
            public Check[] checks; public string[] errors,screenshots; public M4SaveData finalState;
            public string inputMethod="Actual WindowsPlayer drives original Rigidbody2D, E edge, gameplay handlers and uGUI callbacks; no physical keyboard/mouse manual test.";
            public string captureMethod="Live camera and camera-space uGUI; no mock data, AI assets or compositing.";
        }
        private M2Scavenging game; private M4Session life; private M4Hud hud; private M2Hud bag; private M3Trade trade; private M3Hud merchant;
        private string output,phase,normalSavePath; private bool stopped;
        private readonly List<Check> checks=new List<Check>(); private readonly List<string> errors=new List<string>(),screenshots=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartWhenRequested()
        {if(Debug.isDebugBuild&&Array.IndexOf(Environment.GetCommandLineArgs(),"-m5-smoke")>=0)new GameObject("M5 Development Verification").AddComponent<M5SmokeTest>();}
        private static string Arg(string key,string fallback)
        {string[] args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
        private IEnumerator Start()
        {
            output=Arg("-m5Output",Path.Combine(Application.temporaryCachePath,"M5Smoke"));phase=Arg("-m5Phase","write");Directory.CreateDirectory(output);
            Application.logMessageReceived+=RecordError;Application.runInBackground=true;Application.targetFrameRate=60;
            game=FindFirstObjectByType<M2Scavenging>();life=FindFirstObjectByType<M4Session>();hud=FindFirstObjectByType<M4Hud>();bag=FindFirstObjectByType<M2Hud>();trade=FindFirstObjectByType<M3Trade>();merchant=FindFirstObjectByType<M3Hud>();
            yield return new WaitForSeconds(.5f);normalSavePath=life.Store.Path;life.SetVerificationSaveDirectory(Arg("-m5SaveDir",Path.Combine(output,"Saves")));Time.timeScale=3;
            IEnumerator routine=phase=="read"?ReadChecks():phase=="migrate"?MigrationChecks():WriteChecks();
            while(true)
            {
                bool more;try{more=routine.MoveNext();}catch(Exception ex){errors.Add(ex.ToString());break;}if(!more)break;
                yield return routine.Current;if(stopped)break;
            }
            game.Player.SetVerificationInput(null);game.SetVerificationInteraction(null);Time.timeScale=1;
            var result=new Result {phase=phase,unityVersion=Application.unityVersion,platform=Application.platform.ToString(),graphicsDevice=SystemInfo.graphicsDeviceType.ToString(),normalSavePath=normalSavePath,
                passed=checks.Count>3&&errors.Count==0&&checks.All(check=>check.passed),checks=checks.ToArray(),errors=errors.ToArray(),screenshots=screenshots.ToArray(),finalState=life.Capture()};
            File.WriteAllText(Path.Combine(output,phase+"-result.json"),JsonUtility.ToJson(result,true));Debug.Log("M5_"+phase.ToUpperInvariant()+"_"+(result.passed?"PASS":"FAIL")+": "+checks.Count+" checks, "+errors.Count+" errors");
            Application.logMessageReceived-=RecordError;Application.Quit(result.passed?0:2);
        }
        private M2Pickup Source(string id)=>game.Targets.Single(target=>!target.IsDropped&&target.Id==id);
        private IEnumerator WriteChecks()
        {
            Require(game.State.Life.dayIndex==1&&game.State.CashCents==0&&game.State.Count(ItemKind.Letter)==1,"Fresh day1 starts without cash and with one protected letter");
            yield return Walk(life.InteractionPosition("E-01"));yield return PressE();Require(game.State.Life.kettleObserved,"O-01 visit old kettle first without linear gate");life.Close();
            yield return Walk(game.World.Center(new Vector2Int(49,11)));yield return PressE();Require(game.State.Life.residentTalked&&game.State.CashCents==0,"Q-01 resident only records clue, no trade or money");life.Close();
            yield return Walk(game.World.RegionEntry("G"));yield return PressE();
            Require(trade.IsOpen&&trade.Pending.Status==SaleResult.Empty&&trade.Confirm()==SaleResult.Empty&&game.State.CashCents==0,"O-01 empty-handed Chenbo interaction is safe before scavenging");trade.Close();
            Require(!game.State.Life.letterRead,"M-01 player crosses A-G without forced letter reading");
            yield return Walk(life.InteractionPosition("C-01"));yield return PressE();
            Require(!life.Confirm()&&game.State.CashCents==0,"L-01 no-money breakfast fails without debit or lock");hud.HelpButton.onClick.Invoke();life.Close();
            yield return Walk(life.InteractionPosition("C-04"));yield return PressE();hud.PrimaryButton.onClick.Invoke();
            Require(game.State.Life.helpMealClaimed&&game.State.CashCents==0,"L-02 use only helper meal before economic failure");life.Close();
            foreach(string id in M5DailyRules.SourceIds)
            {
                yield return Walk(Source(id).transform.position);
                Require(game.TryInteract(Source(id))==InteractionResult.Success,"Exhaust approved source "+id);
                Require(game.TryInteract(Source(id))==InteractionResult.Unavailable,"Same-day repeated source rejects "+id);
                yield return Walk(game.World.RegionEntry("D"));
                Require(game.TryDrop(Source(id).Item)==InteractionResult.Success,"Discard finite unit away from source "+id);
            }
            Require(game.State.GroundCount==7&&game.State.TotalGrams==0&&game.State.CashCents==0&&M5DailyRules.SourceIds.All(game.State.Claimed),"R-01 zero cash, used helper, seven exhausted sources and seven discarded units");
            Require(game.Targets.Where(target=>target.IsDropped).All(target=>target.GetComponent<Collider2D>()==null),"Discarded units never add blocking colliders");
            Require(life.SaveNow()&&life.LoadNow()&&game.State.GroundCount==7&&M5DailyRules.SourceIds.All(game.State.Claimed),"Same-day disk reload cannot reset seven claims or duplicate ground");
            bag.SetBagOpen(true);yield return Capture("01-exhausted-zero-cash.png");bag.SetBagOpen(false);
            yield return Walk(life.InteractionPosition("A-01"));yield return PressE();hud.PrimaryButton.onClick.Invoke();
            Require(game.State.Life.dayIndex==2&&game.State.GroundCount==0&&game.State.ClearedCount==7&&game.State.CashCents==0,"R-01 R-02 free rest to day2 cleans seven drops without charging cash");
            Require(M5DailyRules.SourceIds.All(id=>game.Available(Source(id)))&&game.Targets.All(target=>!target.IsDropped),"R-03 exactly seven original sources available, no old drop visuals");
            Require(game.State.Life.helpMealClaimed&&game.State.Count(ItemKind.Letter)==1&&game.State.Life.kettleObserved,"Cross-day retains one-time reward, protected letter and clue flags");
            for(int i=0;i<20;i++)life.Confirm();Require(game.State.Life.dayIndex==2,"Holding/replaying rest confirmation cannot skip more days");
            yield return Capture("02-free-rest-cleans-and-refreshes.png");life.Close();
            yield return Walk(game.World.RegionEntry("D"));Require(life.SaveNow()&&life.SaveNow(),"Save and backup after cleanup before recovery scavenging");
            File.WriteAllText(Path.Combine(output,"expected.json"),JsonUtility.ToJson(life.Capture(),true));
            yield return Capture("03-day2-refreshed-street.png");
            Require(life.Validate(life.Capture()),"Writer exits with valid day2 zero-cash recovery checkpoint");
        }
        private IEnumerator ReadChecks()
        {
            Require(game.State.Life.dayIndex==1&&game.State.CashCents==0&&!game.State.Life.helpMealClaimed,"Independent reader process begins fresh before explicit load");
            var expected=JsonUtility.FromJson<M4SaveData>(File.ReadAllText(Path.Combine(output,"expected.json")));
            life.OpenLoad();hud.PrimaryButton.onClick.Invoke();
            Require(game.State.Life.dayIndex==2&&game.State.ClearedCount==7&&game.State.GroundCount==0&&game.State.Life.helpMealClaimed,"S-02 cold load retains cleanup and helper flag, without reviving old items");
            Require(Vector2.Distance(game.Player.Body.position,expected.position)<.001f&&Vector2.Dot(game.Player.Facing,expected.facing)>.999f,"S-01 cold load restores exact saved position and facing");
            Require(M5DailyRules.SourceIds.All(id=>game.Available(Source(id)))&&game.State.Life.kettleObserved&&game.State.Life.residentTalked,"S-03 current-day source availability and optional clues restored");
            for(int i=0;i<3;i++)Require(life.LoadNow()&&game.State.GroundCount==0&&game.State.ClearedCount==7,"Repeated post-cleanup load cannot restore expired ground "+i);
            foreach(string id in new[]{"B-02","D-01","D-02","D-03"}){yield return Walk(Source(id).transform.position);yield return PressE();}
            Require(game.State.TotalGrams==3000&&game.State.Count(ItemKind.Paper)==2&&game.State.Count(ItemKind.Bottle)==2,"R-01 next-day four sources restore basic3kg route");
            Require(game.State.Snapshot().inventory.Where(unit=>unit.kind!=ItemKind.Letter).All(unit=>unit.spawnedDay==2&&unit.unitId.EndsWith("@2",StringComparison.Ordinal)),"Fresh units have day2 identities, not cleared first-day copies");
            yield return Walk(Source("F-03").transform.position);yield return PressE();
            Require(game.State.TotalGrams==3000&&!game.State.Claimed("F-03")&&game.Available(Source("F-03")),"I-02 refreshed heavy metal still rejects overweight6kg");
            bag.SetBagOpen(true);yield return Capture("04-recovered-3kg-backpack.png");bag.SetBagOpen(false);
            yield return Walk(game.World.RegionEntry("G"));yield return PressE();merchant.ConfirmButton.onClick.Invoke();
            Require(game.State.CashCents==44&&game.State.SoldCount==4&&game.State.TotalGrams==0,"E-01 next-day recovery sells3kg for44 cents");trade.Close();
            yield return Walk(life.InteractionPosition("C-01"));yield return PressE();hud.PrimaryButton.onClick.Invoke();
            Require(game.State.CashCents==18&&game.State.Life.purchasedMeals==1&&game.State.Life.helpMealClaimed,"R-01 complete failure recovery buys26c meal and leaves18c without resetting help");
            yield return Capture("05-recovery-breakfast-18c.png");life.Close();
            yield return Walk(life.InteractionPosition("C-04"));yield return PressE();
            Require(!life.Confirm()&&game.State.Life.helpMealClaimed&&game.State.CashCents==18,"L-02 helper remains unavailable after overnight and cold load");life.Close();
            yield return Walk(Source("F-01").transform.position);yield return PressE();
            Require(game.State.Count(ItemKind.Paper)==1&&game.TryDrop(ItemKind.Paper)==InteractionResult.Success,"I-03 drop current-day searched paper after recovery");
            Require(life.SaveNow(),"S-02 save current-day dropped paper");
            M2Pickup drop=game.Targets.Single(target=>target.IsDropped);Require(game.TryInteract(drop)==InteractionResult.Success&&game.State.GroundCount==0,"I-03 current-day dropped paper can be re-picked");
            Require(life.SaveNow()&&life.LoadNow()&&game.State.Count(ItemKind.Paper)==1&&game.State.GroundCount==0&&!game.Available(Source("F-01")),"S-02 save after re-pick keeps bag only and source claimed");
            yield return Walk(life.InteractionPosition("A-01"));yield return PressE();hud.PrimaryButton.onClick.Invoke();life.Close();
            Require(game.State.Life.dayIndex==3&&game.State.Count(ItemKind.Paper)==1&&game.State.TotalGrams==1000&&game.State.GroundCount==0,"R-02 next overnight keeps carried paper without new ground");
            yield return Walk(Source("F-01").transform.position);yield return PressE();
            Require(game.State.Count(ItemKind.Paper)==2&&game.State.TotalGrams==2000,"Held yesterday paper and fresh today paper coexist");
            Require(game.State.Snapshot().inventory.Where(unit=>unit.kind==ItemKind.Paper).Select(unit=>unit.unitId).Distinct().Count()==2,"Same source across days owns two distinct physical IDs");
            for(int i=0;i<20;i++)game.TryInteract(Source("F-01"));Require(game.State.Count(ItemKind.Paper)==2,"R-03 same-day search spam cannot create third paper");
            bag.SetBagOpen(true);yield return Capture("06-held-and-fresh-papers.png");bag.SetBagOpen(false);
            Require(game.TryDrop(ItemKind.Paper)==InteractionResult.Success&&game.State.Snapshot().groundDrops.Single().unit.spawnedDay==2,"Drop old carried paper today with its original birth identity");
            Require(life.SaveNow()&&life.LoadNow()&&game.State.GroundCount==1&&game.State.Count(ItemKind.Paper)==1,"Old carried item dropped today remains re-pickable until next rest");
            yield return Walk(life.InteractionPosition("A-01"));yield return PressE();hud.PrimaryButton.onClick.Invoke();life.Close();
            Require(game.State.Life.dayIndex==4&&game.State.ClearedCount==8&&game.State.GroundCount==0&&game.State.Count(ItemKind.Paper)==1,"Next rest clears only discarded old paper and keeps day3 carried paper");
            Require(life.SaveNow()&&life.LoadNow()&&game.State.ClearedCount==8&&game.State.GroundCount==0,"S-02 post-cleanup save does not resurrect any of eight expired units");
            Require(game.State.Count(ItemKind.Letter)==1&&game.TryDrop(ItemKind.Letter)==InteractionResult.Protected&&game.State.Life.helpMealClaimed,"I-05 protected letter and lifetime helper flag survive all days");
            yield return Walk(game.World.RegionEntry("G"));yield return PressE();merchant.ConfirmButton.onClick.Invoke();trade.Close();
            Require(game.State.CashCents==30&&game.State.SoldCount==5,"Remaining legitimate day3 paper sells once for12c, total30c");
            string[] route={"A","B","D","C","D","G","D","F","D","E","D","B","A"};
            foreach(string region in route){yield return Walk(game.World.RegionEntry(region));Require(Vector2.Distance(game.Player.Body.position,game.World.RegionEntry(region))<.08f,"M1 regression route "+region);}
            Require(life.SaveNow()&&life.Validate(life.Capture())&&game.State.CashCents==30&&game.State.Life.dayIndex==4,"Full recovery and regression finish with valid conserved day4 state");
            life.OpenJournal();yield return Capture("07-day4-regression-complete.png");life.Close();
        }
        private IEnumerator MigrationChecks()
        {
            string fixture=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Verification","M4Schema3.json");
            Directory.CreateDirectory(Path.GetDirectoryName(life.Store.Path));File.Copy(fixture,life.Store.Path,true);
            Require(life.LoadNow()&&game.State.Snapshot().schemaVersion==4,"Real M4 schema3 file loads into M5 after checksum and migration");
            Require(game.State.CashCents==18&&game.State.Life.dayIndex==2&&game.State.Life.helpMealClaimed&&game.State.SoldCount==4,"M4 migration retains exact18c/day2/helper and four sold units");
            Require(game.State.GroundCount==0&&game.State.ClearedCount==1&&M5DailyRules.SourceIds.All(id=>game.Available(Source(id))),"M4 expired day1 ground clears and day2 source slots become available");
            Require(life.SaveNow()&&File.ReadAllText(life.Store.Path+".bak")==File.ReadAllText(fixture),"First M5 save keeps original M4 file as recoverable backup");
            File.WriteAllText(life.Store.Path,"broken");Require(life.LoadNow()&&game.State.ClearedCount==1&&game.State.CashCents==18,"Damaged M5 primary falls back to M4 backup and migrates without duplication");
            yield return Walk(life.InteractionPosition("C-04"));yield return PressE();Require(!life.Confirm()&&game.State.Life.helpMealClaimed,"Migrated M4 helper cannot be rewarded again");life.Close();
            yield return Walk(Source("B-02").transform.position);yield return PressE();
            Require(game.State.Count(ItemKind.Paper)==1&&game.State.Snapshot().inventory.Any(unit=>unit.unitId=="B-02@2"),"Migrated day1 sold source yields a distinct legal day2 unit");
            Require(life.SaveNow()&&life.LoadNow()&&game.State.Count(ItemKind.Paper)==1&&game.State.ClearedCount==1,"Migrated and resaved progress remains valid on current schema");
            bag.SetBagOpen(true);yield return Capture("08-m4-save-upgraded.png");bag.SetBagOpen(false);
        }
        private IEnumerator PressE(){game.SetVerificationInteraction(false);yield return null;game.SetVerificationInteraction(true);yield return null;yield return null;game.SetVerificationInteraction(false);yield return null;}
        private IEnumerator Walk(Vector2 goal)
        {
            List<Vector2> path=game.World.VerificationPath(game.Player.Body.position,goal);if(path==null){errors.Add("No clear route to "+goal);stopped=true;yield break;}
            foreach(Vector2 waypoint in path)
            {
                int steps=0;while(Vector2.Distance(game.Player.Body.position,waypoint)>.045f)
                {Vector2 delta=waypoint-game.Player.Body.position;game.Player.SetVerificationInput(delta.normalized*Mathf.Clamp01(delta.magnitude/(game.World.Settings.MovementSpeed*Time.fixedDeltaTime)));yield return new WaitForFixedUpdate();if(++steps>180){errors.Add("Stuck at "+waypoint);stopped=true;break;}}
                if(stopped)break;
            }
            game.Player.SetVerificationInput(Vector2.zero);yield return null;
        }
        private void Require(bool passed,string name)
        {
            checks.Add(new Check {name=name,passed=passed,helpClaimed=game.State.Life.helpMealClaimed,day=game.State.Life.dayIndex,grams=game.State.TotalGrams,ground=game.State.GroundCount,cleared=game.State.ClearedCount,
                sold=game.State.SoldCount,letters=game.State.Count(ItemKind.Letter),claimed=game.State.Snapshot().claimedSourceIds.Length,cashCents=game.State.CashCents});
            if(!passed)errors.Add(name);Debug.Log("M5_CHECK_"+(passed?"PASS ":"FAIL ")+name);
        }
        private IEnumerator Capture(string name)
        {
            yield return null;Camera.main.GetComponent<CameraFollow>().Snap();Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();
            Camera camera=Camera.main;RenderTexture original=camera.targetTexture,active=RenderTexture.active,target=RenderTexture.GetTemporary(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32);var texture=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
            try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);texture.Apply();var colors=new HashSet<Color32>();Color32[] pixels=texture.GetPixels32();for(int i=0;i<pixels.Length;i+=997)colors.Add(pixels[i]);if(colors.Count<8)errors.Add("Blank capture "+name);File.WriteAllBytes(Path.Combine(output,name),texture.EncodeToPNG());screenshots.Add(name);}
            finally{camera.targetTexture=original;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);Destroy(texture);}
        }
        private void RecordError(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
    }
}
