using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Reborn1985.M1;
using Reborn1985.M2;
using Reborn1985.M4;
using UnityEngine;

namespace Reborn1985.P0
{
    public sealed class P0UsabilitySmokeTest : MonoBehaviour
    {
        [Serializable]private sealed class Check{public string name,saveStatus;public bool passed,dirty;public int grams,day;public long cashCents;}
        [Serializable]private sealed class Result
        {
            public string phase,unityVersion,platform,graphicsDevice;public bool passed;public Check[] checks;public string[] errors,screenshots;public M4SaveData finalState;
            public string inputMethod="Actual WindowsPlayer drives original Rigidbody2D, E edge, UI button callbacks and save handlers. No physical keyboard/mouse manual test.";
            public string captureMethod="Live camera plus camera-space uGUI; no compositing or mock results.";
        }
        private M4Session life;private M4Hud display;private M2Scavenging game;private M2Hud bag;private P0UsabilityHud ui;
        private string output,phase;private bool stopped;
        private readonly List<Check> checks=new List<Check>();private readonly List<string> errors=new List<string>(),screenshots=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartWhenRequested(){if(Debug.isDebugBuild&&Array.IndexOf(Environment.GetCommandLineArgs(),"-p0-usability-smoke")>=0)new GameObject("P0 Usability Verification").AddComponent<P0UsabilitySmokeTest>();}
        private static string Arg(string key,string fallback){string[] args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
        private IEnumerator Start()
        {
            output=Arg("-p0Output",Path.Combine(Application.temporaryCachePath,"P0Usability"));phase=Arg("-p0Phase","write");Directory.CreateDirectory(output);
            Application.logMessageReceived+=RecordError;Application.runInBackground=true;Application.targetFrameRate=60;
            life=FindFirstObjectByType<M4Session>();display=FindFirstObjectByType<M4Hud>();game=FindFirstObjectByType<M2Scavenging>();bag=FindFirstObjectByType<M2Hud>();
            yield return new WaitForSeconds(.5f);ui=display.Usability;life.SetVerificationSaveDirectory(Arg("-p0SaveDir",Path.Combine(output,"Saves")));Time.timeScale=3;
            IEnumerator routine=phase=="read"?ReadChecks():WriteChecks();
            while(true){bool more;try{more=routine.MoveNext();}catch(Exception ex){errors.Add(ex.ToString());break;}if(!more)break;yield return routine.Current;if(stopped)break;}
            game.Player.SetVerificationInput(null);game.SetVerificationInteraction(null);Time.timeScale=1;
            var result=new Result{phase=phase,unityVersion=Application.unityVersion,platform=Application.platform.ToString(),graphicsDevice=SystemInfo.graphicsDeviceType.ToString(),passed=checks.Count>5&&errors.Count==0&&checks.All(check=>check.passed),checks=checks.ToArray(),errors=errors.ToArray(),screenshots=screenshots.ToArray(),finalState=life.Capture()};
            File.WriteAllText(Path.Combine(output,phase+"-result.json"),JsonUtility.ToJson(result,true));Debug.Log("P0_USABILITY_"+phase.ToUpperInvariant()+"_"+(result.passed?"PASS":"FAIL")+": "+checks.Count+" checks, "+errors.Count+" errors");Application.logMessageReceived-=RecordError;Application.Quit(result.passed?0:2);
        }
        private IEnumerator WriteChecks()
        {
            Require(FindObjectsByType<TextMesh>(FindObjectsSortMode.None).Length==0,"Full P0 HUD has no permanent region/NPC TextMesh labels over actors");
            ui.Tick();Require(ui.HintVisible&&ui.HintText.Contains("免费休息"),"Spawn shows the single nearby original-bedding action");CheckHintGeometry("Spawn");
            Require(life.HasUnsavedChanges&&ui.SaveText.Contains("尚未手动保存"),"New run clearly says it has not been saved");
            string fresh=JsonUtility.ToJson(game.State.Snapshot());
            Require(!life.LoadNow()&&game.Feedback.Contains("先按F5")&&JsonUtility.ToJson(game.State.Snapshot())==fresh,"No-slot read gives friendly instruction and keeps progress");
            Require(life.SaveNow()&&!life.HasUnsavedChanges,"Successful manual save establishes a clean snapshot and pose");
            ui.Tick();Require(ui.SaveText.Contains("已保存")&&!ui.SaveText.Contains("未保存"),"Save status shows actual success rather than an assumed save");
            string beforeHelp=JsonUtility.ToJson(game.State.Snapshot());ui.ControlsButton.onClick.Invoke();ui.Tick();
            Require(ui.ControlsOpen&&!ui.HintVisible&&ui.HelpText.Contains("不必按顺序")&&ui.HelpText.Contains("免费休息"),"Optional controls explain free order and recovery without target badge overlap");
            Require(ui.ControlsTextFits,"All controls and recovery text fits the real rendered text rectangle");
            Require(JsonUtility.ToJson(game.State.Snapshot())==beforeHelp&&!life.HasUnsavedChanges,"Opening controls changes no gameplay data or save dirtiness");
            yield return Capture("01-optional-controls.png");bag.SetBagOpen(true);ui.Tick();
            Require(!ui.ControlsOpen&&bag.BagOpen&&!ui.HintVisible,"Opening backpack closes controls and suppresses the floating badge");
            bag.LetterReadButton.onClick.Invoke();
            Require(life.View==LifeView.Letter&&!bag.BagOpen&&game.State.Count(ItemKind.Letter)==1&&game.State.TotalGrams==0,"Backpack letter button opens reading and preserves protected item");
            Require(life.HasUnsavedChanges&&!life.ContextPrompt().Contains("Enter")&&life.ContextPrompt().Contains("Esc"),"Letter read is unsaved progress; read-only panel does not advertise confirmation");
            yield return Capture("02-letter-and-unsaved-status.png");life.Close();Require(life.SaveNow()&&!life.HasUnsavedChanges,"Save after optional reading clears its dirty status");
            ui.ControlsButton.onClick.Invoke();yield return Walk(game.World.Center(new Vector2Int(6,15)));
            Require(ui.ControlsOpen&&life.HasUnsavedChanges,"Controls card does not freeze movement and changed pose is unsaved");ui.HideControls();ui.Tick();
            Require(ui.HintVisible&&ui.HintText.Contains("废纸"),"First nearby paper uses the same eligible target as E interaction");CheckHintGeometry("Nearby paper");
            yield return Capture("03-nearby-paper.png");
            M2Pickup paper=game.Targets.Single(target=>!target.IsDropped&&target.Id=="B-02");
            var wall=new GameObject("P0 verifier temporary wall",typeof(BoxCollider2D));wall.layer=8;wall.transform.position=(game.Player.Body.position+(Vector2)paper.transform.position)*.5f;wall.GetComponent<BoxCollider2D>().size=new Vector2(.18f,.18f);Physics2D.SyncTransforms();ui.Tick();
            Require(!game.InReach(paper.transform.position)&&(!ui.HintVisible||!ui.HintText.Contains("废纸")),"Floating hint does not offer paper through a solid wall");
            Destroy(wall);yield return null;Physics2D.SyncTransforms();yield return PressE();
            Require(game.State.Count(ItemKind.Paper)==1&&game.State.TotalGrams==1000,"Normal E still collects the first legal paper after temporary wall removal");
            bag.SetBagOpen(true);ui.Tick();Require(!ui.HintVisible,"Backpack suppresses floating badge without changing item interactions");bag.SetBagOpen(false);
            Require(life.SaveNow()&&!life.HasUnsavedChanges,"Collecting and saving retain exact1kg and clean state");
            yield return Walk(game.World.RegionEntry("D"));ui.Tick();Require(!ui.HintVisible&&life.HasUnsavedChanges,"Far from every legal point no floating action is invented; movement is dirty");
            yield return Capture("04-unsaved-movement.png");
            yield return Walk(life.InteractionPosition("C-01"));yield return PressE();ui.Tick();Require(!ui.HintVisible,"Life interaction suppresses floating badge");
            display.HelpButton.onClick.Invoke();life.Close();yield return Walk(life.InteractionPosition("C-04"));yield return PressE();display.PrimaryButton.onClick.Invoke();life.Close();
            Require(game.State.Life.helpMealClaimed&&game.State.CashCents==0,"Original once-only helper path is retained");
            yield return Walk(life.InteractionPosition("C-01"));yield return PressE();
            Require(!life.ContextPrompt().Contains("询问帮工")&&!life.Confirm()&&game.Feedback.Contains("免费休息")&&!game.Feedback.Contains("询问一次性帮工"),"After helper used, prompt and insufficient-money advice explain only available actions and rest");
            yield return Capture("05-used-help-recovery-advice.png");life.Close();
            Require(life.SaveNow()&&life.SaveNow(),"Save current position, paper, letter and helper to primary and backup");string good=File.ReadAllText(life.Store.Path);
            File.WriteAllText(life.Store.Path,"broken");Require(life.LoadNow()&&!life.HasUnsavedChanges&&life.SaveStatusText.Contains("备份"),"Actual backup recovery keeps persistent save guidance");
            yield return Capture("06-backup-status.png");string live=JsonUtility.ToJson(game.State.Snapshot());
            File.WriteAllText(life.Store.Path+".bak","broken");Require(!life.LoadNow()&&life.SaveStatusText.Contains("读取失败")&&JsonUtility.ToJson(game.State.Snapshot())==live,"Double-corrupt load reports failure without pretending a fresh save or changing live progress");
            File.WriteAllText(life.Store.Path,good);File.WriteAllText(life.Store.Path+".bak",good);Require(life.SaveNow()&&!life.HasUnsavedChanges&&life.SaveStatusText.Contains("已保存"),"New successful save clears previous read error and backup hint");
            ui.ControlsButton.onClick.Invoke();life.OpenJournal();ui.Tick();Require(!ui.ControlsOpen&&life.IsOpen&&!ui.HintVisible,"Opening a life view closes the optional controls card without menu stacking");life.Close();
            Require(!life.HasUnsavedChanges,"Read-only journal view does not mark gameplay as changed");
            File.WriteAllText(Path.Combine(output,"expected.json"),JsonUtility.ToJson(life.Capture(),true));Require(life.SaveNow()&&life.Validate(life.Capture()),"Writer exits with valid original schema4 progress");
        }
        private IEnumerator ReadChecks()
        {
            Require(game.State.Count(ItemKind.Paper)==0&&ui.SaveText.Contains("尚未手动保存"),"New process does not auto-load or claim its fresh run was saved");
            var expected=JsonUtility.FromJson<M4SaveData>(File.ReadAllText(Path.Combine(output,"expected.json")));
            ui.ControlsButton.onClick.Invoke();life.OpenLoad();display.PrimaryButton.onClick.Invoke();ui.Tick();
            Require(!ui.ControlsOpen&&game.State.Count(ItemKind.Paper)==1&&game.State.TotalGrams==1000&&game.State.Life.helpMealClaimed&&game.State.Life.letterRead,"Cold read retains letter/paper/helper and closes temporary controls");
            Require(Vector2.Distance(game.Player.Body.position,expected.position)<.001f&&Vector2.Dot(game.Player.Facing,expected.facing)>.999f,"Cold read restores exact saved pose");
            Require(!life.HasUnsavedChanges&&ui.SaveText.Contains("已读取存档"),"Cold read status reports actual loaded state and is clean");
            yield return Capture("07-cold-read-status.png");
            string fixture=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Verification","M5Schema4.json");File.Copy(fixture,life.Store.Path,true);
            Require(life.LoadNow()&&game.State.Life.dayIndex==4&&game.State.CashCents==30&&game.State.ClearedCount==8&&game.State.SoldCount==5,"Actual previous M5 save loads with its day4/30c/8cleared/5sold records unchanged");
            Require(game.State.Snapshot().schemaVersion==4&&game.State.Life.helpMealClaimed&&game.State.Count(ItemKind.Letter)==1&&!life.HasUnsavedChanges,"UI-only update preserves M5 schema and once-only reward state");
            // Measure the rendered frame after the camera-space canvas follows the loaded pose.
            yield return Capture("08-m5-save-compatible.png");CheckHintGeometry("Loaded M5 spawn");
            string before=JsonUtility.ToJson(game.State.Snapshot());ui.ControlsButton.onClick.Invoke();ui.HideControls();Require(JsonUtility.ToJson(game.State.Snapshot())==before&&!life.HasUnsavedChanges,"Optional guidance remains presentation only on a loaded M5 run");
            Require(life.SaveNow()&&life.LoadNow()&&game.State.CashCents==30&&game.State.ClearedCount==8,"Re-saving compatible data retains its economic and cleanup ledger");
            yield return Walk(life.InteractionPosition("B-01"));yield return PressE();Require(life.View==LifeView.Notice&&!life.ContextPrompt().Contains("Enter"),"Optional notice remains readable with truthful close-only prompt");life.Close();
            Require(life.SaveNow()&&life.Validate(life.Capture()),"Reader completes with valid existing gameplay state");
        }
        private void CheckHintGeometry(string context)
        {
            if(!ui.HintVisible){Require(false,context+" has a visible nearby badge");return;}
            var corners=new Vector3[4];ui.HintRect.GetWorldCorners(corners);Vector3 min=Camera.main.WorldToScreenPoint(corners[0]),max=Camera.main.WorldToScreenPoint(corners[2]);
            Rect hintRect=Rect.MinMaxRect(min.x,min.y,max.x,max.y);
            Debug.Log("P0_HINT_GEOMETRY "+context+": "+hintRect+" / "+Screen.width+"x"+Screen.height);
            Require(hintRect.xMin>=0&&hintRect.yMin>=0&&hintRect.xMax<=Screen.width&&hintRect.yMax<=Screen.height,context+" badge lies inside actual window bounds");
            Bounds body=game.Player.GetComponentsInChildren<SpriteRenderer>().First().bounds;foreach(SpriteRenderer renderer in game.Player.GetComponentsInChildren<SpriteRenderer>())body.Encapsulate(renderer.bounds);
            Vector3 bodyMin=Camera.main.WorldToScreenPoint(body.min),bodyMax=Camera.main.WorldToScreenPoint(body.max);
            Require(!hintRect.Overlaps(Rect.MinMaxRect(bodyMin.x,bodyMin.y,bodyMax.x,bodyMax.y)),context+" badge does not cover Asheng or his facing arrow");
        }
        private IEnumerator PressE(){game.SetVerificationInteraction(false);yield return null;game.SetVerificationInteraction(true);yield return null;yield return null;game.SetVerificationInteraction(false);yield return null;}
        private IEnumerator Walk(Vector2 goal)
        {
            List<Vector2> path=game.World.VerificationPath(game.Player.Body.position,goal);if(path==null){errors.Add("No clear route to "+goal);stopped=true;yield break;}
            foreach(Vector2 waypoint in path){int steps=0;while(Vector2.Distance(game.Player.Body.position,waypoint)>.045f){Vector2 delta=waypoint-game.Player.Body.position;game.Player.SetVerificationInput(delta.normalized*Mathf.Clamp01(delta.magnitude/(game.World.Settings.MovementSpeed*Time.fixedDeltaTime)));yield return new WaitForFixedUpdate();if(++steps>180){errors.Add("Stuck at "+waypoint);stopped=true;break;}}if(stopped)break;}game.Player.SetVerificationInput(Vector2.zero);yield return null;
        }
        private void Require(bool ok,string name){checks.Add(new Check{name=name,passed=ok,saveStatus=life.SaveStatusText,dirty=life.HasUnsavedChanges,grams=game.State.TotalGrams,day=game.State.Life.dayIndex,cashCents=game.State.CashCents});if(!ok)errors.Add(name);Debug.Log("P0_USABILITY_CHECK_"+(ok?"PASS ":"FAIL ")+name);}
        private IEnumerator Capture(string name)
        {
            yield return null;Camera.main.GetComponent<CameraFollow>().Snap();ui.Tick();Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();
            Camera camera=Camera.main;RenderTexture previous=camera.targetTexture,active=RenderTexture.active,target=RenderTexture.GetTemporary(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32);var texture=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
            try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);texture.Apply();var colors=new HashSet<Color32>();Color32[] pixels=texture.GetPixels32();for(int i=0;i<pixels.Length;i+=997)colors.Add(pixels[i]);if(colors.Count<8)errors.Add("Blank capture "+name);File.WriteAllBytes(Path.Combine(output,name),texture.EncodeToPNG());screenshots.Add(name);}
            finally{camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);Destroy(texture);}
        }
        private void RecordError(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
    }
}
