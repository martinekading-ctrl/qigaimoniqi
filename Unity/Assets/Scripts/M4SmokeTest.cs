using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Reborn1985.M1;
using Reborn1985.M2;
using Reborn1985.M3;
using UnityEngine;

namespace Reborn1985.M4
{
    public sealed class M4SmokeTest : MonoBehaviour
    {
        [Serializable]private sealed class Check
        {
            public string name;public bool passed;public long cashCents;public int grams,day,paidMeals,ground,letters;
            public bool helpClaimed,letterRead,kettleObserved;
        }
        [Serializable]private sealed class Result
        {
            public string phase,unityVersion,platform,graphicsDevice,normalSavePath;public bool passed;
            public Check[] checks;public string[] errors,screenshots;public M4SaveData finalState;
            public string inputMethod="Actual WindowsPlayer drives same Rigidbody2D, E edge, life/UI and save handlers. Physical keyboard/mouse not manually operated.";
            public string captureMethod="Live camera plus camera-space uGUI, no mock scenes or compositing.";
        }
        private M4Session life;private M4Hud hud;private M2Scavenging game;private M2Hud bag;private M3Trade trade;private M3Hud tradeHud;
        private string output,phase,normalSavePath;private bool stopped;
        private readonly List<Check> checks=new List<Check>();private readonly List<string> errors=new List<string>(),screenshots=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartWhenRequested()
        {if(Debug.isDebugBuild&&Array.IndexOf(Environment.GetCommandLineArgs(),"-m4-smoke")>=0)new GameObject("M4 Development Verification").AddComponent<M4SmokeTest>();}
        private static string Arg(string key,string fallback)
        {string[] args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
        private IEnumerator Start()
        {
            output=Arg("-m4Output",Path.Combine(Application.temporaryCachePath,"M4Smoke"));phase=Arg("-m4Phase","write");Directory.CreateDirectory(output);
            Application.logMessageReceived+=RecordError;Application.runInBackground=true;Application.targetFrameRate=60;
            life=FindFirstObjectByType<M4Session>();hud=FindFirstObjectByType<M4Hud>();game=FindFirstObjectByType<M2Scavenging>();bag=FindFirstObjectByType<M2Hud>();trade=FindFirstObjectByType<M3Trade>();tradeHud=FindFirstObjectByType<M3Hud>();
            yield return new WaitForSeconds(.5f);normalSavePath=life.Store.Path;life.SetVerificationSaveDirectory(Arg("-m4SaveDir",Path.Combine(output,"Saves")));Time.timeScale=3;
            IEnumerator routine=phase=="read"?ReadChecks():WriteChecks();
            while(true)
            {
                bool more;try{more=routine.MoveNext();}catch(Exception ex){errors.Add(ex.ToString());break;}if(!more)break;
                yield return routine.Current;if(stopped)break;
            }
            game.Player.SetVerificationInput(null);game.SetVerificationInteraction(null);Time.timeScale=1;
            var result=new Result{phase=phase,unityVersion=Application.unityVersion,platform=Application.platform.ToString(),graphicsDevice=SystemInfo.graphicsDeviceType.ToString(),normalSavePath=normalSavePath,
                passed=errors.Count==0&&checks.Count>10&&checks.All(check=>check.passed),checks=checks.ToArray(),errors=errors.ToArray(),screenshots=screenshots.ToArray(),finalState=life.Capture()};
            File.WriteAllText(Path.Combine(output,phase+"-result.json"),JsonUtility.ToJson(result,true));Debug.Log("M4_"+phase.ToUpperInvariant()+"_"+(result.passed?"PASS":"FAIL")+": "+checks.Count+" checks, "+errors.Count+" errors");
            Application.logMessageReceived-=RecordError;Application.Quit(result.passed?0:2);
        }
        private M2Pickup Source(string id)=>game.Targets.Single(target=>!target.IsDropped&&target.Id==id);
        private IEnumerator WriteChecks()
        {
            Require(game.State.CashCents==0&&game.State.Count(ItemKind.Letter)==1&&game.State.Life.dayIndex==1,"Fresh opening: one protected letter, zero cash, first day");
            Require(!game.State.Life.letterRead&&game.State.Life.openingSeen,"Non-blocking opening shown once, reading not forced");
            yield return Walk(life.InteractionPosition("C-01"));yield return PressE();
            Require(life.View==LifeView.Breakfast,"Empty-handed player freely reaches Linshen before scavenging or reading");
            Require(!life.Confirm()&&game.State.CashCents==0&&game.State.Life.purchasedMeals==0,"L-01 insufficient cash changes no meal/cash state");
            yield return Capture("01-no-money-breakfast.png");hud.HelpButton.onClick.Invoke();
            Require(game.State.Life.helpOffered&&!game.State.Life.helpMealClaimed,"Accept one helper task via actual UI");
            life.Close();Require(life.SaveNow(),"L-03 save before completing helper task");
            yield return Walk(life.InteractionPosition("C-04"));yield return PressE();hud.PrimaryButton.onClick.Invoke();
            Require(game.State.Life.helpCompleted&&game.State.Life.helpMealClaimed&&game.State.CashCents==0,"L-02 completing C-04 gives one meal, no cash");
            for(int i=0;i<20;i++)life.Confirm();Require(game.State.Life.helpMealClaimed&&game.State.Life.purchasedMeals==0,"20 confirmation repeats cannot issue additional helper meals");
            Require(life.SaveNow(),"L-03 save after helper reward");string beforeHelp=File.ReadAllText(life.Store.Path+".bak");
            File.WriteAllText(life.Store.Path,beforeHelp);Require(life.LoadNow()&&game.State.Life.helpOffered&&!game.State.Life.helpMealClaimed,"L-03 actual pre-reward file restores unclaimed state consistently");
            yield return Walk(life.InteractionPosition("C-04"));yield return PressE();hud.PrimaryButton.onClick.Invoke();
            Require(game.State.Life.helpMealClaimed&&game.State.CashCents==0,"Restored pre-reward state can complete exactly one helper meal");
            Require(life.SaveNow()&&life.LoadNow()&&!game.State.CompleteHelp(),"L-03 post-reward file reload cannot redeem again");
            life.OpenPoint("C-04");yield return Capture("02-help-claimed.png");life.Close();
            foreach(string id in new[]{"B-02","D-01","D-02","D-03"}){yield return Walk(Source(id).transform.position);yield return PressE();}
            Require(game.State.TotalGrams==3000&&game.State.Count(ItemKind.Paper)==2&&game.State.Count(ItemKind.Bottle)==2,"M2 base route remains 3000g");
            Require(life.SaveNow(),"Save real 3kg backpack before any sale");
            Require(game.TryDrop(ItemKind.Paper)==InteractionResult.Success&&game.State.TotalGrams==2000,"Change saved backpack by dropping one paper");
            Require(life.LoadNow()&&game.State.TotalGrams==3000&&game.State.GroundCount==0&&game.State.Count(ItemKind.Paper)==2,"Pre-sale load restores all 3kg and removes later ground copy");
            yield return Walk(game.World.RegionEntry("G"));yield return PressE();tradeHud.ConfirmButton.onClick.Invoke();
            Require(game.State.CashCents==44&&game.State.TotalGrams==0,"M3 first sale remains44 cents");trade.Close();
            Require(life.SaveNow()&&life.LoadNow()&&game.State.CashCents==44&&game.State.SoldCount==4&&!trade.IsOpen,"Post-sale disk restore keeps44 cents, four sold identities and no pending quote");
            yield return Walk(life.InteractionPosition("C-01"));yield return PressE();life.Close();
            Require(game.State.CashCents==44&&game.State.Life.purchasedMeals==0,"Cancel breakfast view does not debit or consume food");
            yield return PressE();hud.PrimaryButton.onClick.Invoke();
            Require(game.State.CashCents==18&&game.State.Life.purchasedMeals==1&&game.State.Life.mealExpenseCents==26,"L-01 buy26c breakfast after44c income leaves18c");
            for(int i=0;i<20;i++)hud.PrimaryButton.onClick.Invoke();Require(game.State.CashCents==18&&game.State.Life.purchasedMeals==1,"Repeated purchase callback cannot duplicate current meal or debit");
            yield return Capture("03-breakfast-18c.png");life.Close();
            Require(life.SaveNow()&&life.LoadNow()&&game.State.CashCents==18&&game.State.Life.purchasedMeals==1,"Post-breakfast disk restore keeps18 cents and exactly one paid meal");
            bag.SetBagOpen(true);life.OpenLetter();Require(game.State.Life.letterRead&&game.State.Count(ItemKind.Letter)==1&&game.State.TotalGrams==0,"Optional letter reading retains protected item and zero weight");
            yield return Capture("04-letter-summary.png");life.Close();
            foreach(string id in new[]{"A-02","A-03","B-01","E-01"}){yield return Walk(life.InteractionPosition(id));yield return PressE();Require(life.IsOpen,"Optional environment opens "+id);life.Close();}
            yield return Walk(game.World.Center(new Vector2Int(49,11)));yield return PressE();
            Require(game.State.Life.residentTalked&&game.State.Life.kettleObserved&&game.State.Life.yearRead&&game.State.Life.noticeRead&&game.State.Life.puddleObserved,"All environment and old-kettle clues recorded without linear gates");
            life.Close();life.OpenJournal();yield return Capture("05-clues.png");life.Close();
            Require(game.State.Count(ItemKind.Letter)==1&&game.State.CashCents==18&&game.State.TotalGrams==0,"Clue interactions add neither goods nor cash");
            yield return Walk(Source("F-01").transform.position);yield return PressE();
            Require(game.TryDrop(ItemKind.Paper)==InteractionResult.Success&&game.State.GroundCount==1,"Drop real searched paper before saving");
            Require(life.SaveNow(),"Save while one ground item exists");
            M2Pickup drop=game.Targets.Single(target=>target.IsDropped);Require(game.TryInteract(drop)==InteractionResult.Success&&game.State.GroundCount==0,"Live state can re-pick the saved ground item");
            Require(life.LoadNow()&&game.State.GroundCount==1&&game.State.Count(ItemKind.Paper)==0,"Loading restores ground OR backpack ownership, never both");
            for(int i=0;i<3;i++)Require(life.LoadNow()&&game.Targets.Count(target=>target.IsDropped&&target.gameObject.activeSelf)==1,"Repeat load reconstructs exactly one ground visual "+i);
            yield return Walk(life.InteractionPosition("A-01"));yield return PressE();hud.PrimaryButton.onClick.Invoke();
            Require(game.State.Life.dayIndex==2&&game.State.CashCents==18&&game.State.Life.helpMealClaimed,"Free original-bedding rest advances day, preserves money and helper flag");
            for(int i=0;i<10;i++)life.Confirm();Require(game.State.Life.dayIndex==2,"Repeated rest confirmation does not skip extra days");
            Require(game.State.Claimed("F-01")&&game.State.GroundCount==1,"M4 rest does not pre-implement M5 refresh/cleanup");
            yield return Capture("06-free-rest.png");life.Close();
            yield return Walk(game.World.RegionEntry("E"));Require(life.SaveNow()&&life.SaveNow(),"Final saved state and backup contain same complete progress");
            File.WriteAllText(Path.Combine(output,"expected.json"),JsonUtility.ToJson(life.Capture(),true));yield return Capture("07-saved-day2.png");
            Require(life.Validate(life.Capture())&&game.State.CashCents==18&&game.State.GroundCount==1,"Writer exits with validated full state; reader must launch a new process");
        }
        private IEnumerator ReadChecks()
        {
            Require(game.State.CashCents==0&&game.State.Life.dayIndex==1&&!game.State.Life.helpMealClaimed,"Separate process starts fresh before explicit load");
            var expected=JsonUtility.FromJson<M4SaveData>(File.ReadAllText(Path.Combine(output,"expected.json")));
            Require(Vector2.Distance(game.Player.Body.position,expected.position)>1,"Saved position differs from new-run spawn");
            life.OpenLoad();hud.PrimaryButton.onClick.Invoke();
            Require(game.State.CashCents==18&&game.State.Life.dayIndex==2&&game.State.Life.purchasedMeals==1&&game.State.Life.helpMealClaimed,"Cold restart loads exact18c, day2, paid1 and helper1");
            Require(Vector2.Distance(game.Player.Body.position,expected.position)<.001f&&Vector2.Dot(game.Player.Facing,expected.facing)>.999f,"Position and facing restored across full quit/relaunch");
            Require(game.State.Count(ItemKind.Letter)==1&&game.State.Life.letterRead&&game.State.Life.kettleObserved&&game.State.Life.residentTalked&&game.State.Life.yearRead&&game.State.Life.noticeRead,"Letter and optional clue states survive cold restart");
            Require(game.State.SoldCount==4&&game.State.Snapshot().receipts.Length==1&&game.State.GroundCount==1&&game.State.Count(ItemKind.Paper)==0,"Sold records and ground/backpack exclusivity restored");
            Require(game.Targets.Count(target=>target.IsDropped&&game.Available(target))==1&&!game.Available(Source("B-02"))&&!game.Available(Source("F-01"))&&game.Available(Source("F-02")),"Claimed/unclaimed sources and exactly one ground object restored");
            yield return Capture("08-cold-restart-loaded.png");
            yield return Walk(life.InteractionPosition("C-04"));yield return PressE();Require(!life.Confirm()&&game.State.Life.helpMealClaimed&&game.State.CashCents==18,"Already-claimed help cannot issue a second meal after restart");life.Close();
            yield return Walk(Source("F-02").transform.position);yield return PressE();Require(game.State.Count(ItemKind.Bottle)==1&&game.State.TotalGrams==500,"Unclaimed search still produces exactly once after load");
            Require(life.LoadNow()&&game.State.Count(ItemKind.Bottle)==0&&!game.State.Claimed("F-02"),"Reload restores whole prior state, not extra claimed resources or mixed inventory");
            M2Pickup drop=game.Targets.Single(target=>target.IsDropped);yield return Walk(drop.transform.position);
            Require(game.TryInteract(drop)==InteractionResult.Success&&game.State.Count(ItemKind.Paper)==1&&game.State.GroundCount==0,"Restored ground item can be re-picked once");
            Require(!game.Available(Source("F-01")),"Re-picked item never resets original search claim");
            Require(life.LoadNow()&&life.SaveNow()&&life.SaveNow(),"Create valid primary and backup for filesystem fault tests");
            string good=File.ReadAllText(life.Store.Path);
            File.WriteAllText(life.Store.Path,"{truncated");Require(life.LoadNow()&&game.State.CashCents==18&&game.State.Life.helpMealClaimed,"Corrupt primary restores whole valid backup without reward duplication");
            string live=JsonUtility.ToJson(game.State.Snapshot());Vector2 position=game.Player.Body.position;
            File.WriteAllText(life.Store.Path+".bak","broken");Require(!life.LoadNow()&&JsonUtility.ToJson(game.State.Snapshot())==live&&Vector2.Distance(position,game.Player.Body.position)<.001f,"Both corrupt files leave current live state and pose untouched");
            File.WriteAllText(life.Store.Path,good);File.WriteAllText(life.Store.Path+".bak",good);File.WriteAllText(life.Store.Path+".tmp","partial");
            Require(life.LoadNow()&&game.State.GroundCount==1,"Incomplete temp write ignored; primary and ground state remain valid");
            File.WriteAllText(life.Store.Path,good.Replace("\"version\": 1","\"version\": 99"));
            Require(life.LoadNow()&&game.State.Life.helpMealClaimed,"Future primary envelope refused and previous valid backup used");
            File.WriteAllText(life.Store.Path,good);Require(life.SaveNow()&&life.Validate(life.Capture()),"Fault-test slot ends valid, without touching normal player saves");
            life.OpenJournal();yield return Capture("09-restored-clues.png");life.Close();
            Require(game.State.CashCents==18&&game.State.Count(ItemKind.Letter)==1&&game.State.GroundCount==1&&game.State.Life.dayIndex==2,"Cold-load and fault tests finish with conserved full state");
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
            var state=game.State.Life;checks.Add(new Check{name=name,passed=passed,cashCents=game.State.CashCents,grams=game.State.TotalGrams,day=state.dayIndex,paidMeals=state.purchasedMeals,ground=game.State.GroundCount,letters=game.State.Count(ItemKind.Letter),helpClaimed=state.helpMealClaimed,letterRead=state.letterRead,kettleObserved=state.kettleObserved});
            if(!passed)errors.Add(name);Debug.Log("M4_CHECK_"+(passed?"PASS ":"FAIL ")+name);
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
